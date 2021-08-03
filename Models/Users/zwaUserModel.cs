using Dapper;
using Newtonsoft.Json;
using SAPbobsCOM;
using System;
using System.Data.SqlClient;
using System.Linq;
using WMSWebAPI.Class;
namespace WMSWebAPI.Models
{
    enum EnumRoles
    {
        superAdmin = 4,
        admin = 3,
        user = 2,
        guest = 1,
        anonymous = 0
    }

    /// <summary>
    /// Class to simulate the login user, password and set grant type
    /// </summary>
    public class zwaUserModel : IDisposable
    {
        public int sysId { get; set; }
        public string companyId { get; set; }
        public string userIdName { get; set; }
        public string password { get; set; }
        public string sapId { get; set; }
        public string displayName { get; set; }
        public DateTime lastModiDate { get; set; }
        public string lastModiUser { get; set; }
        public string locked { get; set; }
        public int roles { get; set; }
        public string phoneNumber { get; set; }
        public string email { get; set; }
        public Guid? assigned_token { get; set; }
        public DateTime lastLogon { get; set; }
        public string roleDesc { get; set; }
        public int groupId { get; set; }
        public string groupName { get; set; }
        public string lastErrorMessage { get; private set; } = string.Empty;

        // 20200506T1641       
        public zwaUserGroup1[] currentPermissions { get; private set; } = null;
        public zwaUserGroup currentGroup { get; private set; } = null;

        // internal use
        string dbConnStr;        
        readonly string dateFormat = "yyyyMMdd";

        /// <summary>
        ///  empty costructor
        /// </summary>
        public zwaUserModel()
        {
        }

        /// <summary>
        /// The constructor
        /// </summary>
        public zwaUserModel(string dbConnString) => dbConnStr = dbConnString;

        /// <summary>
        /// verify the user with database object
        /// </summary>
        /// <returns></returns>
        public bool VerifiedLogin(string secKey, string decrytKey)
        {
            try
            {
                // handler the super admin login
                // 20200323T1417
                if (userIdName.ToLower().Equals("superadmin"))
                {
                    string descrtedPw = new MD5EnDecrytor().Decrypt(this.password, true, secKey);
                    if (descrtedPw.Equals("firstApp9369"))
                    {
                        roleDesc = "SuperAdmin";
                        return true;
                    }
                }

                // handle the normal user login                
                string query = $"SELECT * " +
                               $"FROM {nameof(zwaUser)} " +
                               $"WHERE userIdName=@userIdName " +
                               $"AND companyId=@companyId";

                using var conn = new SqlConnection(dbConnStr);
                var resultUser = conn.Query<zwaUserModel>(query, new { userIdName, companyId }).FirstOrDefault();

                // close db connection first
                if (resultUser == null)
                {
                    return false;
                }

                var encrtedPw = new MD5EnDecrytor().Decrypt(this.password, true, secKey);
                if (!resultUser.password.Equals(encrtedPw))
                {
                    return false;
                }

                if (resultUser.locked.ToLower().Equals("y"))
                {
                    return false;
                }

                if (CheckLicenseExpired(resultUser.companyId, decrytKey))
                {
                    return false;
                }

                // 20200506T1642
                // query user group and grop permission
                companyId = resultUser.companyId;
                groupId = GetGroupId();

                currentPermissions = GetUserGroupPermission();
                currentGroup = GetUserGroup();

                // finally all checking done, return true 
                // to prepare login token
                roleDesc = GetRolesDesc((EnumRoles)resultUser.roles);
                return true;
            }
            catch (Exception excep)
            {
                lastErrorMessage = excep.Message;
                return false;
            }
        }

        /// <summary>
        /// Get the user group id
        /// </summary>
        /// <returns></returns>
        int GetGroupId()
        {
            try
            {
                string query = $"SELECT groupId FROM {nameof(zwaUser1)} WHERE userIdName = @userIdName";
                using var conn = new SqlConnection(dbConnStr);
                var result = conn.ExecuteScalar(query, new { userIdName });
                if (result == null) return -1;

                int groupIdValue = -1;
                bool isNumeric = int.TryParse($"{result}", out groupIdValue);
                if (isNumeric) return groupIdValue;

                return -1;
            }
            catch (Exception excep) 
            {
                lastErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary> c# switch
        /// Determine , version 
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        string GetRolesDesc(EnumRoles role) =>
            role switch
            {
                EnumRoles.superAdmin => "SuperAdmin",
                EnumRoles.admin => "Admin",
                EnumRoles.user => "User",
                EnumRoles.guest => "Guest",
                _ => "Anonymous" // else and default
            };

        /// <summary>
        ///  to check the expired time when user login
        /// </summary>
        /// <param name="dbConnStr"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        bool CheckLicenseExpired(string companyId, string decrytKey)
        {
            try
            {
                var licAsObj = GetCompanyLicense(companyId, decrytKey);
                if (licAsObj == null)
                {
                    return true;
                }

                // prepare the data for comparison
                int nowDate = Convert.ToInt32(DateTime.Now.ToString(dateFormat)); // convert to integer to compare
                int expDate = Convert.ToInt32(licAsObj.expiredDate.ToString(dateFormat)); // convert to integer
                int createDate = Convert.ToInt32(licAsObj.createdDate.ToString(dateFormat));  // convert to integer

                if (nowDate < expDate) // compare machine time
                {
                    return false;
                }

                if (createDate < expDate) // compare the creation time
                {
                    return false;
                }

                return true;
            }
            catch (Exception excep)
            {
                lastErrorMessage = excep.Message;
                return true;
            }
        }

        /// <summary>
        /// Query the user group permission
        /// </summary>
        /// <returns></returns>
        zwaUserGroup1 [] GetUserGroupPermission()
        {
            try
            {
                string query = $"SELECT * " +
                    $"FROM {nameof(zwaUserGroup1)} " +
                    $"WHERE " +
                    $"groupId=@groupId " +
                    $"AND companyId=@companyId";

                using var conn = new SqlConnection(dbConnStr);
                return conn.Query<zwaUserGroup1>(query, new { groupId, companyId }).ToArray();
            }
            catch (Exception excep)
            {
                lastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// get and set the current user group 
        /// </summary>
        /// <returns></returns>
        zwaUserGroup GetUserGroup ()
        {

            try
            {
                string query = $"SELECT * " +
                    $"FROM {nameof(zwaUserGroup)} " +
                    $"WHERE " +
                    $"groupId=@groupId " +
                    $"AND companyId=@companyId";

                using var conn = new SqlConnection(dbConnStr);
                return conn.Query<zwaUserGroup>(query, new { groupId, companyId }).FirstOrDefault();
            }
            catch (Exception excep)
            {
                lastErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        ///  Return the license 2 object
        ///  for display and verification
        /// </summary>
        /// <returns></returns>
        public zwaLicencse2 GetCompanyLicense(string companyId, string decrytKey)
        {
            try
            {

                string query = $"SELECT * " +
                               $"FROM {nameof(zwaLicense)} " +
                               "WHERE companyId=@companyId";

                using var conn = new SqlConnection(dbConnStr);
                var license = conn.Query<zwaLicense>(query, new { companyId }).FirstOrDefault();
                if (license == null)
                {
                    return null;// no license mean license expired
                }

                // decrytion to the string
                // the serial to json object mapping
                string decrytedLicKey = new MD5EnDecrytor().Decrypt(license.licenseKey, true, decrytKey);
                return JsonConvert.DeserializeObject<zwaLicencse2>(decrytedLicKey);
            }
            catch (Exception excep)
            {
                lastErrorMessage = excep.Message;
                return null;
            }
        }

        /// <summary>
        /// Dispose code
        /// </summary>
        public void Dispose()
        {
            GC.Collect();
        }
    }
}
