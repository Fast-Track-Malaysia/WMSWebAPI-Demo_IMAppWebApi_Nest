using System;
using System.Data.SqlClient;
using Dapper;
using WMSWebAPI.Class;
using WMSWebAPI.Models.Request;
namespace WMSWebAPI.SAP_SQL
{
    public class SQL_OIGE : IDisposable
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
        public SQL_OIGE(string dbConnStr) => databaseConnStr = dbConnStr;

        /// <summary>
        /// Get Goods Receipt DocSeries
        /// </summary>
        /// <returns></returns>
        public string GetGoodsIssuesDocSeries()
        {
            try
            {
                string query = "SELECT U_DocumentSeries FROM [@APPSETUP] WHERE U_Operation='Goods Issues'";
                using (var conn = new SqlConnection(databaseConnStr))
                {
                    var result = conn.ExecuteScalar(query);
                    if (result == null) return string.Empty;

                    return (string)result;
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return string.Empty;
            }
        }

        /// <summary>
        /// Use to update the UDT for goods receipt doc series
        /// </summary>
        /// <param name="docSeries"></param>
        /// <returns></returns>
        public int UpdateGoodsIssuesDocSeries(string docSeries)
        {
            try
            {
                string updateSql = "UPDATE [@APPSETUP] SET U_DocumentSeries = @docSeries " +
                    "WHERE U_Operation='Goods Issues'";

                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Execute(updateSql, new { docSeries });
                }
            }
            catch (Exception excep)
            {
                LastErrorMessage = excep.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Create GRPO Request
        /// </summary>
        public int CreateGoodsIssuesRequest(zwaRequest dtoRequest, zwaGRPO[] grpoLines)
        {
            try
            {
                if (dtoRequest == null) return -1;
                if (grpoLines == null) return -1;
                if (grpoLines.Length == 0) return -1;

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

                    if (grpoLines.Length > 0)
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
                            $",SourceBaseLine" +
                            $",Warehouse) VALUES (" +
                            $"@Guid" +
                            $",@ItemCode" +
                            $",@Qty" +
                            $",@SourceCardCode" +
                            $",@SourceDocNum" +
                            $",@SourceDocEntry" +
                            $",@SourceDocBaseType" +
                            $",@SourceBaseEntry" +
                            $",@SourceBaseLine " +
                            $",@Warehouse)";

                        result = conn.Execute(insertGrpo, grpoLines, trans);
                        CommitDatabase();
                        return result;
                    }

                    Rollback();
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
