using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.Demo
{
    public class zwaInventoryRequest
    {
        public int Id { get; set; }
        public Guid Guid { get; set; }
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
        public string FromWarehouse { get; set; }
        public string ToWarehouse { get; set; }
        public string AppUser { get; set; }
        public DateTime TransTime { get; set; }
    }
}
