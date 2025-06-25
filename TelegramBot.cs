using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MonitoringOrders;

public class TelegramBot
{
    private readonly ITelegramBotClient client;
    private readonly GoogleSheetReader sheetReader;

    public TelegramBot(string token, GoogleSheetReader sheetReader)
    {
        this.client = new TelegramBotClient(token);
        this.sheetReader = sheetReader;
    }
    public void Start(long chatId)
    {
        var cts = new CancellationTokenSource();

        client.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token
        );

        Console.WriteLine("Bot is running...");

        var timer = new Timer(async _ =>
        {
            try
            {
                await NotifyOrdersAsync(chatId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in auto notifier: " + ex.Message);
            }
        }, null, TimeSpan.Zero, TimeSpan.FromDays(1)); 

        Console.ReadLine();
        cts.Cancel();
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cts)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;

            if(text == "/start")
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "📦 Zakazlar" }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = false
                };

                await client.SendTextMessageAsync(
                    chatId,
                    "Assalomu alaykum! Tanlang:",
                    replyMarkup: keyboard);
            }
            else if (text == "/zakazlar" || text == "📦 Zakazlar")
            {
                var orders = await sheetReader.ReadOrdersAsync();
                var suppliers = orders.Select(o => o.Supplier).Distinct();

                var buttons = suppliers
                    .Select(s => InlineKeyboardButton.WithCallbackData(s, $"supplier:{s}"))
                    .Chunk(2)
                    .Select(row => row.ToArray())
                    .ToArray();

                var markup = new InlineKeyboardMarkup(buttons);

                await client.SendTextMessageAsync(
                    chatId,
                    "Iltimos, ta'minotchini tanlang:",
                    replyMarkup: markup);
            }
            else
            {
                await bot.SendTextMessageAsync(chatId, "Yuboring: /zakazlar");
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            var callback = update.CallbackQuery!;
            var chatId = callback.Message.Chat.Id;

            if (callback.Data.StartsWith("supplier:"))
            {
                var supplierName = callback.Data.Replace("supplier:", "");

                var orders = await sheetReader.ReadOrdersAsync();
                var filtered = orders.Where(o => o.Supplier == supplierName).ToList();

                if (!filtered.Any())
                {
                    await client.SendTextMessageAsync(chatId, $"❌ '{supplierName}' uchun buyurtmalar topilmadi.");
                    return;
                }

                var groups = filtered.GroupBy(o => o.OrderCode);

                foreach (var group in groups)
                {
                    var msg = $"🧾 *{supplierName}* \n";

                    foreach (var order in group)
                    {
                        msg += $"\n📦 *{order.ProductName}* ({order.Quantity}m x {order.Price}$) = {order.Total}$\n" +
                               $"📅 {order.OrderDate:yyyy-MM-dd} | ✅ {order.ReadyDate:yyyy:MM:dd} | 🚚 {order.ArrivalDate:yyyy-MM-dd}\n" +
                               $"🏷 {order.Status}\n";
                    }

                    msg += $"\n💲 Jami: *{group.Sum(o => o.Total)}*";

                    await client.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
                }
            }
        }

    }
    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return Task.CompletedTask;
    }
    public async Task NotifyOrdersAsync(long chatId)
    {
        var orders = await sheetReader.ReadOrdersAsync();
        var today = DateTime.Today;

        var notifyOrders = orders.Where(o =>
            (o.ReadyDate - today).TotalDays == 5 ||
            (o.ArrivalDate - today).TotalDays == 5 ||
            o.ReadyDate.Date == today ||
            o.ArrivalDate.Date == today
        ).ToList();

        if (!notifyOrders.Any())
        {
            await client.SendTextMessageAsync(chatId, "Bugun eslatma kerak emas.");
            return;
        }

        var grouped = notifyOrders.GroupBy(o => o.OrderCode);
        await client.SendTextMessageAsync(chatId, $"\n\nBugun {DateTime.Today:yyyy:MM:dd}.\n\n");
        foreach (var group in grouped)
        {
            var message = $"📢 *Eslatma* — 🆔 *{group.Key}* buyurtmalari:\n";

            // Get the first order just to check shared info
            var first = group.FirstOrDefault();
            var sharedReasons = new List<string>();

            if ((first.ReadyDate - today).TotalDays == 5)
                sharedReasons.Add($"⏳ *5 kun qoldi* tayyor bo‘lishiga");

            if ((first.ArrivalDate - today).TotalDays == 5)
                sharedReasons.Add($"⏳ *5 kun qoldi* yetib kelishiga");

            if (first.ReadyDate.Date == today)
                sharedReasons.Add($"✅ *Bugun tayyor bo‘ladi*");

            if (first.ArrivalDate.Date == today)
                sharedReasons.Add($"📦 *Bugun yetib keladi*");

            // Show shared reasons once
            if (sharedReasons.Count > 0)
                message += "\n" + string.Join("\n", sharedReasons) + "\n";

            message += $"\n🙍🏻‍♂️ *Ta'minotchi:* {first.Supplier}\n";

            // List all products in this group
            foreach (var order in group)
            {
                message +=
                    $"\n📦 *{order.ProductName}*\n" +
                    $"🔢 {order.Quantity}m × {order.Price}$ = {order.Total}$\n" +
                    $"🗓 Buyurtma kuni: {order.OrderDate:yyyy-MM-dd}\n" +
                    $"🏷 {order.Status}\n";
            }

            await client.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Markdown);
        }

    }

}
