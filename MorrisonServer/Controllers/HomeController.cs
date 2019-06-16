using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace MorrisonServer.Controllers
{
    public class HomeController : Controller
    {
        string resultCode = "10";
        string errStr = "";

        public ActionResult Index()
        {
            //ViewBag.Title = "Home Page";
            return View();
        }

        public ActionResult Login(string userId, string password)
        {
            JObject jo = new JObject();
            string strSql = "";
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            try
            {
                //userId COLLATE Latin1_General_CS_AI = 分字母大小寫判斷
                strSql = " select t1.sysUserId, t1.userId, t1.userName from S10_users t1 with (nolock) where userId COLLATE Latin1_General_CS_AI = '" + userId + "' and password='" + password + "' and statusId='10' and appSysId='00' ";
                //strSql = " select t1.sysUserId, t1.userId, t1.userName from S10_users t1 with (nolock) where userId='" + userId + "' and password='" + password + "' and statusId='10' ";
                //strSql = " select t1.sysUserId, t1.userId, t1.userName from S10_users t1 with (nolock) where userId='" + userId + "' and password=N'" + ZhClass.DesClass.DESEncrypt3(password) + "' and statusId='10' ";
                DataTable tmpTbl1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTbl1");

                if (tmpTbl1.Rows.Count > 0)
                {
                    userData.sysUserId = Convert.ToInt32(tmpTbl1.Rows[0]["sysUserId"].ToString());
                    userData.userId = tmpTbl1.Rows[0]["userId"].ToString();
                    userData.userName = tmpTbl1.Rows[0]["userName"].ToString();
                    userData.loginTime = DateTime.Now;
                    userData.groups = GetGroupsStr(userData.sysUserId.ToString());
                    userData.dcId = GetDcStr(userData.sysUserId.ToString());
                    userData.actSerial = Convert.ToInt32(ZhClass.AutoSerialNoType2.A_GetAutoSerial("S90_userLog", "actSerial"));
                }
                else
                {
                    errStr = "The uerId or password is incorrect. Please re-enter.(Please check capitals and lower case letters)。";
                    //errStr = "帳號或密碼錯誤，請重新輸入(請檢查大小寫)。";
                    throw new Exception(errStr);
                }

                #region user Login Log
                DataTable tbl_userLog = userData.Get_tbl_userLogV1();
                DataRow userDr = tbl_userLog.NewRow();
                userDr["actSerial"] = userData.actSerial;
                userDr["sysUserId"] = userData.sysUserId;
                userDr["statusType"] = "DP";
                userDr["statusId"] = "00";
                userDr["clientIp"] = Request.ServerVariables["REMOTE_ADDR"];
                userDr["creatUser"] = userData.sysUserId;

                tbl_userLog.Rows.Add(userDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveUserLog(tbl_userLog);
                if (errStr != "") throw new Exception(errStr);
                #endregion

                #region Auth identify
                string jUserData = JsonConvert.SerializeObject(userData);
                //調整為24小時後失效 20180822 By.Ray
                FormsAuthentication.SetAuthCookie(userData.sysUserId.ToString(), true, FormsAuthentication.FormsCookiePath);
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddDays(1), true, JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookiePath);
                FormsIdentity identity = new FormsIdentity(ticket);
                //FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddHours(1), true,
                //    JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookieName);

                HttpCookie test = FormsAuthentication.GetAuthCookie(userData.sysUserId.ToString(), true);
                string encTicket = FormsAuthentication.Encrypt(ticket); //加密

                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                cookie.HttpOnly = true;
                Response.Cookies.Add(cookie);
                #endregion

                jo.Add("GoPage", "/System/Index");
                jo.Add("resultCode", resultCode);
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
                jo.Add("resultCode", resultCode);
                jo.Add("error", errStr);
            }

            #region operLog
            DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
            DataRow operDr = tbl_OperLog.NewRow();
            operDr["actSerial"] = userData.actSerial;
            operDr["logDate"] = DateTime.UtcNow;
            operDr["sysUserId"] = userData.sysUserId;
            operDr["clientIp"] = Request.ServerVariables["REMOTE_ADDR"];
            operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
            operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
            operDr["statusId"] = "10";
            operDr["resultCode"] = resultCode;
            operDr["errMsg"] = errStr;
            //if (resultCode == "01") //operDr["strSql"] = strSql;

            tbl_OperLog.Rows.Add(operDr);
            errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }

        //第二版 兩個登入情境
        public ActionResult Login2(string userId, string password, string S3)
        {
            JObject jo = new JObject();
            string strSql = "";
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            try
            {
                //userId COLLATE Latin1_General_CS_AI = 分字母大小寫判斷
                strSql = " select t1.sysUserId, t1.userId, t1.userName from S10_users t1 with (nolock) where userId COLLATE Latin1_General_CS_AI = '" + userId + "' and password=N'" + ZhClass.DesClass.DESEncrypt3(password) + "' and statusId='10' and appSysId='00' ";
                DataTable tmpTbl1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTbl1");

                if (tmpTbl1.Rows.Count == 0)
                {
                    strSql = " select t1.sysUserId, t1.userId, t1.userName from S10_users t1 with (nolock) where userId COLLATE Latin1_General_CS_AI = '" + userId + "' and password='" + password + "' and statusId='10' and appSysId='00' ";
                    tmpTbl1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTbl1");
                    if (tmpTbl1.Rows.Count == 0)
                    {
                        if (S3 == "Y")
                        {
                            jo.Add("firstLogin", "Y");
                            jo.Add("resultCode", resultCode);
                            return Content(JsonConvert.SerializeObject(jo), "application/json");
                        }
                        else
                        {
                            errStr = "The uerId or password is incorrect. Please re-enter.(Please check capitals and lower case letters)。";
                            //errStr = "帳號或密碼錯誤，請重新輸入(請檢查大小寫)。";
                            throw new Exception(errStr);
                        }
                    }
                }

                #region 可以登入
                userData.sysUserId = Convert.ToInt32(tmpTbl1.Rows[0]["sysUserId"].ToString());
                userData.userId = tmpTbl1.Rows[0]["userId"].ToString();
                userData.userName = tmpTbl1.Rows[0]["userName"].ToString();
                userData.loginTime = DateTime.Now;
                userData.groups = GetGroupsStr(userData.sysUserId.ToString());
                userData.dcId = GetDcStr(userData.sysUserId.ToString());
                userData.actSerial = Convert.ToInt32(ZhClass.AutoSerialNoType2.A_GetAutoSerial("S90_userLog", "actSerial"));
                #endregion
                if (tmpTbl1.Rows.Count > 0)
                {
                    #region 登入密碼重新加密
                    strSql = " Update S10_users set password=@password, actTime=getDate() where sysUserId=@sysUserId ";
                    SqlParameter[] param =
                    {
                            new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ZhClass.DesClass.DESEncrypt3(password)),
                            new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId)
                        };
                    errStr = ZhClass.SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                    #endregion
                }
                else
                {
                    errStr = "The uerId or password is incorrect. Please re-enter.(Please check capitals and lower case letters).";
                    throw new Exception(errStr);
                }

                jo.Add("firstLogin", "N");

                #region user Login Log
                DataTable tbl_userLog = userData.Get_tbl_userLogV1();
                DataRow userDr = tbl_userLog.NewRow();
                userDr["actSerial"] = userData.actSerial;
                userDr["sysUserId"] = userData.sysUserId;
                userDr["statusType"] = "DP";
                userDr["statusId"] = "00";
                userDr["clientIp"] = Request.ServerVariables["REMOTE_ADDR"];
                userDr["creatUser"] = userData.sysUserId;

                tbl_userLog.Rows.Add(userDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveUserLog(tbl_userLog);
                if (errStr != "") throw new Exception(errStr);
                #endregion

                #region Auth identify
                //string jUserData = JsonConvert.SerializeObject(userData);
                ////調整為24小時後失效 20180822 By.Ray
                //FormsAuthentication.SetAuthCookie(userData.sysUserId.ToString(), true, FormsAuthentication.FormsCookiePath);
                //FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddDays(1), true,JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookiePath);
                //FormsIdentity identity = new FormsIdentity(ticket);
                ////FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddHours(1), true,
                ////    JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookieName);

                //string encTicket = FormsAuthentication.Encrypt(ticket); //加密

                //var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                //cookie.HttpOnly = true;
                //Response.Cookies.Add(cookie);
                #endregion

                #region Auth identify V2
                string jUserData = JsonConvert.SerializeObject(userData);
                //調整為24小時後失效 20180822 By.Ray
                FormsAuthentication.SetAuthCookie(userData.sysUserId.ToString(), true, FormsAuthentication.FormsCookiePath);
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddDays(1), true, JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookiePath);
                FormsIdentity identity = new FormsIdentity(ticket);
                //FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddHours(1), true,
                //    JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookieName);

                string encTicket = FormsAuthentication.Encrypt(ticket); //加密
                Session["ticket"] = encTicket;

                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                cookie.HttpOnly = true;
                Response.Cookies.Add(cookie);
                #endregion

                jo.Add("GoPage", "/System/Index");
                jo.Add("resultCode", resultCode);
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
                jo.Add("resultCode", resultCode);
                jo.Add("error", errStr);
            }

            #region operLog
            DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
            DataRow operDr = tbl_OperLog.NewRow();
            operDr["actSerial"] = userData.actSerial;
            operDr["logDate"] = DateTime.UtcNow;
            operDr["sysUserId"] = userData.sysUserId;
            operDr["clientIp"] = Request.ServerVariables["REMOTE_ADDR"];
            operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
            operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
            operDr["statusId"] = "10";
            operDr["resultCode"] = resultCode;
            operDr["errMsg"] = errStr;
            //if (resultCode == "01") //operDr["strSql"] = strSql;

            tbl_OperLog.Rows.Add(operDr);
            errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }

        public ActionResult webLogin()
        {
            return View();
        }

        [HttpPost]
        public ActionResult WebLogin(string userId, string password)
        {
            JObject jo = new JObject();
            string strSql = "";
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            try
            {
                //userId COLLATE Latin1_General_CS_AI = 分字母大小寫判斷
                strSql = " select t1.sysUserId, t1.userId, t1.userName from S10_users t1 with (nolock) where userId COLLATE Latin1_General_CS_AI = '" + userId + "' and password=N'" + ZhClass.DesClass.DESEncrypt3(password) + "' and statusId='10' and appSysId='10' ";
                DataTable tmpTbl1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTbl1");

                if (tmpTbl1.Rows.Count == 0)
                {
                    strSql = " select t1.sysUserId, t1.userId, t1.userName from S10_users t1 with (nolock) where userId COLLATE Latin1_General_CS_AI = '" + userId + "' and password='" + password + "' and statusId='10' and appSysId='10' ";
                    tmpTbl1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTbl1");
                    if (tmpTbl1.Rows.Count == 0)
                    {
                        errStr = "The uerId or password is incorrect. Please re-enter.(Please check capitals and lower case letters).";
                        //errStr = "帳號或密碼錯誤，請重新輸入(請檢查大小寫)。";
                        throw new Exception(errStr);
                    }
                }

                #region 可以登入
                userData.sysUserId = Convert.ToInt32(tmpTbl1.Rows[0]["sysUserId"].ToString());
                userData.userId = tmpTbl1.Rows[0]["userId"].ToString();
                userData.userName = tmpTbl1.Rows[0]["userName"].ToString();
                userData.loginTime = DateTime.Now;
                userData.groups = GetGroupsStr(userData.sysUserId.ToString());
                userData.dcId = GetDcStr(userData.sysUserId.ToString());
                userData.actSerial = Convert.ToInt32(ZhClass.AutoSerialNoType2.A_GetAutoSerial("S90_userLog", "actSerial"));
                #endregion
                if (tmpTbl1.Rows.Count > 0)
                {
                    #region 登入密碼重新加密
                    strSql = " Update S10_users set password=@password, actTime=getDate() where sysUserId=@sysUserId ";
                    SqlParameter[] param =
                    {
                            new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ZhClass.DesClass.DESEncrypt3(password)),
                            new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId)
                        };
                    errStr = ZhClass.SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                    #endregion
                }
                else
                {
                    errStr = "The uerId or password is incorrect. Please re-enter.(Please check capitals and lower case letters).";
                    throw new Exception(errStr);
                }

                #region user Login Log
                DataTable tbl_userLog = userData.Get_tbl_userLogV1();
                DataRow userDr = tbl_userLog.NewRow();
                userDr["actSerial"] = userData.actSerial;
                userDr["sysUserId"] = userData.sysUserId;
                userDr["statusType"] = "DP";
                userDr["statusId"] = "00";
                userDr["clientIp"] = Request.ServerVariables["REMOTE_ADDR"];
                userDr["creatUser"] = userData.sysUserId;

                tbl_userLog.Rows.Add(userDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveUserLog(tbl_userLog);
                if (errStr != "") throw new Exception(errStr);
                #endregion

                #region Auth identify
                //string jUserData = JsonConvert.SerializeObject(userData);
                ////調整為24小時後失效 20180822 By.Ray
                //FormsAuthentication.SetAuthCookie(userData.sysUserId.ToString(), true, FormsAuthentication.FormsCookiePath);
                //FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddDays(1), true,JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookiePath);
                //FormsIdentity identity = new FormsIdentity(ticket);
                ////FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddHours(1), true,
                ////    JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookieName);

                //string encTicket = FormsAuthentication.Encrypt(ticket); //加密

                //var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                //cookie.HttpOnly = true;
                //Response.Cookies.Add(cookie);
                #endregion

                #region Auth identify V2
                string jUserData = JsonConvert.SerializeObject(userData);
                //調整為24小時後失效 20180822 By.Ray
                FormsAuthentication.SetAuthCookie(userData.sysUserId.ToString(), true, FormsAuthentication.FormsCookiePath);
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddDays(1), true, JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookiePath);
                FormsIdentity identity = new FormsIdentity(ticket);
                //FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddHours(1), true,
                //    JsonConvert.SerializeObject(userData), FormsAuthentication.FormsCookieName);

                string encTicket = FormsAuthentication.Encrypt(ticket); //加密
                Session["ticket"] = encTicket;

                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                cookie.HttpOnly = true;
                Response.Cookies.Add(cookie);
                #endregion

                jo.Add("GoPage", "/MorrisonWEB/M10");
                jo.Add("resultCode", resultCode);
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
                jo.Add("resultCode", resultCode);
                jo.Add("error", errStr);
            }

            #region operLog
            DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
            DataRow operDr = tbl_OperLog.NewRow();
            operDr["actSerial"] = userData.actSerial;
            operDr["logDate"] = DateTime.UtcNow;
            operDr["sysUserId"] = userData.sysUserId;
            operDr["clientIp"] = Request.ServerVariables["REMOTE_ADDR"];
            operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
            operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
            operDr["statusId"] = "10";
            operDr["resultCode"] = resultCode;
            operDr["errMsg"] = errStr;
            //if (resultCode == "01") //operDr["strSql"] = strSql;

            tbl_OperLog.Rows.Add(operDr);
            errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }
        public ActionResult ActSingleLogin(Models.Base.MD_Login actRow)
        {
            JObject jo = new JObject();
            try
            {
                actRow.userId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userId);
                actRow.password = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.password);
                actRow.userName = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userName);
                actRow.email = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.email);
                actRow.contactTel = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.contactTel);
                actRow.zip = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.zip);
                actRow.addr = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.addr);

                SqlParameter[] param =
                {
                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, null),
                    new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "00"),
                    new SqlParameter("userId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userId),
                    new SqlParameter("userName", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userName),
                    new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ZhClass.DesClass.DESEncrypt3(actRow.password.ToString())),
                    new SqlParameter("countryCode", SqlDbType.SmallInt, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  1),
                    new SqlParameter("contactTel", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.contactTel),
                    new SqlParameter("email", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.email),
                    new SqlParameter("zip", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.zip),
                    new SqlParameter("addr", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.addr),
                    new SqlParameter("statusType", SqlDbType.Char,2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "N0"),
                    new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                    new SqlParameter("statusType2", SqlDbType.Char,2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "UT"),
                    new SqlParameter("statusId2", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                    new SqlParameter("deviceLimit", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, 1),
                };

                string strSql = " INSERT INTO S10_users (appSysId,userId,userName,password,countryCode,statusType,statusId,deviceLimit,statusType2,statusId2,email,zip,addr,contactTel) VALUES (@appSysId,@userId,@userName,@password,@countryCode,@statusType,@statusId,@deviceLimit,@statusType2,@statusId2,@email,@zip,@addr,@contactTel) ";
                strSql += " SELECT @sysUserId = SCOPE_IDENTITY() ";
                errStr = ZhClass.SqlTool.ExecuteNonQuery(ZhConfig.GlobalSystemVar.StrConnection1, strSql, param);
                if (errStr != "") throw new Exception(errStr);
                //站點
                //strSql = " INSERT INTO C10_dcPerson (dcId,sysUserId,personId,statusType,statusx) VALUES ('ORD','" + param[0].Value + "','a','N0','10') ";
                //errStr = ZhClass.SqlTool.ExecuteNonQuery(ZhConfig.GlobalSystemVar.StrConnection1, strSql);
                //if (errStr != "") throw new Exception(errStr);

                jo.Add("resultCode", "10");
            }
            catch (Exception ex)
            {
                jo.Add("error", ex.Message);
                jo.Add("resultCode", "01");
            }

            return Content(JsonConvert.SerializeObject(jo), "application/json");

        }

        public ActionResult Logout()
        {

            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

            try
            {
                if (HttpContext.User.Identity.IsAuthenticated)
                {
                    #region save user LogOut
                    Session.Remove("ticket");
                    FormsAuthentication.SignOut();
                    #endregion
                }
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            #region operLog 當使用者登入的時間逾時，便不在記錄log
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = Request.ServerVariables["REMOTE_ADDR"];
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "90";
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return RedirectToAction("Index", "Home");
        }

        private string GetGroupsStr(string sysUserId)
        {
            string strSql = "";
            string groups = "";
            strSql = " select * from S10_userGroups with (nolock) where sysUserId='" + sysUserId + "' ";
            DataTable tmpTbl1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTbl1");

            foreach (DataRow dr in tmpTbl1.Rows)
            {
                groups += dr["groupId"].ToString() + ",";
            }

            if (groups != "") groups = groups.Substring(0, groups.Length - 1);
            return groups;
        }


        private string GetDcStr(string sysUserId)
        {
            string strSql = "";
            string dcId = "";
            strSql = " select * from C10_dcPerson with (nolock) where sysUserId='" + sysUserId + "' ";
            DataTable tmpTbl1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTbl1");

            foreach (DataRow dr in tmpTbl1.Rows)
            {
                dcId += dr["dcId"].ToString() + ",";
            }

            if (dcId != "") dcId = dcId.Substring(0, dcId.Length - 1);
            return dcId;
        }
    }
}
