using System;
namespace WMSWebAPI.Models.BWY
{
    public class zwainvtCount
    {
        public int id { get; set; }
        public string CountName { get; set; }
        public string Status { get; set; }
        public DateTime CreateDt { get; set; }
        public string CreatedBy { get; set; }
        public string OutletId { get; set; }
        public Guid Guid { get; set; }
    }
}
