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
    public class TransferRequestController : ControllerBase
    {
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030

        readonly IConfiguration _configuration;
        ILogger _logger;

        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;

        public TransferRequestController(IConfiguration configuration, ILogger<GrpoController> logger)
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
                    //case "GetTransferRequestList":
                    //    {
                    //        return GetTransferRequestList(bag);
                    //    }
                    //case "GetTransferRequestLine":
                    //    {
                    //        return GetTransferRequestLine(bag);
                    //    }
                    case "CheckItemCodeExistance":
                        {
                            return CheckItemCodeExistance(bag);
                        }
                    case "CreateInventoryRequest":
                        {
                            return CreateInventoryRequest(bag);
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
        /// Handler create inventpry request
        /// </summary>
        /// <returns></returns>
        IActionResult CreateInventoryRequest (Cio bag)
        {
            using (var inventoryRequest = new SQL_OWTQ(_dbConnectionStr))
            {
                var result = inventoryRequest.CreateInventoryRequest(bag.dtoRequest, bag.dtoInventoryRequest, bag.dtoInventoryRequestHead);
                _lastErrorMessage = inventoryRequest.LastErrorMessage;
            }

            if (string.IsNullOrWhiteSpace(_lastErrorMessage))
            {
                return Ok(bag);
            }

            return BadRequest(_lastErrorMessage);
        }

        /// <summary>
        /// Check the item code exist in sap database
        /// </summary>
        /// <returns></returns>
        IActionResult CheckItemCodeExistance(Cio bag)
        {
            try
            {
                using (var oitm = new SQL_OWTQ(_dbConnectionStr))
                {
                    bag.Item = oitm.CheckItemCodeExist(bag.QueryItemCode);
                    _lastErrorMessage = oitm.LastErrorMessage;
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
        /// Get Transfer RequestList
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        //IActionResult GetTransferRequestList(Cio bag)
        //{
        //    try
        //    {
        //        using (var grpo = new SQL_OWTQ(_dbConnectionStr))
        //        {
        //            bag.TransferRequestList = grpo.GetTransferRequestList();
        //            _lastErrorMessage = grpo.LastErrorMessage;
        //        }

        //        if (string.IsNullOrWhiteSpace(_lastErrorMessage))
        //        {
        //            return Ok(bag);
        //        }

        //        return BadRequest(_lastErrorMessage);
        //    }
        //    catch (Exception excep)
        //    {
        //        Log($"{excep}", bag);
        //        return BadRequest($"{excep}");
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        //IActionResult GetTransferRequestLine(Cio bag)
        //{
        //    try
        //    {
        //        using (var grpo = new SQL_OWTQ(_dbConnectionStr))
        //        {
        //            bag.TransferRequestLine = grpo.GetTransferRequestLines(bag.TransferRequestDocEntry);
        //            _lastErrorMessage = grpo.LastErrorMessage;
        //        }

        //        if (string.IsNullOrWhiteSpace(_lastErrorMessage))
        //        {
        //            return Ok(bag);
        //        }

        //        return BadRequest(_lastErrorMessage);
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
