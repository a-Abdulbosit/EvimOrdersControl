using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using MonitoringOrders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MonitoringOrders
{

    public class GoogleSheetReader
    {
        public async Task<List<Order>> ReadOrdersAsync()
        {
            string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
            string ApplicationName = "Evim zakazlar";
            string sheetId = "1jK7DU5UjYTFQ4gutO6sd3WzY1KYs5mxLJZGNpHGJaFo";
            string range = "A2:J";

            GoogleCredential credential;
            using (var stream = new FileStream("C:\\Users\\Zuc\\Desktop\\evim-463210-c19ae84c538c.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var request = service.Spreadsheets.Values.Get(sheetId, range);
            var response = await request.ExecuteAsync();
            var values = response.Values;

            var orders = new List<Order>();
            foreach (var row in values)
            {
                if (row.Count < 10 || row.Any(cell => string.IsNullOrWhiteSpace(cell?.ToString())))
                    continue;
                try
                {
                    var order = new Order
                    {
                        OrderDate = DateTime.Parse(row[0].ToString()),
                        Supplier = row[1].ToString(),
                        ProductName = row[2].ToString(),
                        Quantity = int.Parse(row[3].ToString()),
                        Price = decimal.Parse(row[4].ToString(), new CultureInfo("ru-RU")),
                        Total = decimal.Parse(row[5].ToString()),
                        ReadyDate = DateTime.Parse(row[6].ToString()),
                        ArrivalDate = DateTime.Parse(row[7].ToString()),
                        Status = row[8].ToString(),
                        OrderCode = row[9].ToString()
                    };
                    orders.Add(order);
                }
                catch { }
            }

            return orders;
        }
    }

}
