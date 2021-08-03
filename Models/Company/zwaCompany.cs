using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WMSWebAPI.Models.Company
{
    /// <summary>
    /// Class represent a company from database table zwaCompany
    /// </summary>
    public class zwaCompany : IDisposable
    {
        // for bindable properties
        public string companyId { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string contactPerson { get; set; }
        public string celollar { get; set; }
        public string email { get; set; }
        public DateTime? lastModiDate { get; set; }
        public string LastModiUser { get; set; }
        public string locked { get; set; }
        // ------------------------------------------ used by the app
        public string licenseKey { get; set; }
        public string connDbString { get; set; }
        public string erpDbdetails { get; set; }

        // inner private declaration
        
        string lastErrorMessage;
        string db_conn_sap;
        SqlTransaction transaction;
        SqlConnection conn;

        /// <summary>
        /// Default constructor
        /// </summary>
        public zwaCompany()
        {
            // constructor
            var dBConnect_SAP = System.Configuration.ConfigurationManager.AppSettings["DBConnect_SAP"];
            if (dBConnect_SAP != null && dBConnect_SAP.Length > 0)
                this.db_conn_sap = dBConnect_SAP.ToString();

            lastErrorMessage = "";
        }

        /// <summary>
        /// Direct contructor to load the data direct from the pass company id
        /// invoke from AdminLogin class
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="dbConnStr"></param>
        public zwaCompany(string companyId, string dbConnStr)
        {
            try
            {
                lastErrorMessage = string.Empty;
                string query =
                    " SELECT " +
                        "name" +
                        ",address" +
                        ",contactPerson" +
                        ",celollar" +
                        ",email" +
                        ",locked" +
                    " FROM " +
                        nameof(zwaCompany) +
                    " WHERE " +
                        "companyId =@ companyId"; // + companyId;

                var param = new { companyId = companyId };
                zwaCompany temp;
                using (var conn = new SqlConnection(dbConnStr))
                {
                    temp = conn.Query<zwaCompany>(query, param).FirstOrDefault();
                }

                // init the this class properties
                if (temp != null)
                {
                    name = temp.name;
                    address = temp.address;
                    contactPerson = temp.address;
                    celollar = temp.celollar;
                    email = temp.email;
                    locked = temp.locked;
                }
            }
            catch (Exception excep)
            {
                lastErrorMessage = excep.Message;
            }
        }

        /// <summary>
        /// Init the conn and transation
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
                lastErrorMessage = excep.Message;
            }
        }

        /// <summary>
        /// Roll back the execution result
        /// </summary>
        void RollBack()
        {
            if (transaction != null) transaction.Rollback();
        }

        /// <summary>
        /// commiting the database when it not null
        /// </summary>
        void Commit()
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Commit();
                    CloseConnTrans();
                }
            }
            catch (Exception excep)
            {
                RollBack();
                lastErrorMessage = excep.Message;
            }
        }

        /// <summary>
        /// Dispose and close the database conn and it transaction object
        /// </summary>
        void CloseConnTrans()
        {
            if (transaction != null)
            {
                transaction.Dispose();
                transaction = null;
            }

            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
                conn = null;
            }
        }

        /// <summary>
        /// return the error message
        /// </summary>
        /// <returns></returns>
        public string GetErrorMessage()
        {
            return lastErrorMessage;
        }

        /// <summary>
        /// Get list of the compnay record from database sap
        /// </summary>
        /// <returns></returns>
        public zwaCompany[] GetList()
        {
            try
            {
                string query = "SELECT * FROM " + nameof(zwaCompany);
                using (var conn = new SqlConnection(db_conn_sap))
                {
                    return conn.Query<zwaCompany>(query).ToArray();
                }
            }
            catch (Exception excep)
            {
                lastErrorMessage = excep.Message;
                return null;
            }
        }

        /// <summary>
        /// to check the existin of the company
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        bool IsCompanyLicenseExist(string companyId)
        {
            try
            {
                string query =
                    " SELECT " +
                        "id" +
                    " FROM " +
                        nameof(zwaLicense) +
                    " WHERE " +
                        "companyId = @companyId";

                var param = new { companyId = companyId };
                using (var conn = new SqlConnection(db_conn_sap))
                {
                    int res = conn.ExecuteScalar<int>(query, param);
                    return (res > 0);
                }
            }
            catch (Exception excep)
            {
                lastErrorMessage = excep.ToString();
                return false;
            }
        }

        /// <summary>
        /// Update the company license key
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        public int UpdateCompanyLicense(zwaCompany company)
        {
            //int result = -1;
            try
            {
                if (IsCompanyLicenseExist(company.companyId))
                {
                    string updateQuery =
                        " UPDATE " +
                            nameof(zwaLicense) +
                        " SET " +
                            "licenseKey = @licenseKey" +
                        " WHERE " +
                            "companyId = @companyId";

                    BeginTransaction();
                    var parameter = new { licenseKey = company.licenseKey, companyId = company.companyId };
                    int updateResult = conn.Execute(updateQuery, parameter, transaction);
                    if (updateResult > 0)
                    {
                        Commit();
                    }
                    return updateResult;
                }

                // ELSE 
                // conduct the insert
                string InsertQuery =
                        "INSERT INTO " + nameof(zwaLicense) + "(companyId,licenseKey)VALUES(@companyId,@licenseKey)";

                BeginTransaction();
                var param = new { licenseKey = company.licenseKey, companyId = company.companyId };

                int insertResult = conn.Execute(InsertQuery, param, transaction);
                if (insertResult > 0)
                {
                    Commit();
                }
                return insertResult;
            }
            catch (Exception excep)
            {
                RollBack();
                lastErrorMessage = excep.ToString();
                return -1;
            }
        }

        /// <summary>
        ///  the class dispose
        /// </summary>
        public void Dispose()
        {
            CloseConnTrans();
            GC.Collect();
        }

        #region obsolate code
        /// <summary>
        /// to check the existin of the company
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        //bool IsCompanyExist( zwaCompany company)
        //{
        //    try
        //    {
        //        string query = "SELECT companyId " +
        //            " FROM zwaCompany " +
        //            " WHERE companyId = @companyId";

        //        using (Database db = new Database (this.db_conn_sap))
        //        using (SqlCommand cmd = new SqlCommand(query, db.conn))
        //        {
        //            cmd.Parameters.AddWithValue("companyId", company.companyId);

        //            var result = cmd.ExecuteScalar();
        //            if (result == null) return false;
        //            if ((int)result == company.companyId) return true;
        //        }
        //    }
        //    catch (Exception excep)
        //    {
        //        lastErrorMessage = excep.ToString();
        //    }

        //    return false;
        //}

        /// <summary>
        /// Insert the new company record
        /// </summary>
        /// <param name="newComp"></param>
        /// <returns></returns>
        //public int InsertNewCompany(zwaCompany newComp)
        //{
        //    int result = -1;
        //    try
        //    {
        //        string insertsql = "INSERT INTO zwaCompany (" +
        //                          " name " +
        //                          " ,address " +
        //                          " ,contactPerson " +
        //                          " ,celollar " +
        //                          " ,email " +
        //                          " ,lastModiDate " +
        //                          " ,LastModiUser " +
        //                          " ,locked " +
        //                          " ,connDbString " +
        //                          " ,erpDbdetails " +
        //                            ")  OUTPUT INSERTED.companyId VALUES ( " +
        //                          "  @name " +
        //                          " ,@address " +
        //                          " ,@contactPerson " +
        //                          " ,@celollar " +
        //                          " ,@email " +
        //                          " ,GETDATE() " +
        //                          " ,@LastModiUser " +
        //                          " ,@locked " +
        //                          " ,@connDbString " +
        //                          " ,@erpDbdetails " +
        //                          ")";

        //        using (Database db = new Database(this.db_conn_sap))
        //        using (SqlCommand cmd = new SqlCommand(insertsql, db.conn))
        //        {
        //            cmd.Parameters.AddWithValue("name", newComp.name);
        //            cmd.Parameters.AddWithValue("address", newComp.address);
        //            cmd.Parameters.AddWithValue("contactPerson", newComp.contactPerson);
        //            cmd.Parameters.AddWithValue("celollar", newComp.celollar);
        //            cmd.Parameters.AddWithValue("email", newComp.email);
        //            cmd.Parameters.AddWithValue("locked", newComp.locked);
        //            cmd.Parameters.AddWithValue("LastModiUser", "SuperAdmin");
        //            cmd.Parameters.AddWithValue("connDbString", newComp.connDbString);
        //            cmd.Parameters.AddWithValue("erpDbdetails", newComp.erpDbdetails);

        //            var cmdResult = cmd.ExecuteScalar();
        //            if (cmdResult == null)
        //            {
        //                lastErrorMessage = db.lastErrMsg;
        //                result = -1;
        //                return result;
        //            }

        //            result = (int)cmdResult;
        //            if (result < 0)
        //            {
        //                lastErrorMessage = db.lastErrMsg;
        //                result = -1;
        //                return result;
        //            }

        //            // else 
        //            newComp.companyId = result; // for new company insert into the log
        //            InsertCompanyLog(newComp, "new insert");
        //        }
        //    }
        //    catch (Exception excep)
        //    {
        //        lastErrorMessage = excep.ToString();
        //    }
        //    return result;
        //}

        /// <summary>
        /// For insert a backup copy 
        /// </summary>
        /// <param name="newComp"></param>
        /// <returns></returns>
        //private int InsertCompanyLog(zwaCompany newComp, string actionName)
        //{
        //    int result = -1;
        //    try
        //    {
        //        string insertsql = "INSERT INTO zwaCompanyLog (" +
        //                          " companyId" +
        //                          " ,name " +
        //                          " ,address " +
        //                          " ,contactPerson " +
        //                          " ,celollar " +
        //                          " ,email " +
        //                          " ,lastModiDate " +
        //                          " ,LastModiUser " +
        //                          " ,locked " +
        //                          " ,action " +
        //                          ", connDbString " +
        //                          ", erpDbdetails " +
        //                            ") VALUES ( " +
        //                          "  @companyId " +
        //                          " ,@name " +
        //                          " ,@address " +
        //                          " ,@contactPerson " +
        //                          " ,@celollar " +
        //                          " ,@email " +
        //                          " ,GETDATE() " +
        //                          " ,@LastModiUser " +
        //                          " ,@locked " +
        //                          " ,@action " +
        //                          " ,@connDbString " +
        //                          " ,@erpDbdetails " +
        //                          ")";

        //        using (Database db = new Database(this.db_conn_sap))
        //        using (SqlCommand cmd = new SqlCommand(insertsql, db.conn))
        //        {
        //            cmd.Parameters.AddWithValue("companyId", newComp.companyId);
        //            cmd.Parameters.AddWithValue("name", newComp.name);
        //            cmd.Parameters.AddWithValue("address", newComp.address);
        //            cmd.Parameters.AddWithValue("contactPerson", newComp.contactPerson);
        //            cmd.Parameters.AddWithValue("celollar", newComp.celollar);
        //            cmd.Parameters.AddWithValue("email", newComp.email);
        //            cmd.Parameters.AddWithValue("locked", newComp.locked);
        //            cmd.Parameters.AddWithValue("LastModiUser", "SuperAdmin");
        //            cmd.Parameters.AddWithValue("action", actionName.ToUpper());
        //            cmd.Parameters.AddWithValue("connDbString", newComp.connDbString + "");
        //            cmd.Parameters.AddWithValue("erpDbdetails", newComp.erpDbdetails + "");

        //            result = cmd.ExecuteNonQuery(); // system return the insert row id
        //            if (result < 0)
        //            {
        //                lastErrorMessage = db.lastErrMsg;
        //            }
        //        }
        //    }
        //    catch (Exception excep)
        //    {
        //        lastErrorMessage = excep.ToString();
        //    }
        //    return result;
        //}

        /// <summary>
        /// to perform the update the company 
        /// </summary>
        /// <param name="editedCompany"></param>
        /// <returns></returns>
        //public int UpdateCompany (zwaCompany editedCompany)
        //{
        //    int result = -1;
        //    try
        //    {
        //        if (IsCompanyExist(editedCompany))
        //        {
        //            string updateSql = "UPDATE  zwaCompany " +
        //                "  SET name = @name " +
        //                ", address = @address " +
        //                ", contactPerson = @contactPerson " +
        //                ", celollar = @celollar " +
        //                ", email = @email " +
        //                ", lastModiDate = GETDATE() " +
        //                ", LastModiUser = @LastModiUser " +
        //                ", locked = @locked " +
        //                ", connDbString = @connDbString " +
        //                ", erpDbdetails = @erpDbdetails " +                    
        //                " WHERE companyid = @companyid";

        //            using (Database db = new Database(this.db_conn_sap))
        //            using (SqlCommand cmd = new SqlCommand(updateSql, db.conn))
        //            {
        //                cmd.Parameters.AddWithValue("name", editedCompany.name);
        //                cmd.Parameters.AddWithValue("address", editedCompany.address);
        //                cmd.Parameters.AddWithValue("contactPerson", editedCompany.contactPerson);
        //                cmd.Parameters.AddWithValue("celollar", editedCompany.celollar);
        //                cmd.Parameters.AddWithValue("email", editedCompany.email);
        //                cmd.Parameters.AddWithValue("LastModiUser", editedCompany.LastModiUser);
        //                cmd.Parameters.AddWithValue("locked", editedCompany.locked);
        //                cmd.Parameters.AddWithValue("connDbString", editedCompany.connDbString + "");
        //                cmd.Parameters.AddWithValue("erpDbdetails", editedCompany.erpDbdetails + "");
        //                cmd.Parameters.AddWithValue("companyid", editedCompany.companyId);

        //                result = cmd.ExecuteNonQuery();
        //                if (result == -1)
        //                {
        //                    lastErrorMessage = db.lastErrMsg;
        //                    return result;
        //                }
        //                // else 
        //                InsertCompanyLog(editedCompany, "Update"); // insert into a log
        //                return result;
        //            }
        //        }
        //        // else 
        //        lastErrorMessage = "The edited company no exist in the database. Please contact admin for help. Thanks";
        //        return -3; 

        //    }
        //    catch (Exception excep)
        //    {
        //        lastErrorMessage = excep.ToString();
        //    }
        //    return result;
        //}

        #endregion
    }
}
