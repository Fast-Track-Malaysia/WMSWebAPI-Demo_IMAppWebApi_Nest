using System;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Policy;
using Dapper;
using DbClass;
using WMSWebAPI.Class;
using WMSWebAPI.Models.Request;
namespace WMSWebAPI.SAP_SQL
{
    public class SQL_ORDR : IDisposable
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
        public SQL_ORDR(string dbConnStr) => databaseConnStr = dbConnStr;

        /// <summary>
        /// Return list of the purchase order line with open item
        /// </summary>
        /// <returns></returns>
        public ORDR[] GetOpenSo(string soStatus)
        {
            try
            {
                //string query =
                //    $"SELECT * " +
                //    $"FROM {nameof(ORDR)} ";

                string query = "SELECT * FROM FTS_vw_IMApp_ORDR ";

                if (soStatus.Length > 0) // open / closed / or all
                {
                    query += $"WHERE DocStatus = @soStatus ";
                }

                query += $"AND DocType = 'I' "; // only cater for the item query

                using var conn = new SqlConnection(this.databaseConnStr);
                return conn.Query<ORDR>(query, new { soStatus }).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Return list of the PO line, based on Doc entry array 
        /// Support 1 or more purchase order doc entry
        /// </summary>
        /// <param name="DocEntry"></param>
        /// <returns></returns>
        public RDR1[] GetOpenSoLines(int soDocEntry)
        {
            try
            {
                //string query = $"SELECT * " +
                //    $"FROM {nameof(RDR1)} " +
                //    $"WHERE DocEntry = @soDocEntry " +
                //    $"AND LineStatus = 'O'";

                // use the view to query the 
                using var conn = new SqlConnection(databaseConnStr);
                return conn.Query<RDR1>("SELECT * FROM FTS_vw_IMApp_RDR1 WHERE DocEntry=@soDocEntry ", new { soDocEntry }).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Base on the item code
        /// Query the item qty for each warehouse
        /// </summary>
        /// <param name="itemCode"></param>
        /// <returns></returns>
        public OITW[] GetItemQtyWarehouse(string itemCode)
        {
            try
            {
                string query = $"SELECT * FROM {nameof(OITW)} WHERE ItemCode = @itemCode AND OnHand > 0";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Query<OITW>(query, itemCode).ToArray();
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Create GRPO Request
        /// </summary>
        public int CreateSoRequest(zwaRequest dtoRequest, zwaGRPO[] doLines) // resue the zwaGRPO object table
        {
            try
            {
                ConnectAndStartTrans();
                string insertSql = $"INSERT INTO {nameof(zwaRequest)} (" +
                    $"request" +
                    $",sapUser " +
                    $",sapPassword" +
                    $",requestTime" +
                    $",phoneRegID" +
                    $",status" +
                    $",guid" +
                    $",sapDocNumber" +
                    $",completedTime" +
                    $",attachFileCnt" +
                    $",tried" +
                    $",createSAPUserSysId " +
                    $")VALUES(" +
                    $"@request" +
                    $",@sapUser" +
                    $",@sapPassword" +
                    $",GETDATE()" +
                    $",@phoneRegID" +
                    $",@status" +
                    $",@guid" +
                    $",@sapDocNumber" +
                    $",GETDATE()" +
                    $",@attachFileCnt" +
                    $",@tried" +
                    $",@createSAPUserSysId)";

                using (conn)
                using (trans)
                {
                    var result = conn.Execute(insertSql, dtoRequest, trans);
                    if (result < 0) return -1;

                    /// perform insert of all the GRPO item 

                    if (doLines.Length > 0)
                    {
                        string insertGrpo = $"INSERT INTO {nameof(zwaGRPO)} " +
                            $"(Guid" +
                            $",ItemCode" +
                            $",Qty" +
                            $",SourceCardCode" +
                            $",SourceDocNum" +
                            $",SourceDocEntry" +
                            $",SourceDocBaseType" +
                            $",SourceBaseEntry" +
                            $",SourceBaseLine) VALUES (" +
                            $"@Guid" +
                            $",@ItemCode" +
                            $",@Qty" +
                            $",@SourceCardCode" +
                            $",@SourceDocNum" +
                            $",@SourceDocEntry" +
                            $",@SourceDocBaseType" +
                            $",@SourceBaseEntry" +
                            $",@SourceBaseLine)";

                        result = conn.Execute(insertGrpo, doLines, trans);
                        CommitDatabase();

                        return result;
                    }
                    return -1;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                Rollback();
                return -1;
            }
        }


        /// <summary>
        ///  use to init the database insert transation
        /// </summary>
        public void ConnectAndStartTrans()
        {
            try
            {
                conn = new SqlConnection(databaseConnStr);
                conn.Open();
                trans = conn.BeginTransaction();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
            }
        }

        /// <summary>
        /// Use to commit a database
        /// </summary>
        public void CommitDatabase()
        {
            try
            {
                trans?.Commit();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
            }
        }

        /// <summary>
        /// Use to roll back a transaction
        /// </summary>
        public void Rollback()
        {
            try
            {
                trans.Rollback();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
            }
        }
    }
}
