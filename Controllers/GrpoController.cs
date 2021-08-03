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
    public class GrpoController : ControllerBase
    {
        //readonly string _dbName = "DatabaseConn";
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030

        readonly IConfiguration _configuration;
        ILogger _logger;

        FileLogger _fileLogger = new FileLogger();        
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;

        public GrpoController(IConfiguration configuration, ILogger<GrpoController> logger)
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
                    case "GetBpWithOpenPo":
                        {
                            return GetBpWithOpenPo(bag);
                        }
                    case "GetWarehouseList":
                        {
                            return GetWarehouseList(bag);
                        }
                    case "GetOpenPo":
                        {
                            return GetOpenPo(bag);
                        }
                    case "GetOpenPoLines":
                        {
                            return GetOpenPoLines(bag);
                        }                   
                    case "CreateGRPORequest":
                        {
                            return CreateGRPORequest(bag);
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
        /// Return the result of Get Biz Partner With Open PO
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult GetBpWithOpenPo(Cio bag)
        {
            try
            {
                using (var po = new SQL_OPOR(_dbConnectionStr))
                {
                    bag.PoBp = po.GetBpWithOpenPo();
                    _lastErrorMessage = po.LastErrorMessage;
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
        /// Get Warehouse List
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetWarehouseList(Cio bag)
        {
            try
            {
                using (var po = new SQL_OPOR(_dbConnectionStr))
                {                   
                    bag.dtoWhs = po.GetWarehouses(); // load the warehouse here
                    _lastErrorMessage = po.LastErrorMessage;
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
        ///  return list of PO by card code array
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetOpenPo(Cio bag)
        {
            try
            {
                using (var po = new SQL_OPOR(_dbConnectionStr))
                {
                    bag.Po = po.GetOpenPo(bag.getPoType);
                    //bag.dtoWhs = po.GetWarehouses(); // load the warehouse here

                    _lastErrorMessage = po.LastErrorMessage;
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
        IActionResult GetOpenPoLines(Cio bag)
        {
            try
            {
                using (var po = new SQL_OPOR(_dbConnectionStr))
                {
                    bag.PoLines = po.GetOpenPoLines(bag.poDocEntry);
                    _lastErrorMessage = po.LastErrorMessage;
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
        /// Return list of the warehouse from the database
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetEntryWhsCodeList (Cio bag)
        {
            try
            {
                using (var po = new SQL_OPOR(_dbConnectionStr))
                {
                    bag.dtoWhs = po.GetWarehouses();
                    _lastErrorMessage = po.LastErrorMessage;
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
        IActionResult CreateGRPORequest(Cio bag)
        {
            try
            {
                // insert the request with GUID
                // insert the OPOR_Ex table
                // insert the POR1_Ex table
                // ask refres the GRPO list for line disapper

                using (var grpo = new SQL_OPOR(_dbConnectionStr))
                {
                    var result = grpo.CreateGRPORequest(bag.dtoRequest, bag.dtoGRPO);
                    _lastErrorMessage = grpo.LastErrorMessage;
                }

                if(string.IsNullOrWhiteSpace(_lastErrorMessage))
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