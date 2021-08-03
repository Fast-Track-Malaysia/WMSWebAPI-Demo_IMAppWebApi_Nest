namespace WMSWebAPI.Models
{
    /// <summary>
    /// Serve as the ext of the zwaUserGroup log
    /// </summary>
    public class zwaUserGroupLog : zwaUserGroup
    {
        public int id { get; set; }
        public string action { get; set; }

        /// <summary>
        /// construtor to update action 
        /// and it ingerent parent base
        /// </summary>
        /// <param name="act"></param>
        /// <param name="obj"></param>
        public zwaUserGroupLog(string act, zwaUserGroup obj) : base(obj)
        {
            this.action = act;
        }
    }
}
