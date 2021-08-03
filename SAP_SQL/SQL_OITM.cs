using Dapper;
using DbClass;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace WMSWebAPI.SAP_SQL
{
    public class SQL_OITM : IDisposable
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
        public SQL_OITM(string dbConnStr) => databaseConnStr = dbConnStr;


        /// <summary>
        /// Return list of item code search
        /// </summary>
        /// <returns></returns>
        public OITM[] GetItems()
        {
            try
            {
                string query = "SELECT * FROM OITM WHERE InvntItem = 'Y'";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Query<OITM>(query).ToArray();
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        ///  Return single item from the database
        /// </summary>
        /// <param name="itemCode"></param>
        /// <returns></returns>
        public OITM GetItem(string itemCode)
        {
            try
            {
                string query = "SELECT * FROM OITM WHERE ItemCode = @itemcode";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Query<OITM>(query, new { itemCode }).FirstOrDefault();
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }
    }
}
