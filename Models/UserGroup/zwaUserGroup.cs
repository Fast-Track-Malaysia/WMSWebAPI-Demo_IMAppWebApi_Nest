using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace WMSWebAPI.Models
{
    /// <summary>
    /// Class represent the [zwaUserGroup] in database
    /// </summary>
    public class zwaUserGroup : IDisposable
    {
        public int groupId { get; set; }
        public string companyId { get; set; }
        public string groupName { get; set; }
        public string groupDesc { get; set; }
        public DateTime lastModiDate { get; set; }
        public string lastModiUser { get; set; }
        
        // Inner declaration
        SqlConnection conn;
        SqlTransaction transaction;
        string db_conn_sap;

        /// <summary>
        /// Use base derive class to update the base
        /// </summary>
        /// <param name="obj"></param>
        protected zwaUserGroup(zwaUserGroup obj)
        {
            groupId = obj.groupId;
            companyId = obj.companyId;
            groupName = obj.groupName;
            groupDesc = obj.groupDesc;
            lastModiDate = obj.lastModiDate;
            lastModiUser = obj.lastModiUser;
        }

        /// <summary>
        /// Construtor 
        /// read database setup from the web config file
        /// </summary>
        public zwaUserGroup(string dbConnString) => db_conn_sap = dbConnString;

        /// <summary>
        /// Empty constructor for dapper init
        /// </summary>
        public zwaUserGroup() 
        { }

        /// <summary>
        /// Return the last error message
        /// </summary>
        /// <returns></returns>
        public string GetErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Start the transaction db to seek roll back
        /// </summary>
        void BeginTransaction()
        {
            try
            {
                conn = new SqlConnection(db_conn_sap);
                conn.Open();
                transaction = conn.BeginTransaction();
            }
            catch (Exception excep)
            {
                GetErrorMessage = $"{excep}";
            }
        }

        /// <summary>
        /// commit all the db transaction 
        /// </summary>
        void Commit()
        {
            transaction?.Commit();
            transaction?.Dispose();
            transaction = null;
            conn?.Close();
            conn = null;
        }

        /// <summary>
        /// roll back the trasaction when erro occur
        /// </summary>
        void RollBack() => transaction?.Rollback();       

        /// <summary>
        /// Perform a update to the existing group
        /// </summary>
        /// <param name="editedGroup"></param>
        /// <returns></returns>
        public int UpdateGroup(zwaUserGroup editedGroup, zwaUserGroup1[] groupPermissionList, zwaUser[] userGroupUsrlist)
        {
            try
            {
                string updateSql = $"UPDATE {nameof(zwaUserGroup)}" +
                                    $" SET " +
                                        $"companyId=@companyId" +
                                        $",groupName=@groupName" +
                                        $",groupDesc=@groupDesc" +
                                        $",lastModiDate=GETDATE()" +
                                        $",lastModiUser=@lastModiUser" +
                                    $" WHERE groupId=@groupId";

                BeginTransaction();
                int result = conn.Execute(updateSql, editedGroup, transaction);

                if (result >= 0)
                {
                    result = InsertGroupLog(editedGroup, "UPDATE");
                }

                // user group permission
                if (result >= 0) // insert the permission 
                {
                    result = UpdateUserGroupPermission(groupPermissionList, editedGroup);
                }

                // handle the usr group user relatioship
                if (result >= 0)
                {
                    result = UpdateUserGroupUsrRelationship(userGroupUsrlist, editedGroup);
                }

                if (result > 0)
                {
                    Commit();
                }
                return result;
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Insert new created group 
        /// </summary>
        /// <param name="newGroup"></param>
        /// <returns></returns>
        public int InsertNewGroup(zwaUserGroup newGroup, zwaUserGroup1[] groupPermissionList, zwaUser[] userGroupUsrlist)
        {
            try
            {
                string insertSql = $"INSERT INTO {nameof(zwaUserGroup)}(" +
                                $"companyId" +
                                $",groupName" +
                                $",groupDesc" +
                                $",lastModiDate" +
                                $",lastModiUser" +
                                $")VALUES(" +
                                $"@companyId" +
                                $",@groupName" +
                                $",@groupDesc" +
                                $",GETDATE()" +
                                $",@lastModiUser" +
                                $") SELECT CAST(SCOPE_IDENTITY() AS INT)";

                BeginTransaction();
                int result = conn.ExecuteScalar<int>(insertSql, newGroup, transaction);

                if (result > 0)
                {
                    newGroup.groupId = result;
                    result = InsertGroupLog(newGroup, "INSERT");
                }

                // user group permission
                if (result >= 0) // insert the permission 
                {
                    result = UpdateUserGroupPermission(groupPermissionList, newGroup);
                }

                // handle the usr group user relatioship
                if (result >= 0)
                {
                    result = UpdateUserGroupUsrRelationship(userGroupUsrlist, newGroup);
                }

                if (result > 0)
                {
                    Commit();
                }
                return result;
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// update the user group user relationship in group 
        /// </summary>
        /// <param name="userList"></param>
        /// <param name="groupInfo"></param>
        /// <returns></returns>
        int UpdateUserGroupUsrRelationship(zwaUser[] userList, zwaUserGroup groupInfo)
        {
            try
            {
                if (userList == null) return 1;
                if (userList.Length == 0) return 1; /// allow not user setup // 20200623

                // delete the user from group relatioship
                string sqlDeleteUserGroupRelationship = $"DELETE FROM {nameof(zwaUser1)} WHERE userId=@userId";
                int result = -1;
                foreach (var usr in userList)
                {
                    result = conn.Execute(sqlDeleteUserGroupRelationship, new { userId = usr.sysId }, transaction);
                }

                // update the user for the group relationship -=== log delete
                if (result >= 0)
                {
                    result = InsertUserGroupUserRelationLog(userList, groupInfo.groupId, "DELETE");
                }

                if (result >= 0)
                {
                    result = InsertUserGroupUserRelation(userList, groupInfo.groupId);
                }

                // insert the user for the group relationship -==== Insert
                if (result >= 0)
                {
                    result = InsertUserGroupUserRelationLog(userList, groupInfo.groupId, "INSERT");
                }

                return result;
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Insert the user group relatioship
        /// </summary>
        /// <param name="userList"></param>
        /// <param name="grouId"></param>
        /// <returns></returns>
        int InsertUserGroupUserRelation(zwaUser[] userList, int grouId)
        {
            try
            {
                string insertSql = $"INSERT INTO {nameof(zwaUser1)}(" +
                                       $"userId" +
                                       $",userIdName" +
                                       $",groupId" +
                                       $",lastModiDate" +
                                       $",lastModiUser" +
                                   $")VALUES(" +
                                    $"@userId" +
                                    $",@userIdName" +
                                    $",@groupId" +
                                    $",@lastModiDate" +
                                    $",@lastModiUser)";

                // prepare insert the log
                List<zwaUser1> newUserGroupUsrList = new List<zwaUser1>();

                for (int x = 0; x < userList.Length; x++)
                {
                    var usr = new zwaUser1();
                    usr.userId = userList[x].sysId;
                    usr.userIdName = userList[x].userIdName;
                    usr.groupId = grouId;
                    usr.lastModiDate = DateTime.Now;
                    usr.lastModiUser = userList[x].lastModiUser;
                    newUserGroupUsrList.Add(usr);
                }

                return conn.Execute(insertSql, newUserGroupUsrList, transaction);
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Insert log into zwaUser1 for group relationship
        /// reuse code for insert and delete
        /// </summary>
        /// <param name="userList"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        int InsertUserGroupUserRelationLog(zwaUser[] userList, int grouId, string action)
        {
            try
            {
                string insertSql = $"INSERT INTO {nameof(zwaUser1Log)}(" +
                                       $"userId" +
                                       $",userIdName" +
                                       $",groupId" +
                                       $",lastModiDate" +
                                       $",lastModiUser" +
                                       $",action" +
                                   $")VALUES(" +
                                    $"@userId" +
                                    $",@userIdName" +
                                    $",@groupId" +
                                    $",@lastModiDate" +
                                    $",@lastModiUser" +
                                    $",@action" +
                                    $")";

                // prepare insert the log
                List<zwaUser1Log> logs = new List<zwaUser1Log>();
                for (int x = 0; x < userList.Length; x++)
                {
                    // convert from zwaUser into zwaUser1
                    zwaUser1 usr = new zwaUser1();
                    usr.userId = userList[x].sysId;
                    usr.userIdName = userList[x].userIdName;
                    usr.groupId = grouId;
                    usr.lastModiDate = DateTime.Now;
                    usr.lastModiUser = userList[x].lastModiUser;

                    // add into the list
                    logs.Add(new zwaUser1Log(action, usr));
                }
                return conn.Execute(insertSql, logs, transaction);
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Insert the user log
        /// </summary>
        /// <param name="newGroup"></param>
        /// <returns></returns>
        int InsertGroupLog(zwaUserGroup newGroup, string action)
        {
            try
            {
                string insertSql = $"INSERT INTO {nameof(zwaUserGroupLog)}(" +
                    $"groupId" +
                    $",companyId" +
                    $",groupName" +
                    $",groupDesc" +
                    $",lastModiDate" +
                    $",lastModiUser" +
                    $",action" +
                    $")VALUES( " +
                    $"@groupId" +
                    $",@companyId" +
                    $",@groupName" +
                    $",@groupDesc" +
                    $",GETDATE()" +
                    $",@lastModiUser" +
                    $",@action" +
                    $")";

                zwaUserGroupLog newLog = new zwaUserGroupLog(action, newGroup);
                return conn.Execute(insertSql, newLog, transaction);
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Query the company group based on company id
        /// </summary>
        /// <param name="compyId"></param>
        /// <returns></returns>
        public zwaUserGroup[] GetCompanyGroup(string companyId)
        {
            try
            {
                string query =
                    $"SELECT " +
                       $"groupId" +
                       $",companyId" +
                       $",groupName" +
                       $",groupDesc" +
                       $",lastModiDate" +
                       $",lastModiUser " +
                    $"FROM " +
                        $"zwaUserGroup " +
                    $"WHERE " +
                        $"companyId = @companyId";

                using (var conn = new SqlConnection(db_conn_sap))
                {
                    return conn.Query<zwaUserGroup>(query, new { companyId }).ToArray();
                }
            }
            catch (Exception excep)
            {
                GetErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Return single group, based on company name and groupid
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public zwaUserGroup GetCompanyGroup(string companyId, int groupId)
        {
            try
            {
                string query =
                    $"SELECT " +
                       $"groupId" +
                       $",companyId" +
                       $",groupName" +
                       $",groupDesc" +
                       $",lastModiDate" +
                       $",lastModiUser " +
                    $"FROM " +
                        $"zwaUserGroup " +
                    $"WHERE " +
                        $"companyId = @companyId " +
                        $"AND groupId = @groupId";

                using (var conn = new SqlConnection(this.db_conn_sap))
                {
                    return conn.Query<zwaUserGroup>(query, new { companyId, groupId }).FirstOrDefault();
                }
            }
            catch (Exception excep)
            {
                GetErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Query based on user id and return the grouop id from the zwaUser1 table
        /// </summary>
        /// <param name="userSysId"></param>
        /// <returns></returns>
        public int GetUserGroupId(int userSysId)
        {
            try
            {
                string query = "SELECT groupId FROM zwaUser1 WHERE userId = @userId";
                using (var conn = new SqlConnection(db_conn_sap))
                {
                    return conn.ExecuteScalar<int>(query, new { userId = userSysId });
                }
            }
            catch (Exception excep)
            {
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Get User Group Permission
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        public zwaUserGroup1[] GetUserGroupPermission(string companyId, int groupId, string appName)
        {
            try
            {
                if (groupId == -1)
                {
                    return QueryUserGrouPermissionTemplate();
                }

                using (var connection = new SqlConnection(db_conn_sap))
                {
                    //var query = "SELECT * FROM zwaUserGroup1 WHERE groupid =@groupId AND companyId=@companyId AND appName=@appName";
                    //return connection.Query<zwaUserGroup1>(query, new { companyId, groupId, appName}).ToArray();

                    var res = connection.Query<zwaUserGroup1>("zwa_spQueryGroupPermission"
                        , new { GroupId = groupId, CompanyId = companyId, AppName = appName }
                        , commandType: CommandType.StoredProcedure).ToArray();
                    return res ?? QueryUserGrouPermissionTemplate();

                    //zwa_spQueryGroupPermission
                }
            }
            catch (Exception excep)
            {
                GetErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Query the template and return template setting
        /// </summary>
        /// <returns></returns>
        public zwaUserGroup1[] QueryUserGrouPermissionTemplate()
        {
            try
            {
                string query = $"SELECT id" +
                               $",screenId" +
                               $",groupId" +
                               $",companyId" +
                               $",parentId" +
                               $",title" +
                               $",dscrptn" +
                               $",authorised" +
                               $",lastModiDate" +
                               $",lastModiUser" +
                               $",isFunctionCtrl" +
                               $",ctrlLimit" + //<--- add in the ctrlLimit 20200423T1232
                               $",appName" +
                            $" FROM " +
                                $"zwaUsrGrpPrmssnTmplt" +
                            $" WHERE " +
                              $"groupId=@groupId " +
                              $"AND companyId=@companyId";

                using (var connection = new SqlConnection(db_conn_sap))
                {
                    return connection.Query<zwaUserGroup1>(query, new { groupId = -1, companyId = -1 }).ToArray();
                }
            }
            catch (Exception excep)
            {
                GetErrorMessage = $"{excep}";
                return null;
            }
        }

        /// <summary>
        /// Perform remove previuos list
        /// insert the new list
        /// </summary>
        /// <param name="permissionList"></param>
        /// <returns></returns>
        int UpdateUserGroupPermission(zwaUserGroup1[] permissionList, zwaUserGroup groupInfo)
        {
            try
            {
                if (groupInfo.groupId == -1) return 0;

                string deleteSql = $"DELETE FROM {nameof(zwaUserGroup1)} WHERE groupId=@groupId";

                var param = new { groupId = groupInfo.groupId };

                int result = -1;
                result = conn.Execute(deleteSql, param, transaction);

                // update the delete group permission log == delete
                if (result >= 0)
                {
                    result = InsertUserGroupPermissionLog(permissionList, "DELETE");
                }

                // perform the insert of the permission
                if (result >= 0)
                {
                    result = InsertUserGroupPermission(permissionList);
                }

                // update the delete group permission log == insert
                if (result >= 0)
                {
                    result = InsertUserGroupPermissionLog(permissionList, "INSERT");
                }

                return result;
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Insert group permission
        /// </summary>
        /// <param name="groupPermission"></param>
        /// <returns></returns>
        int InsertUserGroupPermission(zwaUserGroup1[] groupPermission)
        {

            try
            {
                string sqlInsert = $"INSERT INTO {nameof(zwaUserGroup1)}(" +
                                       $"screenId" +
                                       $",groupId" +
                                       $",companyId" +
                                       $",parentId" +
                                       $",title" +
                                       $",dscrptn" +
                                       $",authorised" +
                                       $",lastModiDate" +
                                       $",lastModiUser" +
                                       $",isFunctionCtrl" +
                                       $",ctrlLimit" + //<-- add in ctrlLimit field 20200423T1229
                                       $",appName" +
                                       $")VALUES(" +
                                       $"@screenId" +
                                       $",@groupId" +
                                       $",@companyId" +
                                       $",@parentId" +
                                       $",@title" +
                                       $",@dscrptn" +
                                       $",@authorised" +
                                       $",@lastModiDate" +
                                       $",@lastModiUser" +
                                       $",@isFunctionCtrl" +
                                       $",@ctrlLimit" + //<-- add in ctrlLimit field 20200423T1229
                                       $",@appName" +
                                       $")";

                // update in batch with trasaction attached                                
                return conn.Execute(sqlInsert, groupPermission, transaction);
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Insert the user group log
        /// </summary>
        /// <param name="groupPermission"></param>
        /// <returns></returns>
        int InsertUserGroupPermissionLog(zwaUserGroup1[] groupPermission, string action)
        {
            try
            {
                string sqlInsert = $"INSERT INTO {nameof(zwaUserGroup1Log)}(" +
                                       $"screenId" +
                                       $",groupId" +
                                       $",companyId" +
                                       $",parentId" +
                                       $",title" +
                                       $",dscrptn" +
                                       $",authorised" +
                                       $",lastModiDate" +
                                       $",lastModiUser" +
                                       $",isFunctionCtrl" +
                                       $",action" +
                                       $",ctrlLimit" + //< add in ctrlLimit field 20200423T1229
                                       $",appName" +
                                       $")VALUES(" +
                                       $"@screenId" +
                                       $",@groupId" +
                                       $",@companyId" +
                                       $",@parentId" +
                                       $",@title" +
                                       $",@dscrptn" +
                                       $",@authorised" +
                                       $",@lastModiDate" +
                                       $",@lastModiUser" +
                                       $",@isFunctionCtrl" +
                                       $",@action" +
                                       $",@ctrlLimit" + //< add in ctrlLimit field 20200423T1229
                                       $",@appName" +
                                       $")";

                // prepare insert the log
                var logs = new List<zwaUserGroup1Log>();
                for (int x = 0; x < groupPermission.Length; x++)
                {
                    logs.Add(new zwaUserGroup1Log(action, groupPermission[x]));
                }
                return conn.Execute(sqlInsert, logs, transaction);
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Return the mas id from zwaUserGroup table
        /// </summary>
        /// <returns></returns>
        public int GetTempGroupIdFromSvr()
        {
            try
            {
                string query = $"SELECT MAX(groupId) + 1 'MaxId' FROM {nameof(zwaUserGroup)}";
                using (var conn = new SqlConnection(db_conn_sap))
                {
                    return conn.ExecuteScalar<int>(query);
                }
            }
            catch (Exception excep)
            {
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// Use to reset user group properties into default 
        /// get ready the user for add in other group
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public int ResetUserGroupToDefault(zwaUser[] user)
        {
            try
            {

                string updateSql = $"UPDATE {nameof(zwaUser1)}" +
                                    $" SET groupId =1 " +
                                    $" WHERE userId = @userId";

                BeginTransaction();

                int counter = 0;
                int result = -1;
                foreach (var usr in user)
                {
                    result = conn.Execute(updateSql, new { userId = usr.sysId }, transaction);
                    counter++;
                }

                if (counter == user.Length)
                {
                    Commit();
                }

                return result;
            }
            catch (Exception excep)
            {
                RollBack();
                GetErrorMessage = $"{excep}";
                return -1;
            }
        }

        /// <summary>
        /// For class dispose
        /// </summary>
        public void Dispose() => GC.Collect();
    }
}
