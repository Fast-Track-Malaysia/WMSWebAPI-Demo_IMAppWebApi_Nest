using Dapper;
using DbClass;
using System;
using System.Data.SqlClient;
using System.Linq;
namespace WMSWebAPI.SAP_SQL
{
    public class SQL_OWHS : IDisposable
    {
        SqlConnection conn;
        SqlTransaction trans;
        string databaseConnStr { get; set; } = string.Empty;

        public string LastErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose() => GC.Collect();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbConnString"></param>
        public SQL_OWHS(string dbConnStr) => databaseConnStr = dbConnStr;

        /// <summary>
        /// Query the database and get list of the warehouse
        /// 20200617T0952
        /// </summary>
        /// <returns></returns>
        public OWHS[] GetWarehouses()
        {
            try
            {
                string query = "SELECT * FROM FTS_vw_IMApp_OWHS";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Query<OWHS>(query).ToArray();
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public OBIN[] GetWarehousesObin(string WhsCode)
        {
            try
            {
                string query =
                    $"SELECT * FROM FTS_vw_IMApp_OBIN " +
                    $"WHERE WhsCode = @WhsCode";
                using var conn = new SqlConnection(databaseConnStr);
                return conn.Query<OBIN>(query, new { WhsCode }).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Get all obin location
        /// </summary>
        /// <returns></returns>
        public OBIN[] GetWarehousesObin()
        {
            try
            {
                string query =
                    $"SELECT * " +
                    $"FROM FTS_vw_IMApp_OBIN";
                using var conn = new SqlConnection(databaseConnStr);
                return conn.Query<OBIN>(query).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }
    }
}
