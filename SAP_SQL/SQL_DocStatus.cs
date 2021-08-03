using Dapper;
using System;
using System.Data.SqlClient;
using System.Linq;
using WMSWebAPI.Models.Request;

namespace WMSWebAPI.SAP_SQL
{
    public class SQL_DocStatus : IDisposable
    {
        string databaseConnStr { get; set; } = "";

        public string LastErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose() => GC.Collect();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbConnString"></param>
        public SQL_DocStatus(string dbConnStr) => databaseConnStr = dbConnStr;

        /// <summary>
        /// Check the doc request status with guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public zmwRequest GetRequestStatuc(string guid)
        {
            try
            {
                string query = $"SELECT * FROM {nameof(zmwRequest)} WHERE guid = @guid";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    var result = conn.Query<zmwRequest>(query, new { guid }).FirstOrDefault();
                    if (result == null) return null;


                    if (!string.IsNullOrWhiteSpace(result.lastErrorMessage) && result.tried >= 3 && result.IsNotify != 1)
                    {
                        UpdateRequestNotification(guid);
                        return result;
                    }

                    if (result.lastErrorMessage != null && result.lastErrorMessage.Contains("Success".ToUpper()))
                    {
                        UpdateRequestNotification(guid);
                        return result;
                    }
                    return null;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        /// <summary>
        ///  update the request is notified
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        int UpdateRequestNotification(string guid)
        {
            try
            {
                string update = $"UPDATE {nameof(zmwRequest)} SET IsNotify = 1 WHERE guid=@guid";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Execute(update, new { guid });
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }
    }
}
