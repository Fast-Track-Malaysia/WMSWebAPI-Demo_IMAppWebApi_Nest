using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.BWY
{
    public class AppsStockCounts
    {
        public int OID { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
        public string StockCountNum { get; set; }
        public string Item { get; set; }
        public decimal Quantity { get; set; }
        public decimal Variance { get; set; }
        public string Bin { get; set; }
        public string Status { get; set; }
        public bool SAP { get; set; }
        public int OptimisticLockField { get; set; }
        public int GCRecord { get; set; }
        public string Counter { get; set; }
        public int CountOrder { get; set; }
        public DateTime CountDateTime { get; set; }
        public string Outlet { get; set; }
    }
}
