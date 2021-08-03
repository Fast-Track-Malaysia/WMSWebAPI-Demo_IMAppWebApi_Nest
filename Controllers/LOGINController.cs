using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using WMSWebAPI.Class;
using WMSWebAPI.Models;
using WMSWebAPI.Models.Users;

namespace WMSWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LOGINController : ControllerBase
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
        public LOGINController(IConfiguration configuration, ILogger<GrpoController> logger)
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
                switch (bag.request)
                {
                    case "Login":
                        {
                            return ProcessLogin(bag);                            
                        }
                    case "ResetPassword":  // 20200716T2209, for app user to reset it own password
                        {
                            return ResetPassword(bag);
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
        /// App Login process after token generation
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult ProcessLogin(Cio cio)
        {
            try
            {
                // 20200409T0958 load the usr permission here 
                var appName = _configuration.GetSection("AppSettings").GetSection("AppName").Value;   
                var secKey = _configuration.GetSection("AppSettings").GetSection("Secret").Value;
                var user = new mwUser(_dbConnectionStr);

                bool isVerify = user.VerifyUser(cio.sap_logon_name, cio.sap_logon_pw, secKey);
                if (!isVerify)
                {
                    Log($"User login fail \n{user.lastErrMsg}", cio);
                    return BadRequest($"User login fail \n{user.lastErrMsg}");
                }

                using (var userGroup = new zwaUserGroup(_dbConnectionStr))
                {
                    if (cio.sap_logon_name.ToLower().Equals("superadmin"))
                    {
                        cio.currentPermissions = userGroup.GetUserGroupPermission(cio.companyName, -1, appName); // get full permission template
                        cio.currentGroup = userGroup.GetCompanyGroup(cio.companyName, 1);
                    }
                    else
                    {                        
                        int groupId = userGroup.GetUserGroupId(user.user.sysId);
                        cio.currentPermissions = userGroup.GetUserGroupPermission(user.user.companyId, groupId, appName);
                        cio.currentGroup = userGroup.GetCompanyGroup(user.user.companyId, groupId);
                    }
                }
                
                cio.token = user.GetEncrytedGUID(secKey);
                cio.sap_logon_name = user.user.userIdName;
                cio.sap_logon_pw = user.GetEncrytedPw(secKey);
                cio.currentUser = user.user.displayName;
                cio.currentUserRole = user.user.roleDesc;

                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Reset user password by user thenself
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult ResetPassword (Cio bag)
        {
            try
            {
                var secKey = _configuration.GetSection("AppSettings").GetSection("Secret").Value;
                var newPw = new MD5EnDecrytor().Decrypt(bag.NewEncrptedPw, true, secKey);

                var sqlUpdate = $"UPDATE {nameof(zwaUser)} " +
                    $"SET password = @NewPassword " +
                    $"WHERE userIdName = @UserIdName";

                using (var conn = new SqlConnection(_dbConnectionStr))
                {
                    var result = conn.Execute(sqlUpdate, new { NewPassword = newPw, UserIdName = bag.sap_logon_name });
                    if (result <= 0 )
                    {
                        var excep = new Exception($"User id name:{bag.sap_logon_name} reset password fail");                    
                        Log($"{excep}", bag);
                        return BadRequest($"{excep}");
                    }
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