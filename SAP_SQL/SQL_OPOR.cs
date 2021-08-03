using System;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using DbClass;
using WMSWebAPI.Class;
using WMSWebAPI.Models.Request;

namespace WMSWebAPI.SAP_SQL
{
    public class SQL_OPOR : IDisposable
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
        public SQL_OPOR(string dbConnStr) => databaseConnStr = dbConnStr;

        /// <summary>
        /// Return list of the business partner with open po item
        /// </summary>
        /// <returns></returns>
        public OCRD[] GetBpWithOpenPo()
        {
            try
            {
                string query =
                    $"SELECT * " +
                    $"FROM {nameof(OCRD)}" +
                    $"WHERE CardCode IN " +
                    $"(SELECT DISTINCT " +
                    $"CardCode " +
                    $"FROM {nameof(OPDN)} " +
                    $"WHERE DocStatus = 'O')";

                using var conn = new SqlConnection(databaseConnStr);
                return conn.Query<OCRD>(query).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }


        /// <summary>
        /// Return list of the purchase order line with open item
        /// </summary>
        /// <returns></returns>
        public OPOR[] GetOpenPo(string poStatus)
        {
            try
            {
                //string query =
                //    $"SELECT * " +
                //    $"FROM {nameof(OPOR)} ";

                // 20200628t2358 QUERY FROM view from database
                string query = "SELECT * FROM FTS_vw_IMApp_OPOR ";

                if (poStatus.Length > 0) // open / closed / or all
                {
                    query += $"WHERE DocStatus = @poStatus ";
                }

                query += $"AND DocType = 'I' "; // only cater for the item query

                using var conn = new SqlConnection(this.databaseConnStr);
                return conn.Query<OPOR>(query, new { poStatus }).ToArray();
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
        public POR1[] GetOpenPoLines(int poDocEntry)
        {
            try
            {
                //string query = $"SELECT * " +
                //    $"FROM {nameof(POR1)} " +
                //    $"WHERE DocEntry = @poDocEntry " +
                //    $"AND LineStatus = 'O'";

                // Query from view and code filter for doc entry
                string query = "SELECT * FROM FTS_vw_IMApp_POR1 WHERE docEntry = @poDocEntry";

                using var conn = new SqlConnection(databaseConnStr);
                return conn.Query<POR1>(query, new { poDocEntry }).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Query the database and get list of the warehouse
        /// 20200617T0952
        /// </summary>
        /// <returns></returns>
        public OWHS[] GetWarehouses()
        {
            try
            {
                string query = "SELECT * FROM OWHS";
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
        /// Create GRPO Request
        /// </summary>
        public int CreateGRPORequest(zwaRequest dtoRequest, zwaGRPO[] grpoLines)
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
