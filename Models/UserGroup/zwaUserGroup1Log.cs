namespace WMSWebAPI.Models
{
    /// <summary>
    /// zwaUserGroup1Log object ext from zwaUserGroup1
    /// </summary>
    public class zwaUserGroup1Log : zwaUserGroup1
    {
        public int id { get; set; }
        public string action { get; set; }
        public zwaUserGroup1Log(string act, zwaUserGroup1 obj) : base(obj)
        {
            this.action = act;
        }
    }

}
