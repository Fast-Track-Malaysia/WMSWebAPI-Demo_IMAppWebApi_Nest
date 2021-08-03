using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.Request
{
    public class zwaGRPO
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public string ItemCode { get; set; }
        public decimal Qty { get; set; }
        public string SourceCardCode { get; set; }
        public int SourceDocNum { get; set; }
        public int SourceDocEntry { get; set; }
        public int SourceDocBaseType { get; set; }
        public int SourceBaseEntry { get; set; }
        public int SourceBaseLine { get; set; }
        public string Warehouse { get; set; } // add in 20200634T1834
    }
}
