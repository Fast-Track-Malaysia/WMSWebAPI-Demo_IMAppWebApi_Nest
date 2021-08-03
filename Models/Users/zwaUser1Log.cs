namespace WMSWebAPI.Models
{
    /// <summary>
    ///   Ext the zwaUser1 Table add 
    /// </summary>
    public class zwaUser1Log : zwaUser1
    {
        public string action { get; set; } // extented properties for log    

        /// <summary>
        /// Contructor for the zwaUserLog and it inherant parant
        /// </summary>
        /// <param name="currentAction"></param>
        /// <param name="obj"></param>
        public zwaUser1Log(string currentAction, zwaUser1 obj) : base(obj)
        {
            this.action = currentAction;
        }
    }
}
