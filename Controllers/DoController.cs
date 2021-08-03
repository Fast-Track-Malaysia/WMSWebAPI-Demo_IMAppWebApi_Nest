using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using WMSWebAPI.Class;
using WMSWebAPI.SAP_SQL;
namespace WMSWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]    
    public class DoController : ControllerBase
    {        
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;

        /// <summary>
        /// Controller constructore
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public DoController(IConfiguration configuration, ILogger<GrpoController> logger)
        {
            _configuration = configuration;
            _dbConnectionStr = _configuration.GetConnectionString(_dbName);
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
                    //case "GetOpenSo":
                    //    {
                    //        return GetBpWithOpenPo(bag);
                    //    }
                    case "GetOpenSo":
                        {
                            return GetOpenSo(bag);
                        }
                    case "GetOpenSoLines":
                        {
                            return GetOpenSoLines(bag);
                        }
                    case "CreateDoRequest":
                        {
                            return CreateDoRequest(bag);
                        }
                    case "GetItemQtyWarehouse":
                        {
                            return GetItemQtyWarehouse(bag);
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
        /// Handler insert the request for the middle ware to create the GRPO
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetOpenSo(Cio bag)
        {
            try
            {
                using (var so = new SQL_ORDR(_dbConnectionStr))
                {
                    bag.So = so.GetOpenSo(bag.getSoType);
                    _lastErrorMessage = so.LastErrorMessage;
                }

                if (_lastErrorMessage.Length > 0)
                {
                    return BadRequest(_lastErrorMessage);
                }
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }
        /// <summary>
        /// Based on the PO doc entries (int array)
        /// Query and return it line
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetOpenSoLines(Cio bag)
        {
            try
            {
                using (var so = new SQL_ORDR(_dbConnectionStr))
                {
                    bag.SoLines = so.GetOpenSoLines(bag.soDocEntry);
                    _lastErrorMessage = so.LastErrorMessage;
                }

                if (_lastErrorMessage.Length > 0)
                {
                    return BadRequest(_lastErrorMessage);
                }
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Handler insert the request for the middle ware to create the GRPO
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult CreateDoRequest(Cio bag)
        {
            try
            {
                // insert the request with GUID
                // insert the OPOR_Ex table
                // insert the POR1_Ex table
                // ask refres the GRPO list for line disapper

                using (var delivery = new SQL_ORDR(_dbConnectionStr))
                {
                    var result = delivery.CreateSoRequest(bag.dtoRequest, bag.dtoDeliveryOrder);
                    _lastErrorMessage = delivery.LastErrorMessage;
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
        /// 20200616T1832
        /// Based on the supplied itemcode, return list of array for the warehouse that having the qty
        /// for user to select from the app
        /// </summary>
        /// <param name="itemCode"></param>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetItemQtyWarehouse(Cio bag)
        {
            try
            {
                using (var delivery = new SQL_ORDR(_dbConnectionStr))
                {
                    var result = delivery.GetItemQtyWarehouse(bag.checkedItemCodeWhsQty);
                    _lastErrorMessage = delivery.LastErrorMessage;
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