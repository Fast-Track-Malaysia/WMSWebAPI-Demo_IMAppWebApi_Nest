using System;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using Dapper;
using DbClass;
using WMSWebAPI.Class;
using WMSWebAPI.Models.Demo;
using WMSWebAPI.Models.Request;

namespace WMSWebAPI.SAP_SQL
{
    public class SQL_OWTQ : IDisposable
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
        public SQL_OWTQ(string dbConnStr) => databaseConnStr = dbConnStr;

        public OWTQ[] GetTransferRequestList(Cio bag)
        {
            try
            {
                // get open line from the request
                string query = string.Empty;
                if (bag.RequestTransferDocFilter.Equals("a"))
                {
                    query = "SELECT * FROM [FTS_vw_IMApp_OWTQ] " +
                        "WHERE Docdate >= @StartDate " +
                        "AND docDate <= @EndDate ";

                    return new SqlConnection(databaseConnStr)
                           .Query<OWTQ>(query,
                           new
                           {
                               StartDate = bag.RequestTransferStartDt,
                               EndDate = bag.RequestTransferEndDt
                           }).ToArray();
                }

                query = "SELECT * FROM [FTS_vw_IMApp_OWTQ] " +
                    "WHERE Docdate >= @StartDate " +
                    "AND docDate <= @EndDate " +
                    "AND DocStatus = @DocStatus";

                return new SqlConnection(databaseConnStr)
                       .Query<OWTQ>(query,
                       new
                       {
                           StartDate = bag.RequestTransferStartDt,
                           EndDate = bag.RequestTransferEndDt,
                           DocStatus = bag.RequestTransferDocFilter
                       }).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// return transfer request open line
        /// </summary>
        /// <returns></returns>
        public WTQ1[] GetTransferRequestLines(int transferRequestDocEntry)
        {
            try
            {
                // get open line from the request
                return new SqlConnection(databaseConnStr).Query<WTQ1>(
                    $"select * from [FTS_vw_IMApp_WTQ1] " +
                    $"where DocEntry = @DocEntry", new { DocEntry = transferRequestDocEntry }).ToArray();
            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// check item code
        /// </summary>
        /// <param name="itemCode"></param>
        /// <returns></returns>
        public OITM CheckItemCodeExist(string itemCode)
        {
            // get open line from the request
            return new SqlConnection(databaseConnStr).Query<OITM>(
                $"select * from [FTS_vw_IMApp_OITM] " +
                $"where ItemCode = @ItemCode", new { ItemCode = itemCode }).FirstOrDefault();
        }

        /// <summary>
        /// check item code
        /// </summary>
        /// <param name="itemCode"></param>
        /// <returns></returns>
        public OITW CheckItemCodeQtyInWarehouse(string ItemCode, string WhsCode)
        {
            return new SqlConnection(databaseConnStr).Query<OITW>(
                $"SELECT * FROM [FTS_vw_IMApp_OITW] " +
                $"WHERE ItemCode = @ItemCode " +
                $"AND WhsCode = @WhsCode", new { ItemCode, WhsCode }).FirstOrDefault();
        }

        /// <summary>
        /// Get Item Whs Bin
        /// </summary>
        /// <param name="ItemCode"></param>
        /// <param name="WhsCode"></param>
        public FTS_vw_IMApp_ItemWhsBin [] GetItemWhsBin (string ItemCode, string WhsCode)
        {
            return  new SqlConnection(databaseConnStr).Query<FTS_vw_IMApp_ItemWhsBin>(
                $"SELECT * FROM [FTS_vw_IMApp_ItemWhsBin] " +
                $"WHERE ItemCode = @ItemCode " +
                $"AND WhsCode = @WhsCode", new { ItemCode, WhsCode }).ToArray();
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

        /// <summary>
        /// Create Inventory Request
        /// </summary>
        /// <returns></returns>
        public int CreateInventoryRequest(zwaRequest dtoRequest,
            zwaInventoryRequest[] dtozwaInventoryRequest, zwaInventoryRequestHead head)
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

                    if (dtozwaInventoryRequest.Length > 0)
                    {
                        string insertInventoryRequest = $"INSERT INTO {nameof(zwaInventoryRequest)} " +
                            $"([Guid] " +
                            $",[ItemCode] " +
                            $",[Quantity] " +
                            $",[FromWarehouse] " +
                            $",[ToWarehouse] " +
                            $",[AppUser] " +
                            $",[TransTime]) " +
                            $"VALUES " +
                            $"(@Guid " +
                            $",@ItemCode " +
                            $",@Quantity " +
                            $",@FromWarehouse " +
                            $",@ToWarehouse " +
                            $",@AppUser " +
                            $",@TransTime) ";

                        result = conn.Execute(insertInventoryRequest, dtozwaInventoryRequest, trans);

                        if (result > 0)
                        {
                            string insertHead =
                                $"INSERT INTO {nameof(zwaInventoryRequestHead)} " +
                                $"([ToWarehouse]" +
                                $",[FromWarehouse]" +
                                $",[Guid]" +
                                $",[TransDate] " +
                                $",[DocNumber] " +
                                $",[Remarks]" +
                                $") VALUES " +
                                $"(@ToWarehouse" +
                                $",@FromWarehouse" +
                                $",@Guid" +
                                $",@TransDate " +
                                $",@DocNumber " +
                                $",@Remarks)";

                            result = conn.Execute(insertHead, head, trans);
                        }
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
    }
}
