using System;
namespace WMSWebAPI.Models
{
    /// <summary>
    /// 20200323T1032
    /// </summary>
    public class zwaLicencse2 /// exit in program for key creation
    {
        public int numberOfLicense { get; set; }
        public DateTime expiredDate { get; set; }
        public DateTime createdDate { get; set; }
        public string companyId { get; set; }
        public string contactPerson { get; set; }
    }
}
