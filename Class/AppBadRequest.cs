using System;

namespace WMSWebAPI.Class
{
    /// <summary>
    /// Use to replied the bad request massage
    /// </summary>
    public class AppBadRequest
    {
        public string Message { get; set; }
        public  DateTime TransDate { get; set; }

        public AppBadRequest (Exception exception)
        {
            TransDate = DateTime.Now;
            Message = exception.ToString();
        }

        /// <summary>
        /// Overrive the to string method
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return TransDate.ToString("dd-MM-yyyy hh:mm" + Message);
        }
    }
}
