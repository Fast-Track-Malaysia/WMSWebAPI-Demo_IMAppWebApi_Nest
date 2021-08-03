using System;
namespace WMSWebAPI.Models
{
    /// <summary>
    /// A sub table support user table
    /// kept user group relation ship
    /// </summary>
    public class zwaUser1
    {
        #region fields for table zwaUser1
        public int id { get; set; }
        public int userId { get; set; }
        public string userIdName { get; set; }
        public int groupId { get; set; }
        public DateTime lastModiDate { get; set; }
        public string lastModiUser { get; set; }
        #endregion fields for table zwaUser1

        /// <summary>
        /// used by the inherent child
        /// </summary>
        /// <param name="initObj"></param>
        protected zwaUser1(zwaUser1 initObj)
        {
            id = initObj.id;
            userId = initObj.userId;
            userIdName = initObj.userIdName;
            groupId = initObj.groupId;
            lastModiDate = initObj.lastModiDate;
            lastModiUser = initObj.lastModiUser;
        }

        /// <summary>
        /// Use to create new zwaUser1 object
        /// </summary>
        public zwaUser1()
        {
            // to do code
        }
    }
}
