using System;
namespace WMSWebAPI.Models.BWY
{
    public class zwaInvtCount1
    {
        public int id { get; set; }
        public string CounterName { get; set; }
        public Guid ParentGuid { get; set; }
        public string ItemCode { get; set; }
        public decimal CountedQty { get; set; }
        public DateTime TransDate { get; set; }
        public int OrderId { get; set; }
        public string OutletId { get; set; }
        public string Bin { get; set; }
    }
}
