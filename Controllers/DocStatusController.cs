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
    public class DocStatusController : ControllerBase
    {
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly IConfiguration _configuration;
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();

        public DocStatusController(IConfiguration configuration, ILogger<GrpoController> logger)
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
                    case "CheckRequestDocStatus":
                        {
                            return CheckRequestDocStatus(bag);
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
        /// return te zmwRequest object based on the guid
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult CheckRequestDocStatus(Cio bag)
        {
            try
            {
                var lastErrorMessage = string.Empty;
                using (var sql_check = new SQL_DocStatus(_dbConnectionStr))
                {
                    var result = sql_check.GetRequestStatuc(bag.checkDocGuid);
                    lastErrorMessage = sql_check.LastErrorMessage;
                    bag.dtoDocStatus = result;

                    if (result == null) return BadRequest();
                    if (lastErrorMessage.Length > 0)
                    {
                        Log(lastErrorMessage, bag);
                        return BadRequest(lastErrorMessage);
                    }

                    return Ok(bag);
                }
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