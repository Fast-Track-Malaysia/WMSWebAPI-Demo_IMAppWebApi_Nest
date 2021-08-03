using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.Demo
{
    public class zwaItemBin
    {        public int Id { get; set; }
        public Guid Guid { get; set; }
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
        public string BinCode { get; set; }
        public int BinAbsEntry { get; set; }
        public string BatchNumber { get; set; }
        public string SerialNumber { get; set; }
        public string TransType { get; set; }
        public DateTime TransDateTime { get; set; }
    }
}
