using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringOrders.Models
{
    public class Order
    {
        public DateTime OrderDate { get; set; }
        public string Supplier { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
        public DateTime ReadyDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public string Status { get; set; }
        public string OrderCode { get; set; }
    }

}
