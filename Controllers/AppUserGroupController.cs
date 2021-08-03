using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WMSWebAPI.Class;
using WMSWebAPI.Models;
namespace WMSWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AppUserGroupController : ControllerBase
    {
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _lastErrorMessage = string.Empty;

        /// <summary>
        /// controller constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public AppUserGroupController(IConfiguration configuration, ILogger<GrpoController> logger)
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
                // actual handle the cio object
                switch (cio.request)
                {
                    case "QueryAppGroupList":
                        {
                            return QueryAppGroupList(cio);
                        }
                    case "QueryAppGroupUserList":
                        {
                            return QueryAppGroupUserList(cio);
                        }
                    case "QueryAppNotGroupUserList":
                        {
                            return QueryAppNotGroupUserList(cio);
                        }
                    case "QueryAppUserGroupPermissionList":
                        {
                            return QueryAppUserGroupPermissionList(cio);
                        }
                    case "AddGroupRequest":
                        {
                            return AddGroupRequest(cio);
                        }
                    case "UpdateGroupRequest":
                        {
                            return UpdateGroupRequest(cio);
                        }
                    case "ResetUserGroupToDefault":
                        {
                            return ResetUserGroupToDefault(cio);
                        }
                    case "GetTempGroupIdFromSvr":
                        {
                            return GetTempGroupIdFromSvr(cio);
                        }
                    case "GetLastErrorMessage":
                        {
                            return BadRequest(_lastErrorMessage);
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
        /// Get Temp Group Id From Svr
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult GetTempGroupIdFromSvr (Cio cio)
        {
            try
            {
                using (var group = new zwaUserGroup(_dbConnectionStr))
                {
                    cio.newUserGroupTempId = group.GetTempGroupIdFromSvr();
                    _lastErrorMessage = group.GetErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }
                Log("Success", cio);
                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Reset User Group To Default
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult ResetUserGroupToDefault(Cio cio)
        {
            try
            {
                int isReset = -1;
                using (var group = new zwaUserGroup(_dbConnectionStr))
                {
                    isReset = group.ResetUserGroupToDefault(cio.zwAppUsers);
                    _lastErrorMessage = group.GetErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                Log("Success", cio);
                return Ok(cio);

            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Update Group Request
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult UpdateGroupRequest (Cio cio)
        {
            try
            {
                int isCreated = -1;
                using (var group = new zwaUserGroup(_dbConnectionStr))
                {
                    isCreated = group.UpdateGroup(cio.newUserGroup, cio.newUserGroupPermission, cio.newUserGroupUsr);
                    _lastErrorMessage = group.GetErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                Log("Success", cio);
                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Add Group Request
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult AddGroupRequest (Cio cio)
        {
            try
            {
                int isCreated = -1;
                using (var group = new zwaUserGroup(_dbConnectionStr))
                {
                    isCreated = group.InsertNewGroup(cio.newUserGroup, cio.newUserGroupPermission, cio.newUserGroupUsr);
                    _lastErrorMessage = group.GetErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                Log("Success", cio);
                return Ok(cio);

            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Query App User Group Permission List
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult QueryAppUserGroupPermissionList (Cio cio)
        {
            try
            {
                string appName = _configuration.GetSection("AppSettings").GetSection("AppName").Value;
                using (var userGroup = new zwaUserGroup(_dbConnectionStr))
                {
                    cio.zwaUserGroupsPermission = userGroup.GetUserGroupPermission(cio.companyName, cio.groupId, appName);
                    if (cio.zwaUserGroupsPermission.Length == 0) // added to counter new group
                    {
                        cio.zwaUserGroupsPermission = userGroup.QueryUserGrouPermissionTemplate();
                    }
                    _lastErrorMessage = userGroup.GetErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                Log("Success", cio);
                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Query App Not Group User List
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult QueryAppNotGroupUserList(Cio cio)
        {
            try
            {
                using (var user = new zwaUser(_dbConnectionStr))
                {
                    cio.zwAppUsers = user.GetNotGroupUsers(cio.companyName, cio.groupId);
                    _lastErrorMessage = user.LastErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                Log("Success", cio);
                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Query App Group User List
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult QueryAppGroupUserList(Cio cio)
        {
            try
            {
                using (var user = new zwaUser(_dbConnectionStr))
                {
                    cio.zwAppUsers = user.GetGroupUsers(cio.companyName, cio.groupId);
                    _lastErrorMessage = user.LastErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                Log("Success", cio);
                return Ok(cio);
            }
            catch (Exception excep)
            {
                Log($"{excep}", cio);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Query App Group List
        /// </summary>
        /// <param name="cio"></param>
        /// <returns></returns>
        IActionResult QueryAppGroupList (Cio cio)
        {
            try
            {
                using (var groupObj = new zwaUserGroup(_dbConnectionStr))
                {
                    cio.zwaGroupList = groupObj.GetCompanyGroup(cio.companyName);
                    _lastErrorMessage = groupObj.GetErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {                    
                    Log(_lastErrorMessage, cio);
                    return BadRequest(_lastErrorMessage);
                }

                Log("Success", cio);
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