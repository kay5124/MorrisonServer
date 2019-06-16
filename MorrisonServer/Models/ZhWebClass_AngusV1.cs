using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Security;
using ZhClass;

namespace MorrisonServer.Models.ZhWebClass_AngusV1
{
    public class UserData
    {
        public int actSerial;
        public int sysUserId;
        public string userId;
        public string userName;
        public string contactTel;
        public DateTime loginTime;
        public string groups;
        public string platform;
        public string model;
        public string UUID;
        public string dcId;

        public DataTable Get_tbl_userLogV1()
        {
            DataTable tbl_userLog = new DataTable("S90_userLog");
            tbl_userLog.Columns.Add("actSerial", typeof(int));
            tbl_userLog.Columns.Add("sysUserId", typeof(int));
            tbl_userLog.Columns.Add("platform", typeof(string));
            tbl_userLog.Columns.Add("model", typeof(string));
            tbl_userLog.Columns.Add("UUID", typeof(string));
            tbl_userLog.Columns.Add("statusType", typeof(string));
            tbl_userLog.Columns.Add("statusId", typeof(string));
            tbl_userLog.Columns.Add("clientIp", typeof(string));
            tbl_userLog.Columns.Add("creatUser", typeof(string));

            return tbl_userLog;
        }

        public DataTable Get_tbl_operLogV1()
        {
            DataTable tbl_operLog = new DataTable("S90_operLog");
            tbl_operLog.Columns.Add("rowId", typeof(int));
            tbl_operLog.Columns.Add("actSerial", typeof(int));
            tbl_operLog.Columns.Add("logDate", typeof(string));
            tbl_operLog.Columns.Add("sysUserId", typeof(int));
            tbl_operLog.Columns.Add("clientIp", typeof(string));
            tbl_operLog.Columns.Add("controllerName", typeof(string));
            tbl_operLog.Columns.Add("actionName", typeof(string));
            tbl_operLog.Columns.Add("statusId", typeof(string));
            tbl_operLog.Columns.Add("statusId2", typeof(string));
            tbl_operLog.Columns.Add("resultCode", typeof(string));
            tbl_operLog.Columns.Add("errMsg", typeof(string));
            tbl_operLog.Columns.Add("tableName", typeof(string));
            tbl_operLog.Columns.Add("tblPrimaryKeysAndValues", typeof(string));
            tbl_operLog.Columns.Add("strSql", typeof(string));

            return tbl_operLog;
        }
    }


    public static class UserHelper
    {
        public static UserData GetUserData()
        {
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                // 先取得該使用者的 FormsIdentity
                FormsIdentity id = HttpContext.Current.User.Identity as FormsIdentity;
                // 再取出使用者的 FormsAuthenticationTicket
                FormsAuthenticationTicket ticket = id.Ticket;
                var userInfo = id.Ticket.UserData;
                UserData userData = JsonConvert.DeserializeObject<UserData>(id.Ticket.UserData);
                return userData;
            }
            else
            {
                UserData userData;
                userData = null;
                return userData;
            }
        }

        public static UserData GetUserDataApp(string ticket)
        {
            HttpCookie authCookie;
            string encTicket;
            FormsAuthenticationTicket tmpTicket;
            UserData userData = new UserData();

            try
            {
                if (ticket == "")
                {
                    authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];
                    encTicket = authCookie.Value;
                    tmpTicket = FormsAuthentication.Decrypt(encTicket);
                }
                else
                {
                    tmpTicket = FormsAuthentication.Decrypt(ticket);
                }

                userData = JsonConvert.DeserializeObject<UserData>(tmpTicket.UserData);

            }
            catch (Exception ex)
            {
                userData = new UserData();
            }
           
            return userData;
        }
    }


    public class Log
    {
        //public static string SaveUserLog(int actSerial, int sysUserId, string statusId, string statusType = "DP", string clientIp = "", string platform = "", string model = "", string UUID = "")
        /// <summary>
        /// 使用者登入的Log記錄，傳入的table請呼叫Get_tbl_userLogV1
        /// 內容欄位包含
        /// </summary>
        /// <param name="tmpTbl"></param>
        /// <returns></returns>
        public static string SaveUserLog(DataTable tmpTbl)
        {
            string errStr = "";
            string strSql = "";

            SqlParameter[] param = {
                    new SqlParameter("actSerial", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["actSerial"].ToString()),
                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["sysUserId"].ToString()),
                    new SqlParameter("statusType", SqlDbType.Char, 2, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["statusType"].ToString()),
                    new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["statusId"].ToString()),
                    new SqlParameter("clientIp", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["clientIp"].ToString()),
                    new SqlParameter("platform", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["platform"].ToString()),
                    new SqlParameter("model", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["model"].ToString()),
                    new SqlParameter("UUID", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["UUID"].ToString()),
                    new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["sysUserId"].ToString())
                };

            strSql = " Insert into S90_userLog (actSerial, sysUserId, statusType, statusId, clientIp, platform, model, UUID, creatUser, creatTime) ";
            strSql += " values (@actSerial, @sysUserId, @statusType, @statusId, @clientIp, @platform, @model, @UUID, @creatUser, getutcdate()) ";
            errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);
            if (errStr != "") throw new Exception(errStr);

            return errStr;
        }

        /// <summary>
        /// 記錄使用者操作的Log
        /// </summary>
        /// <param name="tmpTbl">主要傳入S90_operLog的欄位資料</param>
        /// <returns></returns>
        public static string SaveOperLog(DataTable tmpTbl)
        {
            string errStr = "";
            string strSql = "";
            try
            {

                SqlParameter[] param = {
                new SqlParameter("actSerial", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["actSerial"].ToString()),
                new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["sysUserId"].ToString()),
                new SqlParameter("logDate", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["logDate"].ToString()),
                new SqlParameter("clientIp", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["clientIp"].ToString()),
                new SqlParameter("controllerName", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["controllerName"].ToString()),
                new SqlParameter("actionName", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["actionName"].ToString()),
                new SqlParameter("statusType", SqlDbType.Char, 2, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, "O1"),
                new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["statusId"].ToString()),
                new SqlParameter("statusType2", SqlDbType.Char, 2, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, "DP"),
                new SqlParameter("statusId2", SqlDbType.Char, 2, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["statusId2"].ToString()),
                new SqlParameter("resultCode", SqlDbType.Char, 2, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["resultCode"].ToString()),
                new SqlParameter("errMsg", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["errMsg"].ToString()),
                new SqlParameter("tblPrimaryKeysAndValues", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["tblPrimaryKeysAndValues"].ToString()),
                new SqlParameter("strSql", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0 , 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["strSql"].ToString())
            };

                strSql = " Insert into S90_operLog (actSerial, sysUserId, logDate, clientIp, controllerName, actionName, statusType, statusId, statusType2, statusId2, resultCode, errMsg, tblPrimaryKeysAndValues, strSql) ";
                strSql += " values (@actSerial, @sysUserId, @logDate, @clientIp, @controllerName, @actionName, @statusType, @statusId, @statusType2, @statusId2, @resultCode, @errMsg, @tblPrimaryKeysAndValues, @strSql) ";
                errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                if (errStr != "") throw new Exception(errStr);

            }
            catch (Exception ex)
            {
                errStr = ex.Message;
            }

            return errStr;
        }
    }
}