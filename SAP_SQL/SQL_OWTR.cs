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
    public class SQL_OWTR : IDisposable
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
        public SQL_OWTR(string dbConnStr) => databaseConnStr = dbConnStr;

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
        public int CreateTransferRequest(zwaRequest dtoRequest,
            zwaItemTransferBin[] dtoDetailsBins,
            zwaTransferDetails[] dtoTransferDetails, zwaTransferHead head)
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
                    $",@createSAPUserSysId )";

                using (conn)
                using (trans)
                {
                    var result = conn.Execute(insertSql, dtoRequest, trans);
                    if (result < 0) return -1;

                    /// perform insert of all the GRPO item 

                    if (dtoTransferDetails.Length > 0)
                    {

                        //public int Id { get; set; }
                        //public Guid Guid { get; set; }
                        //public string ItemCode { get; set; }
                        //public int FromBinAbs { get; set; }
                        //public int ToBinAbs { get; set; }
                        //public string FromWhsCode { get; set; }
                        //public string ToWhsCode { get; set; }
                        //public string BatchNo { get; set; }
                        //public string SerialNo { get; set; }
                        //public decimal TransferQty { get; set; }
                        //public DateTime TransDate { get; set; }

                        string insertTransferRequest = $"INSERT INTO {nameof(zwaTransferDetails)} " +
                                            $"([Guid] " +
                                            $",[ItemCode] " +
                                            $",[FromBinAbs] " +
                                            $",[ToBinAbs] " +
                                            $",[FromWhsCode] " +
                                            $",[ToWhsCode] " +
                                            $",[BatchNo] " +
                                            $",[SerialNo] " +
                                            $",[TransferQty] " +
                                            $",[TransDate]  " +
                                            $",[SourceDocNum] " +
                                            $",[SourceDocEntry] " +
                                            $",[SourceDocBaseType] " +
                                            $",[SourceBaseEntry] " +
                                            $",[SourceBaseLine] " +
                                            $",[LineGuid] " +
                                            $") VALUES (" +
                                            $"@Guid " +
                                            $",@ItemCode " +
                                            $",@FromBinAbs " +
                                            $",@ToBinAbs " +
                                            $",@FromWhsCode " +
                                            $",@ToWhsCode " +
                                            $",@BatchNo " +
                                            $",@SerialNo " +
                                            $",@TransferQty " +
                                            $",@TransDate " +
                                            $",@SourceDocNum " +
                                            $",@SourceDocEntry " +
                                            $",@SourceDocBaseType " +
                                            $",@SourceBaseEntry " +
                                            $",@SourceBaseLine " +
                                            $",@LineGuid " +
                                            $") ";

                        result = conn.Execute(insertTransferRequest, dtoTransferDetails, trans);

                        // insert the details bins
                        if (result > 0 && dtoDetailsBins?.Length > 0)
                        {
                            string insertBins = $"INSERT INTO {nameof(zwaItemTransferBin)} " +
                                $"([Guid] " +
                                $",[ItemCode] " +
                                $",[Quantity] " +
                                $",[BinCode] " +
                                $",[FromBinAbsEntry] " +
                                $",[ToBinAbsEntry] " +
                                $",[BatchNumber] " +
                                $",[SerialNumber] " +
                                $",[TransType] " +
                                $",[TransDateTime] " +
                                $",[LineGuid] " +
                                $" ) VALUES (" +
                                $" @Guid " +
                                $",@ItemCode " +
                                $",@Quantity " +
                                $",@BinCode " +
                                $",@FromBinAbsEntry " +
                                $",@ToBinAbsEntry " +
                                $",@BatchNumber " +
                                $",@SerialNumber " +
                                $",@TransType " +
                                $",@TransDateTime " +
                                $",@LineGuid)";

                            result = conn.Execute(insertBins, dtoDetailsBins, trans);
                        }

                        if (result > 0)
                        {
                            string insertHead =
                                $"INSERT INTO {nameof(zwaTransferHead)} " +
                                $"([FromWarehouse] " +
                                $",[ToWarehouse] " +
                                $",[Guid] " +
                                $",[TransDate] " +
                                $",[DocNumber] " +
                                $",[Remarks] " +
                                $") VALUES ( " +
                                $" @FromWarehouse " +
                                $",@ToWarehouse " +
                                $",@Guid " +
                                $",@TransDate " +
                                $",@DocNumber " +
                                $",@Remarks )";

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

        /// <summary>
        /// Get list of bin based on warehouse code
        /// </summary>
        /// <param name="whsCode"></param>
        /// <returns></returns>
        public OBIN[] GetWarehouseBin(string whsCode)
        {
            try
            {
                return new SqlConnection(this.databaseConnStr)
                    .Query<OBIN>("SELECT * FROM [FTS_vw_IMApp_OBIN] WHERE WhsCode =@WhsCode",
                    new { WhsCode = whsCode }).ToArray();

            }
            catch (Exception excep)
            {
                LastErrorMessage = $"{excep}";
                return null;
            }
        }

    }
}
