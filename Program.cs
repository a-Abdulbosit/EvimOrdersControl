namespace MonitoringOrders;

public class Program
{
    static async Task Main(string[] args)
    {
        var sheetReader = new GoogleSheetReader();
        var bot = new TelegramBot("7823479701:AAFKtvkOLzz-7kzg73QYA8CgNgzd1Kn6qV4", sheetReader);


        var chatId = 1333252980;
        bot.Start(chatId);

    }
}
