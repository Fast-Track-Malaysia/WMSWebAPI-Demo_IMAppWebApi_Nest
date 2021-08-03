using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.BWY
{
    public class AppsGRPO
    {
        public int OID { get; set; }
        public string CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
        public string PONum { get; set; }
        public string Item { get; set; }
        public decimal Quantity { get; set; }
        public decimal Variance { get; set; }
        public string Status { get; set; }
        public bool SAP { get; set; }
        public int OptimisticLockField { get; set; }
        public int GCRecord { get; set; }
        public string ReceiptQtyStatus { get; set; }
        public string ReceiptQtySummary { get; set; }
        public string Bin { get; set; }
        public int PoBaseLine { get; set; }
        public int PoBaseEntry { get; set; }
        public string POrefnum { get; set; } // 20200710T1438 for chua request to add in this field represent  U_Outlet + U_RefNo,  SBAPO20070001  need link to 20000097 
        public string RefComments { get; set; } // 20200721T2246 for delivery orver in comma or any comment
    }
}
