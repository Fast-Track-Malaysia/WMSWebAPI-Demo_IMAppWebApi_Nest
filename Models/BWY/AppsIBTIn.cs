using System;
namespace WMSWebAPI.Models.BWY
{
    public class AppsIBTIn
    {
        public int OID { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
        public string IBTNum { get; set; }
        public string Item { get; set; }
        public decimal Quantity { get; set; }
        public decimal Variance { get; set; }
        public string Status { get; set; }
        public bool SAP { get; set; }
        public int OptimisticLockField { get; set; }
        public int GCRecord { get; set; }
        public int IBTBaseLine { get; set; }
        public int IBTBaseEntry { get; set; }
        public string Bin { get; set; }
        public string QtyStatus { get; set; }
        public string QtyStatusSummary { get; set; }
        public string RowSummary { get; set; }
        public string RefComments { get; set; } // 20200721T2246 for delivery orver in comma or any comment
    }
}
