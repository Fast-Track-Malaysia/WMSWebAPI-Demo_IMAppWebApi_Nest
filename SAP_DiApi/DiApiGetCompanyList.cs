using System;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Configuration;
using WMSWebAPI.Models.SAP_DiApi;

namespace WMSWebAPI.SAP_DiApi
{
    public class DiApiGetCompanyList : IDisposable
    {                
        public diSAPCompanyModel[] companyNameList { get; private set; } // return to the app client
        public string lastErrorMessage { get; private set; } = string.Empty; // record of the error message generated by the code
        
        /// <summary>
        /// Constructor 
        /// Perform setup of the company object
        /// and tried to read company information
        /// </summary>
        public DiApiGetCompanyList(IConfiguration configuration)
        {
            try
            {
                string query =
                   $"SELECT " +
                       $"dbName" +
                       $",cmpName" +
                       $",versStr" +
                       $",dbUser " +
                   $"FROM SRGC";

                var sapCommonDbConnStr = configuration?.GetConnectionString("DatabaseCommonConn");
                // DatabaseWMSConn
                // var sapCommonDbConnStr = configuration?.GetConnectionString("DatabaseWMSConn");
                using var conn = new SqlConnection(sapCommonDbConnStr);
                companyNameList = conn.Query<diSAPCompanyModel>(query).ToArray();               
            }
            catch (Exception excep)
            {
                lastErrorMessage = $"{lastErrorMessage}\n{excep}\n";
            }
        }

        /// <summary>
        /// Disconnect of the sap server
        /// </summary>
        public void Dispose() =>GC.Collect();        
    }

}
