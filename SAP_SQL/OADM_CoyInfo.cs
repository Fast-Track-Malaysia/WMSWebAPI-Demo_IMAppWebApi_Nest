using Dapper;
using DbClass;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WMSWebAPI.Models.SAP_DiApi;
using WMSWebAPI.SAP_DiApi;

namespace WMSWebAPI.Models.Company
{
    public class OADM_CoyInfo : OADM, IDisposable
    {
        string db_conn_sap, dbServerName, dbServerPassword, dbServerUser;
        string databaseConnStr { get; set; } = "";

        public string LastErrorMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Class constructor, starting point
        /// </summary>      
        public OADM_CoyInfo(string dbConnStr)
        {            
            databaseConnStr = dbConnStr;
        }

        /// <summary>
        /// Return string of last error message
        /// Auto property declaration
        /// </summary>
        /// <returns></returns>
        public string GetLastErrorMessage { get; private set; } = "";

        /// <summary>
        /// Return the object of conmpany license 
        /// based on pass in company id / name 
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public zwaLicencse2 GetCompanyLicense(string companyId, string decrytKey)
        {
            try
            {
                zwaLicencse2 resultObj = null;
                using (var userModel = new zwaUserModel())
                {
                    resultObj = userModel.GetCompanyLicense(companyId, decrytKey);
                    GetLastErrorMessage = userModel.lastErrorMessage; // Auto property assignment
                }
                return resultObj;
            }
            catch (Exception excep)
            {
                GetLastErrorMessage = excep.Message;
                return null;
            }
        }

        /// <summary>
        /// Get list of the compnay record from database sap
        /// </summary>
        /// <returns></returns>
        public OADM[] GetCompanyList(IConfiguration _configuration)
        {
            try
            {
                List<OADM> OADMList = null;
                diSAPCompanyModel[] SAPCompanyList = null;
                
                // get SAP server diapi company list 
                // loop each company db connetion to build single OADM
                // add into the array company list and return
                using (var sapObj = new DiApiGetCompanyList(_configuration))
                {
                    GetLastErrorMessage = sapObj.lastErrorMessage;
                    SAPCompanyList = sapObj.companyNameList;
                }

                if (SAPCompanyList == null) return null;
                OADMList = new List<OADM>();

                var dbServerName = _configuration.GetSection("AppSettings").GetSection("AppServer").Value;               
                var dbServerUser = _configuration.GetSection("AppSettings").GetSection("AppDbUser").Value;
                var dbServerPassword = _configuration.GetSection("AppSettings").GetSection("AppDbPw").Value;

                foreach (var db in SAPCompanyList)
                {
                    string connStr = @"Server=" + dbServerName +
                        ";Database=" + db.dbName +
                        ";User Id=" + dbServerUser +
                        ";Password=" + dbServerPassword + ";";

                    string query = "SELECT * FROM " + nameof(OADM);
                    using (var conn = new SqlConnection(connStr))
                    {
                        var OADMCompany = conn.Query<OADM>(query)?.FirstOrDefault();
                        if (OADMCompany != null)
                        {
                            OADMList.Add(OADMCompany);
                        }
                    }
                }

                return OADMList?.ToArray();
            }
            catch (Exception excep)
            {
                GetLastErrorMessage = excep.Message;
                return null;
            }
        }

        /// <summary>
        ///  Dispose code
        /// </summary>
        public void Dispose()
        {
            GC.Collect();
        }
    }
}
