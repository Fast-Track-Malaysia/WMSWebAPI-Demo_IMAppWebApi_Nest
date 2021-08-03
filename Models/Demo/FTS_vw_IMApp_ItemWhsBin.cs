using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.Demo
{
    public class FTS_vw_IMApp_ItemWhsBin
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int BinAbs { get; set; }
        public decimal OnHandQty { get; set; }
        public string WhsCode { get; set; }
        public string BinCode { get; set; }        
    }
}
