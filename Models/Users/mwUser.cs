using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WMSWebAPI.Class;
using WMSWebAPI.Models.Authentcation;

namespace WMSWebAPI.Models.Users
{
    public class mwUser : IDisposable
    {
        /// <summary>
        /// Database table related fields
        /// </summary>
        public zwaUserModel user { get; set; } = null;
        public string lastErrMsg { get; set; } = string.Empty;

        /// <summary>
        /// Class related fields
        /// </summary>

        string databaseConnStr { get; set; } = string.Empty;

        // readonly string userKey = "3s6s9sf@str@ck";

        /// <summary>
        /// Constructor
        /// </summary>
        public mwUser(string dbConnectString)
        {
            try
            {
                var middleManConnStr = dbConnectString; //System.Configuration.ConfigurationManager.AppSettings["DBConnect_SAP"];
                databaseConnStr = middleManConnStr?.ToString();
            }
            catch (Exception exception)
            {
                lastErrMsg = exception.ToString();
            }
        }

        /// <summary>
        /// Return the encryted user password for transmitting among clint and server
        /// </summary>
        /// <returns></returns>
        public string GetEncrytedPw( string seckey)
        {
            if (user == null) return string.Empty;
            
            using (var decrytor = new MD5EnDecrytor())
            {
                return decrytor.Encrypt(user?.password, true, seckey);
            }
        }

        /// <summary>
        /// Return the encryted guid for transmitting among clint and server
        /// </summary>
        /// <returns></returns>
        public string GetEncrytedGUID(string seckey)
        {
            if (user == null) return string.Empty;
            using (var decrytor = new MD5EnDecrytor())
            {
                return decrytor.Encrypt(user?.assigned_token.ToString(), true, seckey);
            }
        }

        /// <summary>
        /// To update the logoff information into the database
        /// </summary>
        /// <returns>return true when update done</returns>
        public bool LogOffUser()
        {
            if (user == null) return false;

            UpdateUserRow(user.sysId, "");
            InsertUserLogonLogs("LOGOFF", "OK", user?.displayName, "WA", "LOGOFF SUCCESS");
            return true;
        }

        /// <summary>
        /// Verify the sent in login information, verify the login info with the database row data
        /// </summary>
        /// <param name="salesCode">3 char sales man code</param>
        /// <param name="pw">sales person password</param>
        /// <returns>true for verified, false for invalid login info</returns>
        public bool VerifyUser(string salesCode, string pw, string secKey)
        {
            try
            {
                // 20200323T1409
                // handle super admin login 
                if (salesCode.ToLower().Equals("superadmin"))
                {
                    string decrytedPw = new MD5EnDecrytor().Decrypt(pw, true, secKey);
                    if (decrytedPw.Equals("firstApp9369"))
                    {
                        this.user = new zwaUserModel();

                        this.user.userIdName = "SuperAdmin";
                        this.user.displayName = "Super Admin";
                        this.user.password = decrytedPw;
                        this.user.sysId = 9369;
                        this.user.assigned_token = Guid.NewGuid();

                        InsertUserLogonLogs("LOGIN", "OK", user?.displayName, "WA", "LOGON SUCCESS");
                        return true;
                    }
                    return false;
                }

                // handler normal user
                // need to change the 
                using (var conn = new SqlConnection(this.databaseConnStr))
                {
                    conn.Open();
                    string query = "SELECT * " +
                        " FROM zwaUser " +
                        " WHERE userIdName = @userIdName ";

                    var param = new { userIdName = salesCode };
                    var user = conn.Query<zwaUserModel>(query, param).FirstOrDefault();

                    if (user == null) return false;
                    string decrytedPw = new MD5EnDecrytor().Decrypt(pw, true, secKey);

                    if (!user.password.Equals(decrytedPw)) return false;
                    if (user.locked.ToLower().Equals("y")) return false;

                    this.user = user;
                    this.user.assigned_token = Guid.NewGuid();
                    UpdateUserRow(user.sysId, this.user.assigned_token.ToString());

                    return true;
                }
            }
            catch (Exception excep)
            {
                lastErrMsg = excep.ToString();
                return false;
            }
        }

        /// <summary>
        /// Update the user row, to indicate the logon success.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="assignedToken"></param>
        /// <returns></returns>
        int UpdateUserRow(int id, string assignedToken)
        {
            try
            {
                string updateSql = "UPDATE zwaUser " +
                    " SET lastLogon = GETDATE() " +
                    " ,assigned_token = @assignedToken " +
                    " WHERE sysId = @id";


                using (var conn = new SqlConnection(databaseConnStr))
                {
                    return conn.Execute(updateSql, new { assignedToken, id });
                }
            }
            catch (Exception excep)
            {
                lastErrMsg = excep.ToString();
                return -1;
            }
        }

        /// <summary>
        /// Insert the login information on the user action success or failure
        /// </summary>
        /// <param name="transName"></param>
        /// <param name="transStatus"></param>
        /// <param name="transUser"></param>
        /// <param name="source"></param>
        /// <param name="remarks_"></param>
        /// <returns></returns>
        int InsertUserLogonLogs(string transName, string transStatus, string transUser, string source, string remarks_)
        {
            try
            {
                string insertLog = " INSERT INTO zwaUserTranLog " +
                            "( transName " +
                            " ,transStatus " +
                            " ,transUser " +
                            " ,transDt " +
                            " ,source " +
                            " ,remarks_) " +
                        " VALUES ( @transName " +
                     ",@transStatus " +
                     ",@transUser " +
                     ",GETDATE() " +
                     ",@source " +
                     ",@remarks_ " +
                     ")";

                using (var conn = new SqlConnection())
                {
                    return conn.Execute(insertLog, new { transName, transStatus, transUser, source, remarks_ });
                }
            }
            catch (Exception excep)
            {
                lastErrMsg = excep.ToString();
                return -1;
            }
        }

        /// <summary>
        /// For dispose the user object
        /// </summary>        
        public void Dispose() => GC.Collect();
    }
}
