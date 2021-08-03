using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WMSWebAPI.Class;
using WMSWebAPI.SAP_SQL;

namespace WMSWebAPI.Controllers
{    
    [Route("[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        //readonly string _dbName = "DatabaseConn";
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030

        readonly IConfiguration _configuration;
        ILogger _logger;

        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;

        public WarehousesController(IConfiguration configuration, ILogger<GrpoController> logger)
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
                    case "GetWarehouseList":
                        {
                            return GetWarehouseList(bag);
                        }
                    case "GetWarehouseBins": // 20200718T1023 for bin location
                        {
                            return GetWarehouseBins(bag);
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
        /// Get Warehouse List
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetWarehouseList(Cio bag)
        {
            try
            {
                using (var whs = new SQL_OWHS(_dbConnectionStr))
                {
                    bag.dtoWhs = whs.GetWarehouses(); // load the warehouse here     
                    bag.DtoBins = whs.GetWarehousesObin(); // load all bin location to app
                    _lastErrorMessage = whs.LastErrorMessage;
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
        /// Return list of obin for the warehouse
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetWarehouseBins(Cio bag)
        {
            try
            {
                using (var whs = new SQL_OWHS(_dbConnectionStr))
                {
                    bag.DtoBins = whs.GetWarehousesObin(bag.QueryWhs); // load the warehouse here                    
                    _lastErrorMessage = whs.LastErrorMessage;
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
