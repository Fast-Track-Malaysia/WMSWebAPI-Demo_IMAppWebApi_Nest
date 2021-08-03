using Dapper;
using DbClass;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using WMSWebAPI.Class;
using WMSWebAPI.Models.BWY;

namespace WMSWebAPI.Controllers.BWY_Impl
{
    [Route("[controller]")]
    [ApiController]
    public class GRPO_BwyController : ControllerBase
    {
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly string _webportal = "DatabaseWebPortalConn"; // 20200710T0031 for update data into web portal

        readonly IConfiguration _configuration;
        ILogger _logger;

        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _dbConnectionStr_webPortal = string.Empty;

        string _lastErrorMessage = string.Empty;

        public GRPO_BwyController(IConfiguration configuration, ILogger<GrpoController> logger)
        {
            _configuration = configuration;
            _dbConnectionStr = _configuration.GetConnectionString(_dbName);
            _dbConnectionStr_webPortal = _configuration.GetConnectionString(_webportal);
            _logger = logger;
        }

        /// <summary>
        /// Controller entry point
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        [Authorize(Roles = "SuperAdmin, Admin, User")] /// tested with authenticated token based   
        [HttpPost]
        public IActionResult ActionPost(Cio bag)
        {
            try
            {
                switch (bag.request)
                {
                    case "QueryPoAndLines":
                        {
                            return QueryPoAndLines(bag);
                        }
                    case "InsertPOAndLine":
                        {
                            return InsertPOAndLine(bag);
                        }
                    case "CheckDoInvDocNumExist": // 20200729T2013 for check GRPO do 
                        {
                            return CheckDoInvDocNumExist(bag);
                        }
                    case "QueryItemsExistance": // 20200811T0008
                        {
                            return QueryItemsExistance(bag);
                        }
                }
                // else not request mapped
                return BadRequest(new AppBadRequest(new Exception("Invalid request specific, please try again later")));
            }
            catch (Exception excep)
            {
                var br = new AppBadRequest(excep);
                Log($"{br}", bag);
                return BadRequest(br);
            }

            /// <summary>
            /// 20200726T1800
            /// Use to check the pass in code
            /// is available at web portal
            /// </summary>
            /// <param name="bag"></param>
            /// <returns></returns>
            IActionResult QueryItemsExistance(Cio bag)
            {
                try
                {
                    // check item master first
                    string query = "SELECT * FROM ItemMasters WHERE Item = @Item";
                    using var conn = new SqlConnection(_dbConnectionStr_webPortal);
                    var item = conn.Query<ItemMasters>(query, new { Item = bag.InvtCntItemCode }).FirstOrDefault();

                    if (item != null)
                    {
                        bag.FoundItem = item;
                        return Ok(bag);
                    }

                    // if not found, check bar code
                    query = $"SELECT t1.* " +
                        $"FROM ItemMasterBarCode t0 " +
                        $"INNER JOIN ItemMasters t1 ON t0.ItemMasters = t1.OID " +
                        $"WHERE t0.BarCodeInfo = @Item";

                    var itemByBarcode = conn.Query<ItemMasters>(query, new { Item = bag.InvtCntItemCode }).FirstOrDefault();
                    if (itemByBarcode != null)
                    {
                        bag.FoundItem = itemByBarcode;
                        return Ok(bag);
                    }

                    // if not found in item master and barcode
                    // return NOK
                    var br = new AppBadRequest(new Exception($"Item {bag.InvtCntItemCode} no found as ItemCode or BarCode."));
                    Log($"{br}", bag);
                    return BadRequest(br);
                }
                catch (Exception excep)
                {
                    var br = new AppBadRequest(excep);
                    Log($"{br}", bag);
                    return BadRequest(br);
                }
            }

            /// <summary>
            /// 20200729T2205
            /// Check  GRPO supplier DO number or invoice number
            /// Ensure the DO Number and invoice number always unique
            /// Return OK to save 
            /// Return bad to exit
            /// </summary>
            /// <returns></returns>
            IActionResult CheckDoInvDocNumExist(Cio bag)
            {
                try
                {
                    string query = $"SELECT * " +
                        $"FROM {nameof(AppsGRPO)} " +
                        $"WHERE RefComments LIKE @RefComments";

                    using var conn = new SqlConnection(_dbConnectionStr_webPortal);

                    var found = conn.Query(query, new { RefComments = $"%{bag.GrpoDoInvVerificationDocNum}%" }).FirstOrDefault();
                    if (found != null)
                    {
                        var br = new AppBadRequest(
                            new Exception($"GRPO Supplier DO or Inv: found duplicated, {bag.GrpoDoInvVerificationDocNum}"));
                        Log($"{br}", bag);
                        return BadRequest(br);
                    }

                    return Ok(bag);
                }
                catch (Exception excep)
                {
                    var br = new AppBadRequest(excep);
                    Log($"{br}", bag);
                    return BadRequest(br);
                }
            }


            /// <summary>
            /// Return the PO herder and PO Lines details
            /// </summary>
            /// <param name="docNum"></param>
            /// <returns></returns>
            IActionResult QueryPoAndLines(Cio bag, bool isQuerybyPOrefnum = false)
            {
                try
                {
                    if (isQuerybyPOrefnum)
                    {
                        return QueryPoAndLinesByPo(bag);
                    }

                    using var conn = new SqlConnection(_dbConnectionStr);
                    var opor = conn.Query<OPOR>("zwaGetPOWithPOrefnum",
                            new
                            {
                                POrefnum = bag.QueryDocNumber
                            },
                            commandType: CommandType.StoredProcedure).First();


                    if (opor == null)
                    {
                        return NotFound(bag); // return purchase order not found
                    }

                    // check AppsGRPO table to ensure PO is always new, yet receive from any scanner
                    // 20200721T2132
                    using var WebPortalConn = new SqlConnection(_dbConnectionStr_webPortal);
                    var appsGrpo = WebPortalConn.Query<AppsGRPO>(
                        $"SELECT * FROM {nameof(AppsGRPO)} WHERE PONum = @DocNum", new { opor.DocNum }).FirstOrDefault();

                    if (appsGrpo != null)
                    {
                        return NotFound(bag); // return purchase order not found, or i
                    }

                    // load the po lines
                    bag.DtoPo = opor;
                    var query = $"SELECT * FROM POR1 " +
                        $"WHERE DocEntry = @DocEntry " +
                        $"AND LineStatus = 'O'";

                    bag.DtoPoLines = conn.Query<POR1>(query, new { opor.DocEntry }).ToArray();
                    return Ok(bag);
                }
                catch (Exception excep)
                {
                    var br = new AppBadRequest(excep);
                    Log($"{br}", bag);
                    return BadRequest(br);
                }
            }

            /// <summary>
            /// Return the PO herder and PO Lines details
            /// </summary>
            /// <param name="docNum"></param>
            /// <returns></returns>
            IActionResult QueryPoAndLinesByPo(Cio bag)
            {
                try
                {
                    //string query = $"SELECT * " +
                    //    $"FROM OPOR " +
                    //    $"WHERE U_Outlet + CAST(U_RefNo as nvarchar) = @docNum " +
                    //    $"AND DocType ='I' ";                    

                    string query = $"SELECT * FROM {nameof(OPOR)} WHERE docNum = @docNum AND DocType ='I'";
                    using var conn = new SqlConnection(_dbConnectionStr);

                    // check po with po number
                    var opor = conn.Query<OPOR>(query, new { docNum = bag.QueryDocNumber }).FirstOrDefault();
                    if (opor == null)
                    {
                        return NotFound(bag); // return purchase order not found
                    }

                    // load the po lines
                    bag.DtoPo = opor;

                    query = $"SELECT * FROM {nameof(POR1)} " +
                        $"WHERE DocEntry = @DocEntry " +
                        $"AND LineStatus = 'O'";

                    bag.DtoPoLines = conn.Query<POR1>(query, new { opor.DocEntry }).ToArray();
                    return Ok(bag);
                }
                catch (Exception excep)
                {
                    var br = new AppBadRequest(excep);
                    Log($"{br}", bag);
                    return BadRequest(br);
                }
            }

            /// <summary>
            /// Insert po and it lines
            /// </summary>
            /// <param name="bag"></param>
            IActionResult InsertPOAndLine(Cio bag) // insert into the web portal 
            {
                try
                {
                    if (bag.DtoGrPoAndLines == null)
                    {
                        var br = new AppBadRequest(new Exception("Relevant DTO is null, Please try again later."));
                        Log($"{br}", bag);
                        return BadRequest(br);
                    }

                    if (bag.DtoGrPoAndLines.Length == 0)
                    {
                        var br = new AppBadRequest(new Exception("Relevant DTO is zero, Please try again later."));
                        Log($"{br}", bag);
                        return BadRequest(br);
                    }

                    string cmd = $"INSERT INTO {nameof(AppsGRPO)}(" +
                                    $"CreateUser" +
                                    $",CreateDate" +
                                    $",PONum" +
                                    $",Item" +
                                    $",Quantity" +
                                    $",Variance" +
                                    $",Status" +
                                    //$",SAP" +
                                    //$",OptimisticLockField" +
                                    //$",GCRecord" +
                                    $",ReceiptQtyStatus" +
                                    $",ReceiptQtySummary" +
                                    $",Bin" +
                                    $",PoBaseLine" +
                                    $",PoBaseEntry" +
                                    $",POrefnum" +
                                    $",RefComments" +
                                    $")VALUES(" +
                                    $"@CreateUser" +
                                    $",@CreateDate" +
                                    $",@PONum" +
                                    $",@Item" +
                                    $",@Quantity" +
                                    $",@Variance" +
                                    $",@Status" +
                                    //$",@SAP" +
                                    //$",@OptimisticLockField" +
                                    //$",@GCRecord" +
                                    $",@ReceiptQtyStatus" +
                                    $",@ReceiptQtySummary" +
                                    $",@Bin" +
                                    $",@PoBaseLine" +
                                    $",@PoBaseEntry" +
                                    $",@POrefnum" +
                                    $",@RefComments" +
                                    $")";

                    using (var conn = new SqlConnection(_dbConnectionStr_webPortal))
                    {
                        var result = conn.Execute(cmd, bag.DtoGrPoAndLines);
                        if (result >= 0)
                        {
                            return Ok(bag);
                        }

                        // else 
                        var br = new AppBadRequest(new Exception("Update insert counter data fail, Please try again later."));
                        Log($"{br}", bag);
                        return BadRequest(br);
                    }
                }
                catch (Exception excep)
                {
                    var br = new AppBadRequest(excep);
                    Log($"{br}", bag);
                    return BadRequest(br);
                }
            }

            /// <summary>
            /// Logging error to log
            /// </summary>
            /// <param name="message"></param>
            /// <param name="obj"></param>
            void Log(string message, Cio bag)
            {
                _logger?.LogError(message, bag);
                _fileLogger.WriteLog(message);
            }
        }
    }
}