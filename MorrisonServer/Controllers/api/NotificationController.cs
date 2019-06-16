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

namespace MorrisonServer.Controllers.api
{
    public class NotificationController : ApiController
    {
        string resultCode = "10";
        string errStr = "";
        StringBuilder strSql = new StringBuilder(200);
        string clientIp = "";
        string operStatusId2 = "10";

        #region T0_Sample 範例
        [HttpPost]
        public HttpResponseMessage A1_RegNotification(Models.NotiFactoryMD.MD_App_userDevice actRow)
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
                actRow.RegId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.RegId);
                actRow.TokenId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.TokenId);
                actRow.platform = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.platform);
                actRow.model = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.model);
                actRow.UUID = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.UUID);
                #endregion

                #region 設置 要傳入的 SqlParameter 資料
                strSql.Clear();
                SqlParameter[] param = {
                    new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.appSysId),
                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.sysUserId),
                    new SqlParameter("regId", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.RegId.ToString()),
                    new SqlParameter("tokenId", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.TokenId.ToString()),
                    new SqlParameter("platform", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.platform.ToString()),
                    new SqlParameter("model", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.model.ToString()),
                    new SqlParameter("UUID", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.UUID.ToString())
                                   };
                #endregion

                strSql.Clear();
                strSql.AppendLine(" Update App_userDevice set RegId=@RegId, TokenId=@TokenId, actUser=@sysUserId, actTime=getDate() ");
                strSql.AppendLine(" where sysUserId=@sysUserId and UUID=@UUID ");

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


        #region private Method
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
        #endregion
    }

}
