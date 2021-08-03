using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WMSWebAPI.Class;
using WMSWebAPI.Models.BWY;

namespace WMSWebAPI.Controllers.BWY_Impl
{
    [Route("[controller]")]
    [ApiController]
    public class InvtCount_BwyController : ControllerBase
    {
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly string _webportal = "DatabaseWebPortalConn"; // 20200710T0031 for update data into web portal

        readonly IConfiguration _configuration;
        ILogger _logger;

        FileLogger _fileLogger = new FileLogger();
        //string _dbConnectionStr = string.Empty;
        string _dbConnectionStr_webPortal = string.Empty;
        string _lastErrorMessage = string.Empty;

        public InvtCount_BwyController(IConfiguration configuration, ILogger<GrpoController> logger)
        {
            _configuration = configuration;
            //_dbConnectionStr = _configuration.GetConnectionString(_dbName);
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
                    case "QueryInvtryCountList":
                        {
                            return QueryInvtryCountList(bag);
                        }
                    case "InsertInvtryCounter":
                        {
                            return InsertInvtryCounter(bag);
                        }

                    case "QueryItemsExistance":
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
        }

        /// <summary>
        /// 20200726T1800
        /// Use to check the pass in code
        /// is available at web portal
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult QueryItemsExistance (Cio bag)
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
        /// Return list of the open count list
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult QueryInvtryCountList(Cio bag)
        {
            try
            {
                // string query = $"SELECT * FROM {nameof(zwainvtCount)} WHERE Status = 'O'";  
                string query = $"SELECT Distinct " +
                    $"StockCountNum, CreateUser, CreateDate, Outlet " +
                    $"FROM {nameof(AppsStockCounts)} " +
                    $"WHERE Outlet = @InvCountOutLet " +
                    $"AND Status = 'O'";

                using var conn = new SqlConnection(_dbConnectionStr_webPortal);
                bag.DtoAppInvtCountList = conn.Query<AppsStockCounts>(query, new { InvCountOutLet =  bag.InvCountOutLet }).ToArray();

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
        /// Insert Invtry Counter
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult InsertInvtryCounter(Cio bag)
        {
            try
            {
                if (bag.DtoAppInvtCounters == null)
                {
                    var br = new AppBadRequest(new Exception("Relevant DTO is null, Please try again later."));
                    Log($"{br}", bag);
                    return BadRequest(br);
                }

                if (bag.DtoAppInvtCounters.Length == 0)
                {
                    var br = new AppBadRequest(new Exception("Relevant DTO is zero, Please try again later."));
                    Log($"{br}", bag);
                    return BadRequest(br);
                }

                string cmd = $"INSERT INTO AppsStockCounts(" +
                    $"CreateUser" +
                    $",CreateDate" +
                    $",StockCountNum" +
                    $",Item" +
                    $",Quantity" +
                    $",Variance" +
                    $",Bin" +
                    $",Status" +
                    //$",SAP" +
                    //$",OptimisticLockField" +
                    //$",GCRecord" +
                    $",Counter" +
                    $",CountOrder" +
                    $",CountDateTime" +
                    $",Outlet" +
                    $") VALUES (" +
                    $"@CreateUser" +
                    $",@CreateDate" +
                    $",@StockCountNum" +
                    $",@Item" +
                    $",@Quantity" +
                    $",@Variance" +
                    $",@Bin" +
                    $",@Status" +
                    //$",@SAP" +
                    //$",@OptimisticLockField" +
                    //$",@GCRecord" +
                    $",@Counter" +
                    $",@CountOrder" +
                    $",@CountDateTime" +
                    $",@Outlet" +
                    $")";

                using (var conn = new SqlConnection(_dbConnectionStr_webPortal))
                {
                    var result = conn.Execute(cmd, bag.DtoAppInvtCounters);
                    if (result >= 0)
                    {
                        return Ok(bag);
                    }

                    // else 
                    var br = new AppBadRequest(new Exception("Update insert counter data fail, Please try again later."));
                    Log($"{br}", bag);
                    return BadRequest(br);
                }

                //string cmd = $"Insert Into {nameof(zwaInvtCount1)}(" +
                //    $"CounterName" +
                //    $",ParentGuid" +
                //    $",ItemCode" +
                //    $",CountedQty" +
                //    $",TransDate" +
                //    $",OrderId" +
                //    $",OutletId" +
                //    $",Bin" +
                //    $")Values(" +
                //    $"@CounterName" +
                //    $",@ParentGuid" +
                //    $",@ItemCode" +
                //    $",@CountedQty" +
                //    $",@TransDate" +
                //    $",@OrderId" +
                //    $",@OutletId" +
                //    $",@Bin)";

                //using (var conn = new SqlConnection(_dbConnectionStr))
                //{
                //    var result = conn.Execute(cmd, bag.DtoInvtCounters);
                //    if (result >= 0)
                //    {
                //        return Ok(bag);
                //    }

                //    // else 
                //    var br = new AppBadRequest(new Exception("Update insert counter data fail, Please try again later."));
                //    Log($"{br}", bag);
                //    return BadRequest(br);
                //}
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