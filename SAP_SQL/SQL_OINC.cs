using System;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using DbClass;
using WMSWebAPI.Class;
using WMSWebAPI.Models.Request;
namespace WMSWebAPI.SAP_SQL
{
    public class SQL_OINC : IDisposable
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
        public SQL_OINC(string dbConnStr) => databaseConnStr = dbConnStr;

        /// <summary>
        /// Return list of the open statuc OINC - inventry counting doc
        /// </summary>
        /// <param name="docStatus"></param>
        /// <returns></returns>
        public OINC [] GetOpenOinc (string docStatus)
        {
            try
            {
                string query =
                    $"SELECT * " +
                    $"FROM {nameof(OINC)} ";


                if (docStatus.Length > 0) // open / closed / or all
                {
                    query += $"WHERE Status = @docStatus ";
                }

                using var conn = new SqlConnection(this.databaseConnStr);
                return conn.Query<OINC>(query, new { docStatus }).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Base on the oinc docentry ... return doc lines
        /// </summary>
        /// <param name="docEntry"></param>
        /// <returns></returns>
        public INC1 [] GetOincLines (int docEntry)
        {
            try
            {
                string query =
                    $"SELECT * " +
                    $"FROM {nameof(INC1)} " +
                    $"WHERE DocEntry = @docEntry " +
                    $"AND counted ='N' " +
                    $"AND LineStatus ='O'";

                using var conn = new SqlConnection(this.databaseConnStr);
                return conn.Query<INC1>(query, new { docEntry }).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Get list of the warehouse bin location for direction
        /// </summary>
        /// <returns></returns>
        public OBIN [] GetOBINs()
        {
            try
            {
                string query =
                    $"SELECT * " +
                    $"FROM {nameof(OBIN)} " +
                    $"WHERE Disabled = 'N'";

                using var conn = new SqlConnection(databaseConnStr);
                return conn.Query<OBIN>(query).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Create update inventory count Request
        /// </summary>
        public int CreateUpdateInventoryCountRequest(zwaRequest dtoRequest, zwaGRPO[] grpoLines)
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
