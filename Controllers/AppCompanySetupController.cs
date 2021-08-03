using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WMSWebAPI.Class;
using WMSWebAPI.Models.Company;
using WMSWebAPI.Opr;
namespace WMSWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AppCompanySetupController : ControllerBase
    {
        readonly string _dbName = "DatabaseWMSConn"; // 20200612T1030
        readonly string _dbCommon = "DatabaseCommonConn";
        readonly IConfiguration _configuration;
        ILogger _logger;
        FileLogger _fileLogger = new FileLogger();
        string _dbConnectionStr = string.Empty;
        string _commonDbConnStr = string.Empty;

        string _lastErrorMessage = string.Empty;

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        public AppCompanySetupController(IConfiguration configuration, ILogger<GrpoController> logger)
        {
            _configuration = configuration;
            _dbConnectionStr = _configuration.GetConnectionString(_dbName);
            _commonDbConnStr = _configuration.GetConnectionString(_dbCommon);
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
                    case "QueryCompanyLicenseInfo":
                        {
                            return QueryCompanyLicenseInfo(bag);
                        }
                    case "QueryAppCompany":
                        {
                            return QueryAppCompany(bag);                            
                        }
                    case "UpdateAppCompany":
                        {
                            return UpdateAppCompany(bag);
                        }
                    case "GetOnholdTriedRequest":
                        {
                            return GetOnholdTriedRequest(bag);
                        }
                    case "ResetRequest":
                        {
                            return ResetRequest(bag);
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
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Reset Request
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult ResetRequest (Cio bag)
        {
            try
            {
                using (var middleMan = new MiddleManRequest())
                {
                    middleMan.ResetRequest(bag.ProblemsRequest);
                    _lastErrorMessage = middleMan.GetLastErrorMessage;

                    // new list
                    bag.ProblemsRequest = middleMan.GetOnHoldRequest();
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, bag);
                    return BadRequest(_lastErrorMessage);
                }

                Log("Succcess", bag);
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Get Onhold Tried Request
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult GetOnholdTriedRequest (Cio bag)
        {
            try
            {
                using (var middleMan = new MiddleManRequest())
                {
                    bag.ProblemsRequest = middleMan.GetOnHoldRequest();
                    _lastErrorMessage = middleMan.GetLastErrorMessage;
                }

                if (!string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, bag);
                    return BadRequest(_lastErrorMessage);
                }

                Log($"Success", bag);                
                return Ok(bag);
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Update App Company
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult UpdateAppCompany (Cio bag)
        {
            try
            {
                var editedComp = bag.zwaCompanyNewEdit;
                if (editedComp == null)
                {
                    _lastErrorMessage = "Edited company object empty, Please try again later. Thanks";
                    Log($"{_lastErrorMessage}", bag);
                    return BadRequest($"{_lastErrorMessage}");
                }

                // perform the update
                using (var company = new zwaCompany())
                {
                    if (bag.keys?.Length > 0)
                    {
                        // perform a update into the zwaLicense
                        editedComp.licenseKey = bag.keys;
                        company.UpdateCompanyLicense(editedComp);
                        _lastErrorMessage = company.GetErrorMessage();
                        return Ok(bag);
                    }
                    

                    if (_lastErrorMessage.Length >0)
                    {
                        _lastErrorMessage = company.GetErrorMessage();
                        Log($"{_lastErrorMessage}", bag);
                        return BadRequest($"{_lastErrorMessage}");
                    }
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
        /// Query Company License Info
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult QueryCompanyLicenseInfo (Cio bag)
        {
            try
            {
                var deKey = _configuration.GetSection("AppSettings").GetSection("DecrytKey").Value;

                using (var company = new OADM_CoyInfo(_dbConnectionStr))
                {
                    bag.compamyLic = company.GetCompanyLicense(bag.companyName, deKey);
                    _lastErrorMessage = company.GetLastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, bag);
                    return Ok(bag);
                }

                Log(_lastErrorMessage, bag);
                return BadRequest($"{_lastErrorMessage}");
            }
            catch (Exception excep)
            {
                Log($"{excep}", bag);
                return BadRequest($"{excep}");
            }
        }

        /// <summary>
        /// Query App Company
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        IActionResult QueryAppCompany(Cio bag)
        {
            try
            {
                //using (var company = new OADM_CoyInfo(_dbConnectionStr))

                using (var company = new OADM_CoyInfo(_commonDbConnStr)) // connect to SAP common database
                {
                    bag.oADM_CompanyInfoList = company.GetCompanyList(_configuration);
                    _lastErrorMessage = company.GetLastErrorMessage;
                }

                if (string.IsNullOrWhiteSpace(_lastErrorMessage))
                {
                    Log(_lastErrorMessage, bag);
                    return Ok(bag);
                }

                Log(_lastErrorMessage, bag);
                return BadRequest($"{_lastErrorMessage}");
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