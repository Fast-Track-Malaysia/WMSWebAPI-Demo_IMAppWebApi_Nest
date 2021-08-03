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
    public class InvntryCntController : ControllerBase
    {

        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030

        readonly IConfiguration _configuration;
        ILogger _logger;

        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;

        public InvntryCntController(IConfiguration configuration, ILogger<GrpoController> logger)
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
                    case "GetInvtryCountDoc":
                        {
                            return GetInvtryCountDoc(bag);
                        }
                    case "GetInvtryCountDocLine":
                        {
                            return GetInvtryCountDocLine(bag);
                        }
                    case "GetBinLocation":
                        {
                            return GetBinLocation(bag);
                        }
                    case "UpdateInventoryCount":
                        {
                            return UpdateInventoryCount(bag);
                        }
                        //case "GetBinLocationAndGetInvtryCountDocLine":
                        //    {
                        //        return GetBinLocationAndGetInvtryCountDocLine(bag);
                        //    }
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
        /// return list of open inventory counting doc
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetInvtryCountDoc(Cio bag)
        {
            try
            {
                using (var iCountDoc = new SQL_OINC(_dbConnectionStr))
                {
                    bag.InvenCountDocs = iCountDoc.GetOpenOinc(bag.OINCStatus);
                    bag.LocationBin = iCountDoc.GetOBINs();
                    _lastErrorMessage = iCountDoc.LastErrorMessage;
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
        /// return list of open inventory counting doc lines
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetInvtryCountDocLine(Cio bag)
        {
            try
            {
                using (var iCountDoc = new SQL_OINC(_dbConnectionStr))
                {
                    bag.InvenCountDocsLines = iCountDoc.GetOincLines(bag.OINCDocEntry);
                    _lastErrorMessage = iCountDoc.LastErrorMessage;
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
        /// return list of bin location
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetBinLocation(Cio bag)
        {
            try
            {
                using (var iCountDoc = new SQL_OINC(_dbConnectionStr))
                {
                    bag.LocationBin = iCountDoc.GetOBINs();
                    _lastErrorMessage = iCountDoc.LastErrorMessage;
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
        /// Update request table and wait middle ware to work to 
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult UpdateInventoryCount(Cio bag)
        {
            try
            {
                using (var updateCount = new SQL_OINC(_dbConnectionStr))
                {
                    var result = updateCount.CreateUpdateInventoryCountRequest(bag.dtoRequest, bag.dtoGRPO);
                    _lastErrorMessage = updateCount.LastErrorMessage;
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
        /// return list of bin location and list of counting litem 
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        //IActionResult GetBinLocationAndGetInvtryCountDocLine(Cio bag)
        //{
        //    try
        //    {
        //        using (var iCountDoc = new SQL_OINC(_dbConnectionStr))
        //        {
        //            bag.InvenCountDocsLines = iCountDoc.GetOincLines(bag.OINCDocEntry);
        //            bag.LocationBin = iCountDoc.GetOBINs();
        //            _lastErrorMessage = iCountDoc.LastErrorMessage;
        //        }

        //        if (_lastErrorMessage.Length > 0)
        //        {
        //            return BadRequest(_lastErrorMessage);
        //        }
        //        return Ok(bag);
        //    }
        //    catch (Exception excep)
        //    {
        //        Log($"{excep}", bag);
        //        return BadRequest($"{excep}");
        //    }
        //}

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