using DbClass;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using WMSWebAPI.Class;
using WMSWebAPI.SAP_SQL;

namespace WMSWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly IConfiguration _configuration;
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;

        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();

        public ItemsController(IConfiguration configuration, ILogger<GrpoController> logger)
        {
            _configuration = configuration;
            _dbConnectionStr = _configuration.GetConnectionString(_dbName);
            _logger = logger;
        }

        public string LastErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose() => GC.Collect();

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
                    case "GetItems":
                        {
                            return GetItems(bag);
                        }
                    case "GetItem":
                        {
                            return GetItem(bag);
                        }
                    case "CreateGoodsReceiveRequest":
                        {
                            return CreateGoodsReceiveRequest(bag);
                        }

                    case "CreateGoodsIssueRequest":
                        {
                            return CreateGoodsIssueRequest(bag);
                        }
                    case "GetGoodsReceiptPriceList": // 20200623T1048 
                        {
                            return GetGoodsReceiptPriceList(bag);
                        }
                    case "UpdateGoodsReceiptPriceListId": // 20200623T1054
                        {
                            return UpdateGoodsReceiptPriceListId(bag);
                        }
                    case "GetGoodsReceiptDocSeries":
                        {
                            return GetGoodsReceiptDocSeries(bag);
                        }
                    case "GetGoodsIssuesDocSeries":
                        {
                            return GetGoodsIssuesDocSeries(bag);
                        }
                    case "UpdateGoodsReceiptDocSeries":
                        {
                            return UpdateGoodsReceiptDocSeries(bag);
                        }
                    case "UpdateGoodsIssuesDocSeries":
                        {
                            return UpdateGoodsIssuesDocSeries(bag);
                        }
                    case "GetGoodsIssuesPriceList":
                        {
                            return GetGoodsIssuesPriceList(bag);
                        }

                    case "UpdateGoodsIssuesPriceListId":
                        {
                            return UpdateGoodsIssuesPriceListId(bag);
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
        /// GetGoodsIssuesPriceList
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetGoodsIssuesPriceList (Cio bag)
        {
            try
            {
                using (var pricelist = new SQL_OPLN(_dbConnectionStr))
                {
                    bag.PriceList = pricelist.GetPriceList();
                    bag.ExistingGiPriceListId = pricelist.GetExistingPriceList_GI();
                    LastErrorMessage = pricelist.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }
                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }


        /// <summary>
        /// UpdateGoodsIssuesPriceListId
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult UpdateGoodsIssuesPriceListId (Cio bag)
        {
            try
            {
                using (var pricelist = new SQL_OPLN(_dbConnectionStr))
                {
                    var result = pricelist.UpdateSelectedGiPriceList(bag.UpdateGiPriceListId);
                    LastErrorMessage = pricelist.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }
                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }


        /// <summary>
        /// return list if the item master
        /// </summary>
        /// <param name="Bag"></param>
        /// <returns></returns>
        IActionResult GetItems (Cio bag)
        {
            try
            {
                using (var items = new SQL_OITM(_dbConnectionStr))
                {
                    bag.Items = items.GetItems();
                    LastErrorMessage = items.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }

                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Get Single item by item code
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetItem(Cio bag)
        {
            try
            {
                using (var items = new SQL_OITM(_dbConnectionStr))
                {
                    bag.Item = items.GetItem(bag.QueryItemCode);
                    LastErrorMessage = items.LastErrorMessage;
                }
                
                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }
                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Create Goods IssueRequest
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult CreateGoodsIssueRequest (Cio bag)
        {
            try
            {
                // insert the request with GUID
                // insert the OPOR_Ex table
                // insert the POR1_Ex table
                // ask refres the GRPO list for line disapper

                using (var goodIssue = new SQL_OIGE(_dbConnectionStr))
                {
                    var result = goodIssue.CreateGoodsIssuesRequest(bag.dtoRequest, bag.dtoGRPO);
                    _lastErrorMessage = goodIssue.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    return Ok(bag);
                }

                return BadRequest(_lastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Create Goods Receive Request
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult CreateGoodsReceiveRequest (Cio bag)
        {
            try
            {
                // insert the request with GUID
                // insert the OPOR_Ex table
                // insert the POR1_Ex table
                // ask refres the GRPO list for line disapper

                using (var goodReceive = new SQL_OIGN(_dbConnectionStr))
                {
                    var result = goodReceive.CreateGoodsReceiveRequest(bag.dtoRequest, bag.dtoGRPO);
                    _lastErrorMessage = goodReceive.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    return Ok(bag);
                }

                return BadRequest(_lastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Get Goods Receipt Price List
        /// </summary>
        /// <returns></returns>
        IActionResult GetGoodsReceiptPriceList (Cio bag)
        {
            try
            {
                using (var pricelist = new SQL_OPLN(_dbConnectionStr))
                {
                    bag.PriceList = pricelist.GetPriceList();
                    bag.ExistingGrPriceListId = pricelist.GetExistingPriceList();
                    LastErrorMessage = pricelist.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }
                return BadRequest(LastErrorMessage);            
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Update Gr Price List Id
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult UpdateGoodsReceiptPriceListId(Cio bag)
        {
            try
            {
                using (var pricelist = new SQL_OPLN(_dbConnectionStr))
                {
                    var result =  pricelist.UpdateSelectedGrPriceList(bag.UpdateGrPriceListId);                    
                    LastErrorMessage = pricelist.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }
                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Update Goods Receipt Doc Series
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult UpdateGoodsReceiptDocSeries (Cio bag)
        {
            try
            {
                using (var goodsReceipt = new SQL_OIGN(_dbConnectionStr))
                {
                    var result = goodsReceipt.UpdateGoodsReceiptDocSeries(bag.UpdateGrDocSeries);
                    LastErrorMessage = goodsReceipt.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }
                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult UpdateGoodsIssuesDocSeries(Cio bag)
        {
            try
            {
                using (var goodIssues = new SQL_OIGE(_dbConnectionStr))
                {
                    var result = goodIssues.UpdateGoodsIssuesDocSeries(bag.UpdateIssueDocSeries);
                    LastErrorMessage = goodIssues.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }
                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Get Goods Receipt Doc Series
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetGoodsReceiptDocSeries (Cio bag)
        {
            try
            {
                using (var goodsReceipt = new SQL_OIGN(_dbConnectionStr))
                {
                    bag.ExistingGrDocSeries = goodsReceipt.GetGoodsReceiptDocSeries();
                    LastErrorMessage = goodsReceipt.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }

                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Get Goods Receipt Doc Series
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetGoodsIssuesDocSeries(Cio bag)
        {
            try
            {
                using (var goodsReceipt = new SQL_OIGE(_dbConnectionStr))
                {
                    bag.ExistingGIDocSeries = goodsReceipt.GetGoodsIssuesDocSeries();
                    LastErrorMessage = goodsReceipt.LastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(LastErrorMessage))
                {
                    return Ok(bag);
                }

                return BadRequest(LastErrorMessage);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
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