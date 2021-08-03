using Dapper;
using System;
using System.Data.SqlClient;
using System.Linq;
using WMSWebAPI.Models.Request;

namespace WMSWebAPI.Opr
{
    /// <summary>
    /// The middle man request handle the reset tried and status of the doc status
    /// </summary>
    public class MiddleManRequest : IDisposable
    {
        string databaseConnStr { get; set; } = string.Empty;
        public string GetLastErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose() => GC.Collect();

        /// <summary>
        /// Construtor, and init
        /// </summary>
        public MiddleManRequest()
        {
            try
            {
                var middleManConnStr = System.Configuration.ConfigurationManager.AppSettings["DBConnect_SAP"];
                databaseConnStr = middleManConnStr?.ToString();
            }
            catch (Exception excep)
            {
                GetLastErrorMessage = $"{excep}";
            }
        }

        /// <summary>
        /// Return array of request who tried >= 3 and in onhold status
        /// </summary>
        /// <returns></returns>
        public zmwRequest[] GetOnHoldRequest()
        {
            try
            {
                string query = $"SELECT * " +
                                $"FROM zmwRequest " +
                                $"WHERE tried >= 3 " +
                                $"AND status = 'ONHOLD'";

                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Query<zmwRequest>(query).ToArray();
                }
            }
            catch (Exception excep)
            {
                GetLastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// use by controller to rest the request, so middel ware can redo the request
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int ResetRequest(zmwRequest[] list)
        {
            try
            {
                string query = $"UPDATE zmwRequest " +
                    $"SET tried = 0" +
                    $",STATUS='ONHOLD'" +
                    $" WHERE id=@id" +
                    $" AND guid=@guid";

                using (var conn = new SqlConnection(databaseConnStr))
                {
                    foreach (var problem in list)
                    {
                        conn.Execute(query, new { problem.id, problem.guid });
                    }
                    return list.Length;
                }
            }
            catch (Exception excep)
            {
                GetLastErrorMessage = $"{excep}";
                return -1;
            }
        }
    }
}
