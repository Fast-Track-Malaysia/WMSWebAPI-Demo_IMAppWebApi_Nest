using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using WMSWebAPI.Class;
using WMSWebAPI.Models;
namespace WMSWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AppUsersSetupController : ControllerBase
    {
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public AppUsersSetupController(IConfiguration configuration, ILogger<GrpoController> logger)
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
        public IActionResult ActionPost(Cio cio)
        {
            try
            {
                _lastErrorMessage = string.Empty;
                switch (cio.request)
                {
                    case "QueryAppUserList":
                        {
                            return QueryAppUserList(cio);
                        }
                    case "AddUserRequest":
                        {
                            return AddUserRequest(cio);
                        }
                    case "UpdateUserRequest":
                        {
                            return UpdateUserRequest(cio);
                        }
                }
                return BadRequest($"Invalid request, please try again later. Thanks");
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Update User Request
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult UpdateUserRequest (Cio cio)
        {
            try
            {                                
                using (var user = new zwaUser(_dbConnectionStr))
                {
                    int createStatus = user.UpdateAppUser(cio);
                    _lastErrorMessage = user.LastErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                // ELSE 
                Log("Sucess", cio);
                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Add User Request
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult AddUserRequest (Cio cio)
        {
            try
            {                                
                using (var user = new zwaUser(_dbConnectionStr))
                {
                    var createStatus = user.CreateAppUser(cio);
                    _lastErrorMessage = user.LastErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                // ELSE 
                Log("Sucess", cio);
                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Admin Query App User List
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult QueryAppUserList (Cio cio)
        {
            try
            {
                // perform the update                
                using (var user = new zwaUser(_dbConnectionStr))
                {
                    cio.zwAppUsers = user.GetCompanyUsersList(cio.companyName); //<--- update the query list
                    _lastErrorMessage = user.LastErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {                    
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                // ELSE 
                Log("Sucess", cio);
                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
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