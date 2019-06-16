using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Text;
using System.Web.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZhClass;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.ServiceModel.Channels;
using System.Net.Mail;
using System.Transactions;

namespace MorrisonServer.Controllers.api
{
    [BasicAuthentication.Filters.BasicAuthentication] // Enable authentication
    [Authorize]

    public class ZhSysController : ApiController
    {
        string resultCode = "10";
        string errStr = "";
        StringBuilder strSql = new StringBuilder(200);
        string clientIp = "";
        string operStatusId2 = "10";

        #region Api Logion/Logout
        [AllowAnonymous]
        [HttpPost]
        public HttpResponseMessage S0_AppLogin(Models.Base.MD_S10_users actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            StringBuilder logStrSql = new StringBuilder();
            try
            {
                #region ACall_checkIsDBNull 
                actRow.appSysId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.appSysId);
                actRow.sysUserId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.sysUserId);
                actRow.password = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.password);
                actRow.UUID = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.UUID);
                actRow.model = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.model);
                actRow.platform = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.platform);
                actRow.appVer = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.appVer);
                #endregion

                #region Check 使用者帳密&UUID
                strSql.Clear();
                strSql.AppendLine(" select t1.sysUserId,t1.userName, t1.deviceLimit, t1.contactTel, t1.email, t1.userTitle, t3.dcId, t3.tranCompId, t3.carId, t3.trailerId, t4.tranCompName ");
                strSql.AppendLine(" , case when t2.sysUserId is null then 0 else 1 end isExist from S10_users t1 ");
                strSql.AppendLine(" left join S10_regLog t2 on t2.sysUserId = t1.sysUserId ");
                strSql.AppendLine(" left join T10_driver t3 on t3.sysUserId=t1.sysUserId ");
                strSql.AppendLine(" left join T10_tranComp t4 on t4.tranCompId=t3.tranCompId ");
                //strSql.AppendLine(" select sysUserId,userName, deviceLimit, contactTel, email, userTitle from S10_users WITH (NOLOCK) ");
                strSql.AppendLine(" where t1.appSysId='" + actRow.appSysId + "'");
                strSql.Append(" and t1.userId COLLATE Latin1_General_CS_AI ='" + actRow.userId + "'"); //判斷大小寫  COLLATE Latin1_General_CS_AI 需相符 20180820 By.Ray
                //strSql.Append(" and t1.userId ='" + actRow.userId + "'");
                strSql.Append(" and t1.password=N'" + ZhClass.DesClass.DESEncrypt3(actRow.password.ToString()) + "' ");
                //strSql.Append(" and password=N'" + ZhClass.DesClass.DESEncrypt3(actRow.password.ToString()) + "' ");
                strSql.Append(" and t1.statusId='10' and t1.appSysId='10' ");
                DataTable tmpTbl = SqlTool.GetDataTable(strSql.ToString(), "App_users");

                if (tmpTbl.Rows.Count == 0)
                {
                    strSql.Clear();
                    strSql.AppendLine(" select t1.sysUserId,t1.userName, t1.deviceLimit, t1.contactTel, t1.email, t1.userTitle, t3.dcId, t3.tranCompId, t3.carId, t3.trailerId, t4.tranCompName ");
                    strSql.AppendLine(" , case when t2.sysUserId is null then 0 else 1 end isExist from S10_users t1 ");
                    strSql.AppendLine(" left join S10_regLog t2 on t2.sysUserId = t1.sysUserId ");
                    strSql.AppendLine(" left join T10_driver t3 on t3.sysUserId=t1.sysUserId ");
                    strSql.AppendLine(" left join T10_tranComp t4 on t4.tranCompId=t3.tranCompId ");
                    //strSql.AppendLine(" select sysUserId,userName, deviceLimit, contactTel, email, userTitle from S10_users WITH (NOLOCK) ");
                    strSql.AppendLine(" where t1.appSysId='" + actRow.appSysId + "'");
                    strSql.Append(" and t1.userId COLLATE Latin1_General_CS_AI ='" + actRow.userId + "'"); //判斷大小寫  COLLATE Latin1_General_CS_AI 需相符 20180820 By.Ray
                                                                                                           //strSql.Append(" and t1.userId ='" + actRow.userId + "'");
                    strSql.Append(" and t1.password='" + actRow.password.ToString() + "' ");
                    //strSql.Append(" and password=N'" + ZhClass.DesClass.DESEncrypt3(actRow.password.ToString()) + "' ");
                    strSql.Append(" and t1.statusId='10' and t1.appSysId='10' ");
                    tmpTbl = SqlTool.GetDataTable(strSql.ToString(), "App_users");
                    if (tmpTbl.Rows.Count == 0)
                    {
                        throw new Exception("The account or password is incorrect. Login application failed!!");
                    }
                }

                DateTime dt = DateTime.Now;

                logStrSql.AppendLine(strSql.ToString());

                //可登入帳號的上限數
                if (tmpTbl.Rows.Count > 0) //可以登入
                {
                    #region 登入密碼重新加密
                    strSql.Clear();
                    strSql.Append("Update S10_users set password=@password, actTime=getDate() where sysUserId=@sysUserId");
                    SqlParameter[] param =
                    {
                            new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ZhClass.DesClass.DESEncrypt3(actRow.password.ToString())),
                            new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tmpTbl.Rows[0]["sysUserId"].ToString())
                        };
                    errStr = ZhClass.SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                    if (errStr != "") throw new Exception(errStr);
                    #endregion


                    int deviceLimit = (tmpTbl.Rows[0]["deviceLimit"].ToString() == "" ? 1 : int.Parse(tmpTbl.Rows[0]["deviceLimit"].ToString()));
                    if (deviceLimit > 0)  //判斷帳號可登入的數量
                    {
                        strSql.Clear();
                        strSql.Append(" select * from App_userDevice WITH (NOLOCK) ");
                        strSql.Append(" where sysUserId='" + tmpTbl.Rows[0]["sysUserId"].ToString() + "' ");
                        DataTable userDevice = SqlTool.GetDataTable(strSql.ToString(), "userDevice");
                        logStrSql.AppendLine(strSql.ToString());

                        strSql.Clear();
                        if (userDevice.Rows.Count > 0)  //判斷帳號是否曾經登入
                        {
                            DataRow[] drUserDevice = userDevice.Select("UUID='" + actRow.UUID + "'");
                            if (drUserDevice.Length > 0)   //判斷是否為先前登入的裝置
                            {
                                DataRow[] drUser = userDevice.Select("UUID='" + actRow.UUID + "' and isUse='True'");
                                if (drUser.Length > 0)   //使用狀態是否為正常
                                {
                                    strSql.Append(" Update App_userDevice set appVer='" + actRow.appVer.ToString() + "', actTime=getDate(), actUser='" + tmpTbl.Rows[0]["sysUserId"].ToString() + "' where sysUserId='" + tmpTbl.Rows[0]["sysUserId"].ToString() + "' and UUID='" + actRow.UUID.ToString() + "' ");
                                }
                                else
                                {
                                    throw new Exception("Login fail,this account not allow this device login");
                                    //throw new Exception("登入失敗，帳號不允許此裝置登入");
                                }
                            }
                            else
                            {
                                #region 移除登入裝置數
                                //if (deviceLimit > userDevice.Rows.Count)
                                //{
                                strSql.Append(" Insert into App_userDevice (sysUserId, platform, model, UUID, isUse, appVer, creatTime, creatUser) ");
                                strSql.AppendLine(" values ('" + tmpTbl.Rows[0]["sysUserId"].ToString() + "', '" + actRow.platform + "','" + actRow.model + "','" + actRow.UUID + "','True', '" + actRow.appVer + "', '" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "','" + tmpTbl.Rows[0]["sysUserId"].ToString() + "') ");
                                //}
                                //else
                                //{
                                //    throw new Exception("該帳號登入的裝置已超出上限數量，不允許登入");
                                //}
                                #endregion
                            }
                        }
                        else
                        {
                            strSql.Append(" Insert into App_userDevice (sysUserId, platform, model, UUID, isUse, appVer, creatTime, creatUser) ");
                            strSql.AppendLine(" values ('" + tmpTbl.Rows[0]["sysUserId"].ToString() + "', '" + actRow.platform + "','" + actRow.model + "','" + actRow.UUID + "','True', '" + actRow.appVer + "', '" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "','" + tmpTbl.Rows[0]["sysUserId"].ToString() + "') ");
                        }

                        errStr = SqlTool.ExecuteNonQuery(strSql.ToString());
                        if (errStr != "") throw new Exception(errStr);
                        logStrSql.AppendLine(strSql.ToString());

                    }
                    else
                    {
                        resultCode = "01";
                        errStr = "You are not eligible to log in. Please contact your system administrator and be eligible to log in.";
                        //errStr = "您未符合登入資格，請聯絡系統管理員，並開通登入資格";
                    }
                }
                else
                {
                    throw new Exception("The account or password is incorrect. Login application failed!!");
                    //throw new Exception("帳號或密碼有誤，登入App失敗 !!");
                }

                if (errStr != "") throw new Exception(errStr);
                #endregion

                #region 進行權限加密
                userData.actSerial = Convert.ToInt32(ZhClass.AutoSerialNoType2.A_GetAutoSerial("S90_userLog", "actSerial"));
                userData.sysUserId = Convert.ToInt32(tmpTbl.Rows[0]["sysUserId"].ToString());
                userData.userId = actRow.userId.ToString();
                userData.userName = tmpTbl.Rows[0]["userName"].ToString();
                userData.loginTime = DateTime.Now;
                userData.platform = actRow.platform.ToString();
                userData.model = actRow.model.ToString();
                userData.UUID = actRow.UUID.ToString();
                userData.contactTel = tmpTbl.Rows[0]["contactTel"].ToString();


                #region user Login Log
                DataTable tbl_userLog = userData.Get_tbl_userLogV1();
                DataRow userDr = tbl_userLog.NewRow();
                userDr["actSerial"] = userData.actSerial;
                userDr["sysUserId"] = userData.sysUserId;
                userDr["statusType"] = "DP";
                userDr["statusId"] = "10";
                userDr["clientIp"] = clientIp;
                userDr["platform"] = userData.platform;
                userDr["model"] = userData.model;
                userDr["UUID"] = userData.UUID;
                userDr["creatUser"] = userData.sysUserId;

                tbl_userLog.Rows.Add(userDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveUserLog(tbl_userLog);
                if (errStr != "") throw new Exception(errStr);
                #endregion


                #region Auth identify
                string jUserData = JsonConvert.SerializeObject(userData);
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(1, userData.sysUserId.ToString(), DateTime.Now, DateTime.Now.AddHours(1), true,
                    jUserData, FormsAuthentication.FormsCookieName);

                string encTicket = FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
                cookie.Path = FormsAuthentication.FormsCookiePath;
                HttpContext.Current.Response.AppendCookie(cookie);
                #endregion

                jo.Add("ticket", encTicket);
                jo.Add("sysUserId", userData.sysUserId);
                jo.Add("email", tmpTbl.Rows[0]["email"].ToString());
                jo.Add("contactTel", tmpTbl.Rows[0]["contactTel"].ToString());
                jo.Add("userName", tmpTbl.Rows[0]["userName"].ToString());
                jo.Add("userTitle", tmpTbl.Rows[0]["userTitle"].ToString());
                jo.Add("isExist", tmpTbl.Rows[0]["isExist"].ToString());
                jo.Add("dcId", tmpTbl.Rows[0]["dcId"].ToString());
                jo.Add("tranCompId", tmpTbl.Rows[0]["tranCompId"].ToString());
                jo.Add("carId", tmpTbl.Rows[0]["carId"].ToString());
                jo.Add("trailerId", tmpTbl.Rows[0]["trailerId"].ToString());
                jo.Add("tranCompName", tmpTbl.Rows[0]["tranCompName"].ToString());
                #endregion
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);


            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "10";
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = logStrSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");

            return resp;
        }

        [HttpPost]
        public HttpResponseMessage S0_AppLogOut(Models.Base.MD_S10_users actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion
                if (userData != null)
                {
                    #region 登出後清除uuid
                    strSql.Clear();
                    strSql.Append(" Update App_userDevice set RegId=NULL, TokenId=NULL where sysUserId='" + userData.sysUserId.ToString() + "' and UUID='" + userData.UUID.ToString() + "' ");

                    errStr = SqlTool.ExecuteNonQuery(strSql.ToString());
                    if (errStr != "") throw new Exception(errStr);
                    #endregion
                }

            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "90";
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion


        #region S0_AppConfirmVersion 版本確認
        [HttpPost]
        public HttpResponseMessage S0_AppConfirmVersion(Models.Base.MD_S00_systemConfig actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion

                #region Confirm Version 版本判斷

                strSql.Clear();
                strSql.Append(" select * from  S00_systemConfig WITH (NOLOCK) where appSysId='" + actRow.appSysId.ToString() + "' ");
                DataTable tmpTbl = SqlTool.GetDataTable(strSql.ToString(), "S00_systemConfig");

                if (string.Compare(actRow.version.ToString(), tmpTbl.Rows[0]["version"].ToString().Trim()) >= 0)
                {

                }
                else
                {
                    errStr = "Your app verion is：" + actRow.version.ToString() + "\nOfficial Version is：" + tmpTbl.Rows[0]["version"].ToString() + "\nPlease update your app version to use！";
                    //errStr = "您目前的版本為：" + actRow.version.ToString() + "\n正式版本為：" + tmpTbl.Rows[0]["version"].ToString() + "\n請更新版本後再繼續使用！";
                    System.Web.Security.FormsAuthentication.SignOut();
                    throw new Exception(errStr);
                }

                //if (model.version.ToString() != tmpTbl.Rows[0]["version"].ToString().Trim())
                //{
                //    resultCode = "01";
                //    errStr = "您目前的版本為：" + model.version + "\n正式版本為：" + tmpTbl.Rows[0]["version"].ToString() + "\n請更新版本後再繼續使用！";
                //    System.Web.Security.FormsAuthentication.SignOut();
                //}
                #endregion
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;

            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "20";
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region S0_AppConfirmAuth 版本確認
        [HttpPost]
        public HttpResponseMessage S0_AppConfirmAuth(Models.Base.MD_S00_systemConfig actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion

                #region 權限驗證
                if (userData == null || userData.userId == "")
                {
                    throw new Exception("permisson lost,Please reLogin");
                    //throw new Exception("權限已遺失，重新登入");
                }
                #endregion
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "20";
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region S0_GetAppUpdateUrl 版本確認
        [HttpPost]
        public HttpResponseMessage S0_GetAppUpdateUrl(Models.Base.MD_S00_systemConfig actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion

                strSql.Clear();
                strSql.Append(" select * from S00_systemConfigLink WITH (NOLOCK) where appSysId='" + actRow.appSysId.ToString() + "' and platform='" + actRow.platform.ToString() + "' ");
                DataTable tbl_AppUrl = SqlTool.GetDataTable(strSql.ToString(), "tbl_AppUrl");
                if (tbl_AppUrl.Rows.Count == 0)
                {
                    throw new Exception("not offer download url,Please contact admin person");
                    //throw new Exception("未提供下載網址，請聯絡管理人員");
                }
                else
                {
                    if (tbl_AppUrl.Rows[0]["url"].ToString().Trim() == "")
                    {
                        throw new Exception("not offer download url,Please contact admin person");
                        //throw new Exception("未提供下載網址，請聯絡管理人員");
                    }

                    jo.Add("url", tbl_AppUrl.Rows[0]["url"].ToString());
                }
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "20";
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region  S1_RegAppUser 使用者帳號申請
        [HttpPost]
        public HttpResponseMessage S1_RegAppUser(Models.Base.MD_S10_regLog actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion

                #region 確認使用者帳戶是否重覆申請
                strSql.Clear();
                strSql.Append(" select count(*) from S10_users where userId='" + actRow.userId.ToString() + "' and password=N'" + actRow.password.ToString() + "' and statusId in ('10', '20') ");
                //strSql.Append(" select count(*) from S10_users where userId='" + actRow.userId.ToString() + "' and password=N'" + ZhClass.DesClass.DESEncrypt3(actRow.password.ToString()) + "' and statusId in ('10', '20') ");
                int isExist = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(strSql.ToString()));

                if (isExist > 0) throw new Exception("this account has been register");
                //if (isExist > 0) throw new Exception("該用戶已申請過，請確認");
                #endregion

                #region 確認信箱是否已重覆申請
                strSql.Clear();
                strSql.Append(" select count(*) from S10_users where email='" + actRow.email.ToString() + "' and statusId in ('10', '20') ");
                isExist = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(strSql.ToString()));

                if (isExist > 0) throw new Exception("this email has been register");
                //if (isExist > 0) throw new Exception("該信箱已申請過，請確認");
                #endregion

                #region ACall_checkIsDBNull 
                actRow.appSysId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.appSysId);
                actRow.userId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userId);
                actRow.password = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.password);
                actRow.userName = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userName);
                actRow.contactTel = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.contactTel);
                actRow.email = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.email);
                actRow.UUID = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.UUID);
                actRow.model = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.model);
                actRow.platform = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.platform);
                actRow.statusId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusId);
                actRow.statusId2 = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusId2);
                actRow.verifyCode = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.verifyCode);
                actRow.memo = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.memo);
                #endregion

                actRow.verifyCode = GetRandomNum(4);

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                    new SqlParameter("rowId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, "0"),
                    new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.appSysId),
                    new SqlParameter("userId", SqlDbType.Char, 10, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userId),
                    new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.password.ToString()),
                    //new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ZhClass.DesClass.DESEncrypt3(actRow.password.ToString())),
                    new SqlParameter("userName", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userName),
                    new SqlParameter("contactTel", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.contactTel),
                    new SqlParameter("email", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.email),
                    new SqlParameter("UUID", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.UUID),
                    new SqlParameter("model", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.model),
                    new SqlParameter("platform", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.platform),
                    new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.statusId),
                    new SqlParameter("statusId2", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.statusId2),
                    new SqlParameter("verifyCode", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.verifyCode),
                    new SqlParameter("memo", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.memo)};
                #endregion

                strSql.Clear();
                strSql.AppendLine(" insert into S10_regLog (appSysId,userId,password,userName,contactTel,email,UUID,model,platform,statusId2,verifyCode,memo, creatTime) ");
                strSql.AppendLine(" values (@appSysId,@userId,@password,@userName,@contactTel,@email,@UUID,@model,@platform, @statusId2,@verifyCode,@memo, getDate())");
                strSql.AppendLine(" SELECT @rowId = SCOPE_IDENTITY()");

                errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                if (errStr != "") throw new Exception(errStr);

                jo.Add("rowId", param[0].Value.ToString());

                if (actRow.statusId2.ToString() == "10")
                {
                    string subject = "zhtech App register";
                    //string subject = "展輝App申請";
                    string content = "Thank you for register zhtech App<br/>Your Verification code：<span style=\"font-size:20px;color:red;\">" + actRow.verifyCode.ToString() + "</span>";
                    //string content = "感謝您申請展輝App<br/>您的驗證碼為：<span style=\"font-size:20px;color:red;\">" + actRow.verifyCode.ToString() + "</span>";
                    Send_FileToEmail(actRow.email.ToString(), subject, content, "");
                }
                else
                {
                    string content = "Thank you for register zhtech app,Verification code is：" + actRow.verifyCode;
                    //string content = "感謝您申請App，驗證碼為：" + actRow.verifyCode;
                    Send_CodeToPhone(actRow.contactTel.ToString(), content);
                }
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "30";
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion



        #region S1_ChkRegAppUser 驗證App使用者
        [HttpPost]
        public HttpResponseMessage S1_ChkRegAppUser(Models.Base.MD_S10_regLog actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            StringBuilder tmpStrSql = new StringBuilder(200);
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion


                #region ACall_checkIsDBNull 
                actRow.rowId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.rowId);
                actRow.userId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userId);
                actRow.password = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.password);
                actRow.verifyCode = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.verifyCode);
                #endregion

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        strSql.Clear();
                        if (actRow.rowId.ToString() != "")
                        {
                            strSql.Append(" select * from S10_regLog where rowId='" + actRow.rowId.ToString() + "' and GETDATE() < DATEADD(HOUR,1, creatTime) and verifyCode='" + actRow.verifyCode.ToString() + "' and statusId='00' ");
                        }
                        else
                        {
                            strSql.Append(" select * from S10_regLog where userId='" + actRow.userId.ToString() + "' and password=N'" + actRow.password.ToString() + "' and statusId='00' and GETDATE() < DATEADD(HOUR,1, creatTime) ");
                            //strSql.Append(" select * from S10_regLog where userId='" + actRow.userId.ToString() + "' and password=N'" + ZhClass.DesClass.DESEncrypt3(actRow.password.ToString()) + "' and statusId='00' and GETDATE() < DATEADD(HOUR,1, creatTime) ");
                        }
                        tmpStrSql.AppendLine(strSql.ToString());

                        DataTable tbl_QueryData1 = ZhClass.SqlTool2.GetDataTable(cn, strSql.ToString(), "tbl_QueryData1");

                        if (tbl_QueryData1.Rows.Count > 0)
                        {
                            #region 設置 要傳入的 SqlParameter 資料
                            SqlParameter[] param = {
                                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, "0"),
                                    new SqlParameter("rowId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["rowId"].ToString()),
                                    new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["appSysId"].ToString()),
                                    new SqlParameter("userId", SqlDbType.Char, 10, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["userId"].ToString()),
                                    new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["password"].ToString()),
                                    new SqlParameter("userName", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["userName"].ToString()),
                                    new SqlParameter("contactTel", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["contactTel"].ToString()),
                                    new SqlParameter("email", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["email"].ToString()),
                                    new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                                    new SqlParameter("deviceLimit", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "1"),
                                    new SqlParameter("verifyCode", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.verifyCode),
                                    new SqlParameter("memo", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["memo"].ToString()),
                                    new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "0")
                                };
                            #endregion

                            strSql.Clear();
                            strSql.AppendLine(" insert into S10_users (appSysId,userId,password,userName,contactTel,email,statusId,deviceLimit,memo,creatUser,creatTime) ");
                            strSql.AppendLine(" values (@appSysId, @userId, @password, @userName, @contactTel, @email, @statusId, @deviceLimit, @memo, @creatUser, getDate()) ");
                            strSql.AppendLine(" select @sysUserId = SCOPE_IDENTITY()");
                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                            if (errStr != "") throw new Exception(errStr);
                            tmpStrSql.AppendLine(strSql.ToString());

                            actRow.sysUserId = param[0].Value.ToString();
                            actRow.rowId = param[1].Value.ToString();

                            strSql.Clear();
                            strSql.AppendLine(" Update S10_regLog set sysUserId='" + actRow.sysUserId.ToString() + "', statusId='10', verifyCreatTime=getDate() where rowId='" + actRow.rowId.ToString() + "' ");
                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString());
                            if (errStr != "") throw new Exception(errStr);
                            tmpStrSql.AppendLine(strSql.ToString());

                        }
                        else
                        {
                            throw new Exception("enter information incorrect or has exceeded the verification time ");
                            //throw new Exception("輸入資料不正確，或者已超過驗證時間。");
                        }

                    }
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "30";  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = tmpStrSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region S1_ForgetUserApp 忘記密碼申請
        [HttpPost]
        public HttpResponseMessage S1_ForgetUserApp(Models.Base.MD_S10_forgetLog actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion


                #region ACall_checkIsDBNull 
                actRow.rowId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.rowId);
                actRow.userId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userId);
                actRow.email = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.email);
                #endregion

                strSql.Clear();
                strSql.AppendLine(" select * from S10_users where userId='" + actRow.userId.ToString() + "' and email='" + actRow.email.ToString() + "' and statusId in ('10', '20') ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception("account or email incorrect,Please check.");
                //if (tbl_QueryData1.Rows.Count == 0) throw new Exception("帳號或信箱有誤，請確認");

                actRow.verifyCode = GetRandomNum(4);

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                    new SqlParameter("rowId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, "0"),
                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["sysUserId"].ToString()),
                    new SqlParameter("verifyCode", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.verifyCode)
                };
                #endregion

                strSql.Clear();
                strSql.AppendLine(" insert into S10_forgetLog (sysUserId, verifyCode, creatTime) values (@sysUserId, @verifyCode, getDate()) ");
                strSql.AppendLine(" select @rowId = SCOPE_IDENTITY()");
                errStr = ZhClass.SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                if (errStr != "") throw new Exception(errStr);

                string subject = "展輝App申請-忘記密碼";
                string content = "您申請了展輝App-忘記密碼功能<br/>您的驗證碼為：<span style=\"font-size:20px;color:red;\">" + actRow.verifyCode.ToString() + "</span>";
                Send_FileToEmail(actRow.email.ToString(), subject, content, "");

                jo.Add("rowId", param[0].Value.ToString());
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "30";  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region S1_ChkForgetUserApp 忘記密碼驗證碼確認
        [HttpPost]
        public HttpResponseMessage S1_ChkForgetUserApp(Models.Base.MD_S10_forgetLog actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion

                #region ACall_checkIsDBNull 
                actRow.rowId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.rowId);
                actRow.verifyCode = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.verifyCode);
                #endregion

                strSql.Clear();
                strSql.Append(" select * from S10_forgetLog where rowId='" + actRow.rowId.ToString() + "' and GETDATE() < DATEADD(HOUR,1, creatTime) and verifyCode='" + actRow.verifyCode.ToString() + "' and statusId='00' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count > 0)
                {
                    #region 設置 要傳入的 SqlParameter 資料
                    SqlParameter[] param = {
                                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, "0"),
                                    new SqlParameter("rowId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["rowId"].ToString()),
                                    new SqlParameter("verifyCode", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.verifyCode)
                                };
                    #endregion

                    strSql.Clear();
                    strSql.Append(" Update S10_forgetLog set statusId='10', verifyCreatTime=getDate() where rowId=@rowId ");
                    errStr = ZhClass.SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                    if (errStr != "") throw new Exception(errStr);
                }
                else
                {
                    throw new Exception("輸入資料不正確，或者已超過驗證時間。");
                }

                jo.Add("sysUserId", tbl_QueryData1.Rows[0]["sysUserId"].ToString());
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "20";  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region S1_ForgetPasswordChange 密碼重設
        [HttpPost]
        public HttpResponseMessage S1_ForgetPasswordChange(Models.Base.MD_S10_forgetLog actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion


                #region ACall_checkIsDBNull 
                actRow.sysUserId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.sysUserId);
                actRow.password = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.password);
                #endregion

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.sysUserId.ToString()),
                    new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.password.ToString())
                    //new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ZhClass.DesClass.DESEncrypt3(actRow.password.ToString()))
                };
                #endregion

                strSql.Clear();
                strSql.AppendLine(" Update S10_users set password=@password , actUser=@sysUserId, actTime=getDate() where sysUserId=@sysUserId ");

                errStr = ZhClass.SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                if (errStr != "") throw new Exception(errStr);
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "40";  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region M11_ActRegUserData 20180728 初次登入時回填的資料api
        [HttpPost]
        public HttpResponseMessage M11_ActRegUserData(Models.Base.MD_S10_regLog actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion


                #region ACall_checkIsDBNull 
                actRow.sysUserId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.sysUserId);
                actRow.password = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.password);
                actRow.email = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.email);
                actRow.dcId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dcId);
                actRow.tranCompId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.tranCompId);
                actRow.carId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.carId);
                actRow.trailerId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.trailerId);
                #endregion

                DateTime dtUtc = DateTime.UtcNow;

                if (string.IsNullOrEmpty(actRow.tranCompId.ToString())) actRow.tranCompId = "";
                if (string.IsNullOrEmpty(actRow.trailerId.ToString())) actRow.trailerId = "";

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.sysUserId.ToString()),
                    new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.password.ToString()),
                    new SqlParameter("email", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.email.ToString()),
                    new SqlParameter("dcId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.dcId.ToString()),
                    new SqlParameter("tranCompId", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.tranCompId.ToString()),
                    new SqlParameter("carId", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.carId.ToString()),
                    new SqlParameter("trailerId", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.trailerId.ToString()),
                    new SqlParameter("statusType", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "N0"),
                    new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                    new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                    new SqlParameter("creatTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                };
                #endregion

                strSql.Clear();
                strSql.AppendLine(" select count(*) from T10_car where dcId='" + actRow.dcId.ToString() + "' and tranCompId='" + actRow.tranCompId.ToString() + "' and carId='" + actRow.carId.ToString() + "' ");
                int car_isExist = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(strSql.ToString()).ToString());

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();
                        DataTable userInfo = SqlTool2.GetDataTable(cn, " select * from S10_users where sysUserId='" + actRow.sysUserId + "' ", "userInfo");
                        strSql.Clear();
                        strSql.AppendLine(" insert into S10_regLog (appSysId, userId, userName, password, contactTel, contactTel2, email, compName, userTitle, statusType, statusId, statusType2, statusId2, sysUserId, creatTime)   ");
                        strSql.AppendLine(" select appSysId, userId, userName, @password as password, contactTel, contactTel2, @email as email, compName, userTitle, 'RS', '10', 'VS', '00', sysUserId, @creatTime as creatTime from S10_users where sysUserId=@sysUserId  ");
                        strSql.AppendLine("");
                        strSql.AppendLine(" update S10_users set password=@password, email=@email, actUser=@actUser, actTime=@actTime where sysUserId=@sysUserId ");
                        strSql.AppendLine("");
                        if (car_isExist == 0)
                        {
                            strSql.AppendLine(" Insert into T10_car (dcId, tranCompId, carId, statusType, statusId, creatUser, creatTime) ");
                            strSql.AppendLine(" values (@dcId, @tranCompId, @carId, @statusType, @statusId, @creatUser, @creatTime) ");
                            strSql.AppendLine("");
                        }
                        strSql.AppendLine(" Update T10_driver set carId=@carId, trailerId=@trailerId, actUser=@actUser, actTime=@actTime where dcId=@dcId and tranCompId=@tranCompId and sysUserId=@sysUserId ");


                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);

                        cn.Close();
                    }
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = DateTime.UtcNow;
                operDr["sysUserId"] = userData.sysUserId;
                operDr["clientIp"] = clientIp;
                operDr["controllerName"] = this.ControllerContext.RouteData.Values["controller"].ToString();
                operDr["actionName"] = this.ControllerContext.RouteData.Values["action"].ToString();
                operDr["statusId"] = "40";  //10:登入系統、20:查詢資料、30:新增、40:修改、50:刪除、90:登出系統
                operDr["statusId2"] = operStatusId2;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            resp.StatusCode = HttpStatusCode.OK;
            resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
            return resp;
        }
        #endregion

        #region private Method
        #region GetRandoNum
        private static string GetRandomNum(int length)
        {
            string str = "";
            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                str += random.Next(0, 10);
            }
            return str;
        }
        #endregion

        #region GetClientIp
        private string GetClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region 透過Email傳送認證碼
        private void Send_FileToEmail(string email, string subject, string content, string fileRoute)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("zhtech@zhtech.com.tw");
            mail.To.Add(email);
            mail.IsBodyHtml = true;
            mail.SubjectEncoding = Encoding.GetEncoding("utf-8");
            mail.BodyEncoding = Encoding.GetEncoding("utf-8");
            mail.Subject = subject;
            mail.Body = content;

            if (fileRoute != "")
            {
                Attachment attachment = new Attachment(fileRoute);
                mail.Attachments.Add(attachment);
            }

            SmtpClient MySmtp = new SmtpClient("smtp.gmail.com", 587);


            MySmtp.Credentials = new NetworkCredential("zhtech@zhtech.com.tw", "24369238");
            MySmtp.EnableSsl = true;

            MySmtp.Send(mail);
            MySmtp = null;
            //MySmtp.Dispose();
        }
        #endregion

        #region 透過Phone傳送認證碼
        private string Send_CodeToPhone(string contactTel, string content)
        {
            string id = "";
            //HttpClient client = new HttpClient();
            //StringBuilder url = new StringBuilder("http://smexpress.mitake.com.tw:9600/SmSendGet.asp?");
            //url.Append("username=").Append("tmsapp");
            //url.Append("&password=").Append("0216740494");
            //url.Append("&dstaddr=").Append(contactTel);
            ////url.Append("&encoding=utf-16be");
            //url.Append("&smbody=").Append(HttpUtility.UrlEncode(content,
            //    Encoding.GetEncoding("Big5")));
            //var id = client.GetStringAsync(url.ToString()).Result;

            return id;
        }
        #endregion
        #endregion

    }
}
