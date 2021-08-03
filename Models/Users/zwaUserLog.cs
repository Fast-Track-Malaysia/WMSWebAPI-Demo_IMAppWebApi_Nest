namespace WMSWebAPI.Models
{/// <summary>
 /// Ext the zwaUser Table add 
 /// </summary>
    public class zwaUserLog : zwaUser
    {
        public string action { get; set; } // extented properties for log
        public int id { get; set; } // extented properties for log

        /// <summary>
        /// the constructor for handler it base and it owm properties
        /// </summary>
        /// <param name="currentAct"></param>
        /// <param name="obj"></param>
        public zwaUserLog(string currentAct, zwaUser obj) : base(obj)
        {
            this.action = currentAct;
        }
    }
}
