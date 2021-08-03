using System;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using DbClass;
namespace WMSWebAPI.SAP_SQL
{
    public class SQL_OPLN : IDisposable
    {
        string databaseConnStr { get; set; } = "";
        SqlConnection conn;
        SqlTransaction trans;

        public string LastErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose() => GC.Collect();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbConnString"></param>
        public SQL_OPLN(string dbConnStr) => databaseConnStr = dbConnStr;

        /// <summary>
        /// return list of the price list for app user selection
        /// </summary>
        /// <returns></returns>
        public OPLN [] GetPriceList()
        {
            try
            {
                string query = "SELECT * FROM OPLN";                
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Query<OPLN>(query).ToArray();
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return null;
            }
        }

        /// <summary>
        /// Get Existing Price List
        /// </summary>
        /// <returns></returns>
        public int GetExistingPriceList()
        {
            try
            {
                string query = "SELECT U_PriceListID FROM [@APPSETUP] WHERE U_Operation ='Goods Receipt'";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    var result = conn.ExecuteScalar(query);
                    if (result == null)
                    {
                        return -1;
                    }

                    return (int)result;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Get Existing Price List
        /// </summary>
        /// <returns></returns>
        public int GetExistingPriceList_GI()
        {
            try
            {
                string query = "SELECT U_PriceListID FROM [@APPSETUP] WHERE U_Operation ='Goods Issues'";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    var result = conn.ExecuteScalar(query);
                    if (result == null)
                    {
                        return -1;
                    }

                    return (int)result;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Update Selected Gr PriceList
        /// </summary>
        /// <param name="priceListId"></param>
        /// <returns></returns>
        public int UpdateSelectedGrPriceList(int priceListId)
        {
            try
            {
                string updateSql = 
                    $"UPDATE [@APPSETUP] SET U_PriceListID = @priceListId " +
                    $"WHERE U_Operation ='Goods Receipt'";

                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Execute(updateSql, new { priceListId } );
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Update Selected Gr PriceList
        /// </summary>
        /// <param name="priceListId"></param>
        /// <returns></returns>
        public int UpdateSelectedGiPriceList(int priceListId)
        {
            try
            {
                string updateSql =
                    $"UPDATE [@APPSETUP] SET U_PriceListID = @priceListId " +
                    $"WHERE U_Operation ='Goods Issues'";

                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Execute(updateSql, new { priceListId });
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
