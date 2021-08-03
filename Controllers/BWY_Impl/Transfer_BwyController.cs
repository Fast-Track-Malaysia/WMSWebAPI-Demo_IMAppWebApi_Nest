using Dapper;
using DbClass;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;
using WMSWebAPI.Class;
using WMSWebAPI.Models.BWY;

namespace WMSWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class Transfer_BwyController : ControllerBase
    {
        //readonly string _dbName = "DatabaseConn";
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly string _webportal = "DatabaseWebPortalConn"; // 20200710T0031 for update data into web portal

        readonly IConfiguration _configuration;
        ILogger _logger;

        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _dbConnectionStr_webPortal = string.Empty;
        string _lastErrorMessage = string.Empty;

        public Transfer_BwyController(IConfiguration configuration, ILogger<GrpoController> logger)
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
                _lastErrorMessage = string.Empty;
                switch (bag.request)
                {
                    case "GetTransferDocAndLines":
                        {
                            return GetTransferDocAndLines(bag);
                        }
                    case "InsertTransferDocAndLines":
                        {
                            return InsertTransferDocAndLines(bag);
                        }
                    case "QueryItemsExistance":
                        {
                            return QueryItemsExistance(bag);
                        }
                }
                return BadRequest($"Invalid request, please try again later. Thanks");
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
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
        /// Check valid transfer doc and return it doc line
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetTransferDocAndLines(Cio bag)
        {
            try
            {
                // 20200728
                // user will scan the sap inventry request doc numnber
                // code check the SAP database to get the inventory transfer doc number 
                // previuos code
                // string query = "SELECT * FROM OWTR WHERE docNum = @docNum";
                // SELECT * FROM OPOR WHERE DocStatus ='O' AND DocType = 'I'

                // 20200730T1117
                // add in the filler as outlet message
                //string query = 
                //    $"SELECT T1.* " +
                //    $"FROM " +
                //    $"OWTQ T0 INNER JOIN OWTR T1 ON T0.U_STReqDoc = T1.U_STReqDoc " +
                //    $"WHERE T0.DocNum = @docNum " +
                //    $"AND T0.ToWhsCode = @requestOutlet ";

                string query = $"SELECT t1.*" +
                    $"FROM " +
                    $"OWTQ T0 INNER JOIN OWTR T1 ON T0.U_STReqDoc = T1.U_STReqDoc " +
                    $"WHERE T0.DocNum = @docNum " +
                    $"AND T0.ToWhsCode = @requestOutlet";

                using var conn = new SqlConnection(_dbConnectionStr);
                // check po with po number
                var transfer = conn.Query<OWTR>(query, 
                    new 
                    { 
                        docNum = bag.QueryDocNumber,
                        requestOutlet = bag.CurrentIBTRequestGroupName
                    }).FirstOrDefault();

                if (transfer == null)
                {
                    return NotFound(bag); // return purchase order not found
                }

                // 20200721T2206
                // check doc already received or not 
                // connect to web portal to check does the doc already process
                // if processed, then return no found
                using var connWebPortal = new SqlConnection(_dbConnectionStr_webPortal);
                var appsIBTIn = connWebPortal.Query<AppsIBTIn>($"SELECT * FROM {nameof(AppsIBTIn)} WHERE IBTNum = @DocNum",
                    new { transfer.DocNum }).FirstOrDefault();

                if (appsIBTIn != null)
                {
                    return NotFound(bag);
                }

                // load the po lines
                bag.DtoTransferDoc = transfer;

                query = "SELECT * FROM WTR1 " +
                    "WHERE DocEntry = @DocEntry " +
                    "AND LineStatus = 'O'";

                bag.DtoTransferDocLines = conn.Query<WTR1>(query, new { transfer.DocEntry }).ToArray();
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
        /// Insert Transfer Doc AndLines
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult InsertTransferDocAndLines(Cio bag)
        {
            try
            {
                if (bag.DtoIBTInAndLines == null)
                {
                    var br = new AppBadRequest(new Exception("Relevant DTO is null, Please try again later."));
                    Log($"{br}", bag);
                    return BadRequest(br);
                }

                if (bag.DtoIBTInAndLines.Length == 0)
                {
                    var br = new AppBadRequest(new Exception("Relevant DTO is zero, Please try again later."));
                    Log($"{br}", bag);
                    return BadRequest(br);
                }

                string insert = $"INSERT INTO {nameof(AppsIBTIn)}(" +
                               $"CreateUser" +
                               $",CreateDate" +
                               $",IBTNum" +
                               $",Item" +
                               $",Quantity" +
                               $",Variance" +
                               $",Status" +
                               //$",SAP" +
                               //$",OptimisticLockField" +
                               //$",GCRecord" +
                               $",IBTBaseLine" +
                               $",IBTBaseEntry" +
                               $",Bin" +
                               $",QtyStatus" +
                               $",QtyStatusSummary" +
                               $",RowSummary" +
                               $",RefComments" +
                               $") VALUES (" +
                               $"@CreateUser" +
                               $",@CreateDate" +
                               $",@IBTNum" +
                               $",@Item" +
                               $",@Quantity" +
                               $",@Variance" +
                               $",@Status" +
                               //$",@SAP" +
                               //$",@OptimisticLockField" +
                               //$",@GCRecord" +
                               $",@IBTBaseLine" +
                               $",@IBTBaseEntry" +
                               $",@Bin" +
                               $",@QtyStatus" +
                               $",@QtyStatusSummary" +
                               $",@RowSummary" +
                               $",@RefComments" +
                               $")";

                using (var conn = new SqlConnection(_dbConnectionStr_webPortal))
                {
                    var result = conn.Execute(insert, bag.DtoIBTInAndLines);
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