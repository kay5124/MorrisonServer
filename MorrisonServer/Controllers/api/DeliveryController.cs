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
using System.Transactions;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using System.Drawing;
using System.Drawing.Imaging;


namespace MorrisonServer.Controllers.api
{
    [BasicAuthentication.Filters.BasicAuthentication] // Enable authentication
    [Authorize]
    public class DeliveryController : ApiController
    {
        string resultCode = "10";
        string errStr = "";
        StringBuilder strSql = new StringBuilder(200);
        string clientIp = "";
        string operStatusId2 = "10";  //來源為 statusType = 


        #region T10_ActChangeTruck 20180728 修改車號
        [HttpPost]
        public HttpResponseMessage T10_ActChangeTruck(Models.api_Delivery.MD_T10_driver actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                #region ACall_checkIsDBNull 
                actRow.sysUserId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.sysUserId);
                actRow.dcId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dcId);
                actRow.tranCompId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.tranCompId);
                actRow.carId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.carId);
                actRow.trailerId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.trailerId);
                #endregion

                if (string.IsNullOrEmpty(actRow.trailerId.ToString())) actRow.trailerId = "";

                DateTime dtUtc = DateTime.UtcNow;

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.sysUserId.ToString()),
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

                        strSql.Clear();
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

        #region T30_GetCarShduData 20180729 修改車號
        [HttpPost]
        public HttpResponseMessage T30_GetCarShduData(Models.api_Delivery.MD_T30_carshdu actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                //userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                #region ACall_checkIsDBNull 
                actRow.dispatch_id = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dispatch_id);
                #endregion

                DateTime dtUtc = DateTime.UtcNow;

                strSql.Clear();
                strSql.AppendLine(" SELECT t1.*, t2.tranCompName, t2.addr, t2.zip, t3.countryName FROM T30_carshdu t1 ");
                strSql.AppendLine(" left join T10_tranComp t2 on t2.tranCompId=t1.tranCompId ");
                strSql.AppendLine(" left join C00_country t3 on t3.countryId=t2.countryId ");
                strSql.AppendLine(" where t1.dispatch_id='" + actRow.dispatch_id.ToString() + "' and t1.tranCompId='" + actRow.tranCompId.ToString() + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0)
                {
                    throw new Exception("Data not found.");
                    //throw new Exception("查無資料");
                }

                if (tbl_QueryData1.Rows[0]["carId"].ToString() != "")
                {
                    throw new Exception("This carId has been binding");
                    //throw new Exception("該張車次已綁定");
                    //if (tbl_QueryData1.Rows[0]["carId2"].ToString() != "")
                    //{
                    //    throw new Exception("該張車次已綁定");
                    //}
                }

                JArray ja = new JArray();
                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    var itemObject = new JObject();
                    foreach (DataColumn dc in tbl_QueryData1.Columns)
                    {
                        switch (dc.ColumnName)
                        {
                            case "dcShip":
                                strSql.Clear();
                                strSql.AppendLine(" select * from T30_carinv where dcShip='" + dr[dc].ToString() + "' order by arriveSeqD ");
                                DataTable tbl_QueryData2 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData2");

                                JArray ja2 = new JArray();
                                foreach (DataRow dr2 in tbl_QueryData2.Rows)
                                {
                                    var itemObject2 = new JObject();
                                    foreach (DataColumn dc2 in tbl_QueryData2.Columns)
                                    {
                                        itemObject2.Add(dc2.ColumnName, dr2[dc2].ToString());
                                    }
                                    ja2.Add(itemObject2);
                                }
                                itemObject.Add("details", ja2);
                                itemObject.Add(dc.ColumnName, dr[dc].ToString());
                                break;
                            default:
                                itemObject.Add(dc.ColumnName, dr[dc].ToString());
                                break;
                        }
                    }
                    ja.Add(itemObject);
                }

                jo.Add("rows", ja);
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

        #region T15_GetDriverId 20180828 取得目前單號配送司機ID
        [HttpPost]
        public HttpResponseMessage T15_GetDriverId()
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                #region 取得 User FormsAuthenticationTicket
                var ticket = HttpContext.Current.Request.Params["ticket"];
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                string dispatch_id = HttpContext.Current.Request.Params["dispatch_id"];

                strSql.Clear();
                strSql.AppendLine(" select sysUserId_run,taskStatus,driverCnt,dcShip from T30_carshdu where dispatch_id='" + dispatch_id + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");
                if (tbl_QueryData1.Rows[0][0].ToString() != userData.sysUserId.ToString()) jo.Add("isDriver", "N");
                else jo.Add("isDriver", "Y");

                #region 判斷可以重新排序的明細筆數
                strSql.Clear();
                strSql.Append(" select count(*) from T30_carinv where dcShip='" + tbl_QueryData1.Rows[0]["dcShip"].ToString() + "' and taskStatus in ('22', '25') ");
                int runCount = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(strSql.ToString()).ToString());

                jo.Add("runCount", runCount);
                #endregion

                jo.Add("taskStatus", tbl_QueryData1.Rows[0][1].ToString());
                jo.Add("driverCnt", tbl_QueryData1.Rows[0][2].ToString());
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


        #region T15_GetDispatchData 20180730 取得配送單資料
        [HttpPost]
        public HttpResponseMessage T15_GetDispatchData(Models.api_Delivery.MD_T30_carshdu actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                #region ACall_checkIsDBNull 
                actRow.dispatch_id = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dispatch_id);
                #endregion

                DateTime dtUtc = DateTime.UtcNow;

                strSql.Clear();
                strSql.AppendLine(" SELECT t1.*, t2.tranCompName, t2.addr, t2.zip, t3.countryName, t4.dcName, t1.cfs_timezone, t1.driverCnt FROM T30_carshdu t1 ");
                strSql.AppendLine(" left join T10_tranComp t2 on t2.tranCompId=t1.tranCompId ");
                strSql.AppendLine(" left join C00_country t3 on t3.countryId=t2.countryId ");
                strSql.AppendLine(" left join C10_dc t4 on t4.dcId=t1.dcId ");
                strSql.AppendLine(" where t1.dispatch_id='" + actRow.dispatch_id.ToString() + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                #region 判斷可以重新排序的明細筆數
                strSql.Clear();
                strSql.Append(" select count(*) from T30_carinv where dcShip='" + tbl_QueryData1.Rows[0]["dcShip"].ToString() + "' and taskStatus in ('22', '25') ");
                int runCount = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(strSql.ToString()).ToString());

                jo.Add("runCount", runCount);
                #endregion

                JArray ja = new JArray();
                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    var itemObject = new JObject();
                    foreach (DataColumn dc in tbl_QueryData1.Columns)
                    {
                        switch (dc.ColumnName)
                        {
                            case "dcShip":
                                strSql.Clear();
                                strSql.AppendLine(" select * from T30_carinv where dcShip='" + dr[dc].ToString() + "' order by arriveSeqD ");
                                DataTable tbl_QueryData2 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData2");

                                JArray ja2 = new JArray();
                                foreach (DataRow dr2 in tbl_QueryData2.Rows)
                                {
                                    var itemObject2 = new JObject();
                                    foreach (DataColumn dc2 in tbl_QueryData2.Columns)
                                    {
                                        switch (dc2.ColumnName)
                                        {
                                            case "realArriveTime2":
                                                if (dr2[dc2].ToString() != "")
                                                {
                                                    itemObject2.Add(dc2.ColumnName + "_1", DateTime.Parse(dr2[dc2].ToString()));
                                                    DateTime dt = Models.MEC_MethodMD.TimeZone.ChangeTimeZone(DateTime.Parse(dr2[dc2].ToString()), dr2["shipto_timezone"].ToString());
                                                    itemObject2.Add(dc2.ColumnName, dt.ToString("MM/dd/yyyy hh:mm"));
                                                }
                                                else
                                                {
                                                    itemObject2.Add(dc2.ColumnName, "");
                                                }
                                                //itemObject2.Add(dc2.ColumnName, (dr2[dc2].ToString() == "" ? "" : DateTime.Parse(dr2[dc2].ToString()).ToString("yyyy-MM-dd hh:mm")));
                                                break;
                                            default:
                                                itemObject2.Add(dc2.ColumnName, dr2[dc2].ToString());
                                                break;
                                        }
                                    }
                                    ja2.Add(itemObject2);
                                }
                                itemObject.Add("details", ja2);
                                itemObject.Add(dc.ColumnName, dr[dc].ToString());
                                break;
                            case "bindTime":
                            case "realDelDate":
                                if (dr[dc].ToString() != "")
                                {
                                    itemObject.Add(dc.ColumnName + "_1", DateTime.Parse(dr[dc].ToString()));
                                    DateTime dt = Models.MEC_MethodMD.TimeZone.ChangeTimeZone(DateTime.Parse(dr[dc].ToString()), dr["cfs_timezone"].ToString());
                                    itemObject.Add(dc.ColumnName, dt.ToString("MM/dd/yyyy hh:mm"));
                                }
                                else
                                {
                                    itemObject.Add(dc.ColumnName, "");
                                }
                                break;
                            default:
                                itemObject.Add(dc.ColumnName, dr[dc].ToString());
                                break;
                        }
                    }
                    ja.Add(itemObject);
                }

                jo.Add("rows", ja);
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

        #region T15_ChkBolInAwsS3
        [HttpPost]
        public HttpResponseMessage T15_ChkBolInAwsS3(Models.api_Delivery.MD_T30_carinv actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                #region ACall_checkIsDBNull 
                actRow.bol_no = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.bol_no);
                #endregion

                string dispatch_id = ZhClass.SqlTool.GetOneDataValue("select dispatch_id from T30_carshdu where dcShip=(select dcShip from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "')").ToString();

                string _bucketName = System.Web.Configuration.WebConfigurationManager.AppSettings["S3_BucketName"]; ;
                string bolPdfPath = "dispatchorder/" + dispatch_id + "/" + actRow.bol_no.ToString();

                bool isExist = Models.MEC_MethodMD.Aws.ChkObjectIsExistInS3(_bucketName, bolPdfPath + ".pdf");
                if (!isExist) throw new Exception("Object does not exist");
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

        #region T15_ChkBolInAwsS3
        [HttpPost]
        public HttpResponseMessage T15_ChkSecDriver()
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                #region ACall_checkIsDBNull 
                var dispatch_id = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["dispatch_id"]);
                #endregion

                DataTable tbl_QuerryData1 = SqlTool.GetDataTable(" select * from T30_carshdu where dispatch_id='" + dispatch_id + "' ", "tbl_QuerryData1");
                if (tbl_QuerryData1.Rows.Count == 0) throw new Exception(" Data not found. ");
                if (string.IsNullOrEmpty(tbl_QuerryData1.Rows[0]["tranCompId2"].ToString()) || int.Parse(tbl_QuerryData1.Rows[0]["driverCnt"].ToString()) < 2)
                {
                    jo.Add("isExist", "N");
                }
                else jo.Add("isExist", "Y");
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

        #region T15_ActChangeDriver 20180904 更換司機
        [HttpPost]
        public HttpResponseMessage T15_ActChangeDriver(/*Models.api_Delivery.MD_T30_carshdu actRow*/)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                #region 取得 User FormsAuthenticationTicket
                var ticket = HttpContext.Current.Request.Params["ticket"];
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                string dispatch_id = HttpContext.Current.Request.Params["dispatch_id"];
                string changeCode = HttpContext.Current.Request.Params["changeCode"];
                var driverId2 = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["driverId2"]);
                if (driverId2.ToString() == userData.userId) throw new Exception(" you can't change yourself ");
                DateTime dtUtc = DateTime.UtcNow;


                strSql.Clear();
                strSql.AppendLine(" select t1.*, us1.sysUserId userSeq1, us2.sysUserId userSeq2 from T30_carshdu t1 ");
                strSql.AppendLine(" left join S10_users us1 on us1.userId=t1.driverId and us1.appSysId='10' ");
                strSql.AppendLine(" left join S10_users us2 on us2.userId=t1.driverId2 and us2.appSysId='10' ");
                strSql.AppendLine(" where dispatch_Id='" + dispatch_id + "' ");

                DataTable T30_carshdu = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "T30_carshdu");
                //DataTable T30_carshdu = SqlTool.GetDataTable(" select * from  T30_carshdu where dispatch_Id='" + dispatch_id + "' ", "tbl_QueryData1");
                if (T30_carshdu.Rows.Count == 0) throw new Exception(" dispatch not found ");

                if (changeCode == "10") driverId2 = T30_carshdu.Rows[0]["driverId"].ToString();
                DataTable tbl_QueryData2 = SqlTool.GetDataTable(" select * from  T10_driver t1 inner join S10_users t2 on t2.sysUserId=t1.sysUserId where t1.driverId='" + driverId2 + "' and t1.tranCompId='" + T30_carshdu.Rows[0]["tranCompId"].ToString() + "' and t1.dcId='" + T30_carshdu.Rows[0]["dcId"].ToString() + "'", "tbl_QueryData2");

                if (tbl_QueryData2.Rows.Count == 0) throw new Exception(" Please check driver account and the driver have the same truck company.");

                //DataTable T30_carshdu = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, " select * from T30_carshdu where dispatch_id='" + dispatch_id + "' ", "T30_carshdu");
                if (string.IsNullOrEmpty(T30_carshdu.Rows[0]["tranCompId2"].ToString()) && string.IsNullOrEmpty(T30_carshdu.Rows[0]["carId2"].ToString()) && string.IsNullOrEmpty(T30_carshdu.Rows[0]["driverId2"].ToString())) //string.IsNullOrEmpty(T30_carshdu.Rows[0]["trailerId2"].ToString()) &&
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                        {

                            cn.Open();

                            #region 設置 要傳入的 SqlParameter 資料
                            SqlParameter[] param = {
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, dispatch_id),
                                    new SqlParameter("tranCompId2", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, T30_carshdu.Rows[0]["tranCompId"].ToString()),
                                    new SqlParameter("carId2", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, T30_carshdu.Rows[0]["carId"].ToString()),
                                    new SqlParameter("trailerId2", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, T30_carshdu.Rows[0]["trailerId"].ToString()),
                                    new SqlParameter("bindEndTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                                    new SqlParameter("sysUserId_run", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData2.Rows[0]["sysUserId"].ToString()),
                                    new SqlParameter("driverId2", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, driverId2),
                                    new SqlParameter("contactTel2", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData2.Rows[0]["contactTel"].ToString()),
                                    new SqlParameter("driverSeq", SqlDbType.SmallInt, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, 2),
                                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                            #endregion

                            strSql.Clear();
                            strSql.AppendLine(" Update T30_carshdu set driverId2=@driverId2, contactTel2=@contactTel2, tranCompId2=@tranCompId2, carId2=@carId2, trailerId2=@trailerId2, bindEndTime=@bindEndTime, bindTime2=@bindEndTime, sysUserId_run=@sysUserId_run, sysUserId2=@sysUserId_run, driverSeq=@driverSeq, actUser=@actUser, actTime=@actTime where dispatch_id=@dispatch_id ");
                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                            if (errStr != "") throw new Exception(errStr);


                            cn.Close();
                        }

                        using (System.Transactions.TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                        {
                            using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                            {
                                cn1.Open();
                                #region 設置 要傳入的 SqlParameter 資料
                                SqlParameter[] param = {
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, dispatch_id),
                                    new SqlParameter("sec_driver_name", SqlDbType.NVarChar, 255, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData2.Rows[0]["userName"].ToString()),
                                    new SqlParameter("epod_update_time_utc", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                                #endregion

                                strSql.Clear();
                                strSql.AppendLine(" Update DISPATCH set sec_driver_name=@sec_driver_name, epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id ");
                                errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn1, strSql.ToString(), param);
                                if (errStr != "") throw new Exception(errStr);

                                cn1.Close();
                            }
                            scopeNew.Complete();
                        }
                        scope.Complete();
                    }
                }
                else
                {
                    string edit_sysUserId_run = "";

                    if (T30_carshdu.Rows[0]["sysUserId_run"].ToString() == T30_carshdu.Rows[0]["userSeq1"].ToString())
                    {
                        edit_sysUserId_run = T30_carshdu.Rows[0]["userSeq2"].ToString();
                    }
                    else
                    {
                        edit_sysUserId_run = T30_carshdu.Rows[0]["userSeq1"].ToString();
                    }
                    #region 設置 要傳入的 SqlParameter 資料
                    SqlParameter[] param = {
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, dispatch_id),
                                    new SqlParameter("sysUserId_run", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, edit_sysUserId_run),
                                    new SqlParameter("driverSeq", SqlDbType.SmallInt, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, (T30_carshdu.Rows[0]["driverSeq"].ToString() == "1" ? "2" : "1")),
                                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                    #endregion
                    strSql.Clear();
                    strSql.AppendLine(" Update T30_carshdu set sysUserId_run=@sysUserId_run, driverSeq=@driverSeq, actUser=@actUser, actTime=@actTime where dispatch_id=@dispatch_id ");
                    errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                    if (errStr != "") throw new Exception(errStr);
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

        #region T16_GetBolData 20180730 取得配送單資料
        [HttpPost]
        public HttpResponseMessage T16_GetBolData(Models.api_Delivery.MD_T30_carinv actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                #region ACall_checkIsDBNull 
                actRow.bol_no = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.bol_no);
                #endregion

                DateTime dtUtc = DateTime.UtcNow;
                strSql.Clear();
                strSql.Append(" select * from T30_carinv where bol_no='" + actRow.bol_no + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                JArray ja = new JArray();
                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    var itemObject = new JObject();
                    foreach (DataColumn dc in tbl_QueryData1.Columns)
                    {
                        itemObject.Add(dc.ColumnName, dr[dc].ToString());
                    }
                    ja.Add(itemObject);
                }

                jo.Add("rows", ja);
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

        #region T17_GetBolData 20180730 取得配送單明細資料，用來重排順序用
        [HttpPost]
        public HttpResponseMessage T17_GetBolData(Models.api_Delivery.MD_T30_carshdu actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                #region ACall_checkIsDBNull 
                actRow.dispatch_id = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dispatch_id);
                #endregion

                DateTime dtUtc = DateTime.UtcNow;
                strSql.Clear();
                strSql.Append(" select * from T30_carinv where dcShip=(select dcShip from T30_carshdu where dispatch_id='" + actRow.dispatch_id + "') and taskStatus in ('22', '25') order by arriveSeqD ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                JArray ja = new JArray();
                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    var itemObject = new JObject();
                    foreach (DataColumn dc in tbl_QueryData1.Columns)
                    {
                        itemObject.Add(dc.ColumnName, dr[dc].ToString());
                    }
                    ja.Add(itemObject);
                }
                jo.Add("taskStatus", SqlTool.GetOneDataValue(" select taskStatus from T30_carshdu where dispatch_id='" + actRow.dispatch_id + "' ").ToString());
                jo.Add("rows", ja);
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


        #region T17_ActBolArriveSeq 20180730 重設配送路順
        [HttpPost]
        public HttpResponseMessage T17_ActBolArriveSeq(Models.api_Delivery.MD_T30_carinv_arriveSeq actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                #region ACall_checkIsDBNull 
                actRow.dispatch_id = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dispatch_id);
                #endregion

                strSql.Clear();
                strSql.Append(" select min(arriveSeqD) from T30_carinv where dcShip=(select dcShip from T30_carshdu where dispatch_id='" + actRow.dispatch_id.ToString() + "') and taskStatus in ('22', '25') ");
                string minSeq = ZhClass.SqlTool.GetOneDataValue(strSql.ToString()).ToString();

                if (minSeq == "")
                {
                    throw new Exception("Can't change route");
                    //throw new Exception("無法變動路順");
                }

                int currSeq = Convert.ToInt32(minSeq);
                strSql.Clear();
                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();
                        foreach (Models.api_Delivery.shipOrder actrow in actRow.newOrderArr)
                        {
                            if (currSeq.ToString() == minSeq)
                            {
                                strSql.AppendLine(" update T30_carinv set arriveSeqD='" + currSeq + "', taskStatus='25', actTime=getutcDate(), actUser='" + userData.sysUserId.ToString() + "' where bol_no='" + actrow.bol_no + "' ");
                            }
                            else
                            {
                                strSql.AppendLine(" update T30_carinv set arriveSeqD='" + currSeq + "', taskStatus='22', actTime=getutcDate(), actUser='" + userData.sysUserId.ToString() + "' where bol_no='" + actrow.bol_no + "' ");
                            }
                            currSeq++;
                        }

                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString());
                        if (errStr != "") throw new Exception(errStr);


                        strSql.Clear();
                        using (System.Transactions.TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                        {
                            using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                            {
                                cn1.Open();
                                currSeq = Convert.ToInt32(minSeq);
                                foreach (Models.api_Delivery.shipOrder actrow in actRow.newOrderArr)
                                {
                                    if (currSeq.ToString() == minSeq)
                                    {
                                        strSql.AppendLine(" update bol set status='25', new_ship_order='" + currSeq + "', epod_update_time_utc=getutcDate() where bol_no='" + actrow.bol_no + "' ");
                                    }
                                    else
                                    {
                                        strSql.AppendLine(" update bol set status='22', new_ship_order='" + currSeq + "', epod_update_time_utc=getutcDate() where bol_no='" + actrow.bol_no + "' ");
                                    }
                                    currSeq++;
                                }

                                errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn1, strSql.ToString());
                                if (errStr != "") throw new Exception(errStr);

                                cn1.Close();
                            }
                            scopeNew.Complete();
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


        #region T20_GetDispatchStatus 20180904 取得目前任務是否為最後一筆與是否可換司機
        [HttpPost]
        public HttpResponseMessage T20_GetDispatchStatus()
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                #region 取得 User FormsAuthenticationTicket
                var ticket = HttpContext.Current.Request.Params["ticket"];
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                string bol_no = HttpContext.Current.Request.Params["bol_no"];
                string dispatch_id = SqlTool.GetOneDataValue(" SELECT dispatch_id from T30_carshdu where dcShip = (SELECT dcShip from T30_carinv where bol_no='" + bol_no + "') ").ToString();
                jo.Add("dispatch_id", dispatch_id);

                strSql.Clear();
                strSql.AppendLine(" select * from T30_carshdu where dispatch_id='" + dispatch_id + "' ");
                DataTable T30_carshdu = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "T30_carshdu");
                if (int.Parse(T30_carshdu.Rows[0]["driverCnt"].ToString()) > 1)
                {
                    if (string.IsNullOrEmpty(T30_carshdu.Rows[0]["tranCompId2"].ToString()) && string.IsNullOrEmpty(T30_carshdu.Rows[0]["carId2"].ToString()) && string.IsNullOrEmpty(T30_carshdu.Rows[0]["driverId2"].ToString())) //string.IsNullOrEmpty(T30_carshdu.Rows[0]["trailerId2"].ToString()) &&
                    {
                        jo.Add("changeDriver", "Y");
                    }
                    else
                    {
                        jo.Add("changeDriver", "M");
                    }

                }
                else
                {
                    jo.Add("changeDriver", "N");
                }
                //strSql.Clear();
                //strSql.AppendLine(" select * from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "' ");
                //DataTable T30_carinv = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "T30_carinv");
                string bol_no_arriveSeqD = SqlTool.GetOneDataValue(" select arriveSeqD from T30_carinv where bol_no='" + bol_no + "' ").ToString();
                if (bol_no_arriveSeqD == T30_carshdu.Rows[0]["detailCount"].ToString()) jo.Add("isLast", "Y");
                else jo.Add("isLast", "N");
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

        #region T20_ActCompleteBol 20180730 完成bol 明細資料
        [HttpPost]
        public HttpResponseMessage T20_ActCompleteBol(/*Models.api_Delivery.MD_T30_carinv actRow*/)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            #region 取得使用者資訊
            //var ticket = HttpContext.Current.Request.Params["ticket"];
            //if (string.IsNullOrEmpty(ticket))
            //{
            //    ticket = HttpContext.Current.Request.Params["webTicket"];
            //}
            //userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
            #endregion
            clientIp = GetClientIp();
            JObject jo = new JObject();
            DateTime dtUtc = DateTime.UtcNow;
            try
            {
                Models.api_Delivery.MD_T30_carinv actRow = new Models.api_Delivery.MD_T30_carinv();

                #region ACall_checkIsDBNull 
                actRow.appSysId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["appSysId"]);
                actRow.longitudeDev = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["longitudeDev"]);
                actRow.latitudeDev = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["latitudeDev"]);
                actRow.bol_no = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["bol_no"]);
                actRow.POD_name = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["POD_name"]);
                actRow.statusType3 = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["statusType3"]);
                actRow.statusId3 = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["statusId3"]);
                actRow.memo = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["memo"]);
                actRow.taskStatus = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["taskStatus"]);
                actRow.type = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["type"]);
                actRow.base64img = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["base64img"]);
                actRow.fileType = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["fileType"]);
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(HttpContext.Current.Request.Params["ticket"]);
                #endregion

                if (string.IsNullOrEmpty(actRow.longitudeDev.ToString())) actRow.longitudeDev = " ";
                if (string.IsNullOrEmpty(actRow.latitudeDev.ToString())) actRow.latitudeDev = " ";

                if (actRow.longitudeDev.ToString().Length >= 11) actRow.longitudeDev = actRow.longitudeDev.ToString().Substring(0, 11);
                if (actRow.latitudeDev.ToString().Length >= 11) actRow.latitudeDev = actRow.latitudeDev.ToString().Substring(0, 11);

                string dispatch_id = ZhClass.SqlTool.GetOneDataValue("select dispatch_id from T30_carshdu where dcShip=(select dcShip from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "')").ToString();
                string shipto_timezone = ZhClass.SqlTool.GetOneDataValue("select shipto_timezone from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "'").ToString();
                string _bucketName = System.Web.Configuration.WebConfigurationManager.AppSettings["S3_BucketName"];
                string bolPdfPath = "dispatchorder/" + dispatch_id + "/" + actRow.bol_no.ToString();

                //strSql.Clear();
                //strSql.AppendLine(" select * from T30_carshdu where dispatch_id='" + dispatch_id + "' ");
                //DataTable T30_carshdu = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "T30_carshdu");
                //if (int.Parse(T30_carshdu.Rows[0]["driverCnt"].ToString()) > 1)
                //{
                //    if (string.IsNullOrEmpty(T30_carshdu.Rows[0]["tranCompId2"].ToString()) && string.IsNullOrEmpty(T30_carshdu.Rows[0]["carId2"].ToString()) && string.IsNullOrEmpty(T30_carshdu.Rows[0]["driverId2"].ToString())) //string.IsNullOrEmpty(T30_carshdu.Rows[0]["trailerId2"].ToString()) &&
                //    {
                //        jo.Add("changeDriver", "Y");
                //    }
                //    else
                //    {
                //        jo.Add("changeDriver", "M");
                //    }

                //}
                //else
                //{
                //    jo.Add("changeDriver", "N");
                //}


                ////strSql.Clear();
                ////strSql.AppendLine(" select * from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "' ");
                ////DataTable T30_carinv = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "T30_carinv");
                //string bol_no_arriveSeqD = SqlTool.GetOneDataValue(" select arriveSeqD from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "' ").ToString();
                //if (bol_no_arriveSeqD == T30_carshdu.Rows[0]["detailCount"].ToString()) jo.Add("isLast", "Y");
                //else jo.Add("isLast", "N");

                #region 處理圖片
                string fileName = actRow.bol_no.ToString() + "_" + dtUtc.ToString("ddMMHHmmss");
                MemoryStream ms = new MemoryStream();
                string saveKey = "";

                //type:10 (表示是拍照或是選照片)
                //type:20 (簽名)
                //type:30 (WEB操作)
                if (actRow.type.ToString() == "10")
                {
                    #region 取得檔案並進行壓縮
                    HttpPostedFile file = HttpContext.Current.Request.Files["file"];
                    file.InputStream.CopyTo(ms);

                    Models.MEC_MethodMD.ImageCompression ic = new Models.MEC_MethodMD.ImageCompression();
                    ms = ic.MemoryStreamToMemoryStream(ms, actRow.fileType.ToString());
                    #endregion


                    #region 上傳圖片至SERVER 測試用20180822 By.Ray
                    //var savePath = System.Web.Hosting.HostingEnvironment.MapPath("~/PDF/"); //儲存檔案位置 版本2
                    //if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
                    //string filePath = savePath + fileName + ".jpg";
                    //file.SaveAs(filePath);
                    //resp.StatusCode = HttpStatusCode.OK;
                    //resp.Content = new StringContent(JsonConvert.SerializeObject(jo), System.Text.Encoding.UTF8, "application/json");
                    //return resp;
                    #endregion

                    #region 上傳圖片至s3
                    IAmazonS3 client;
                    saveKey = "pod/" + dispatch_id + "/" + fileName + actRow.fileType.ToString();
                    using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(Amazon.RegionEndpoint.USWest2))
                    {
                        var request = new PutObjectRequest()
                        {
                            BucketName = _bucketName,
                            Key = saveKey,
                            InputStream = ms
                        };
                        PutObjectResponse response = client.PutObject(request);
                    }
                    #endregion
                }
                else if (actRow.type.ToString() == "20")
                {
                    actRow.fileType = ".pdf";

                    #region 確認bol是否存在於 s3
                    bool isExist = Models.MEC_MethodMD.Aws.ChkObjectIsExistInS3(_bucketName, bolPdfPath + actRow.fileType);
                    if (!isExist) throw new Exception("Object does not exist");
                    #endregion

                    byte[] data = System.Convert.FromBase64String(actRow.base64img.ToString().Replace("data:image/png;base64,", ""));

                    #region 將pdf暫時儲至server端
                    //如果server端沒有PDF資料夾則建立
                    var savePath = System.Web.Hosting.HostingEnvironment.MapPath("\\PDF\\");
                    if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

                    string localPath = System.Web.HttpContext.Current.Server.MapPath("\\PDF\\" + fileName + ".pdf");
                    var fileStream = File.Create(localPath);
                    Models.MEC_MethodMD.Aws aws = new Models.MEC_MethodMD.Aws();
                    aws.GetObjectInS3(_bucketName, bolPdfPath + actRow.fileType).ResponseStream.CopyTo(fileStream);
                    fileStream.Close();
                    #endregion

                    jo.Add("step3", "mergin pdf and image");

                    #region 取得bol pdf，並進行合併
                    using (Stream inputPdfStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (Stream inputImageStream = new MemoryStream(data))
                    using (Stream outputPdfStream = new FileStream(localPath.Replace(fileName, "new_" + fileName), FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(inputImageStream);

                        var reader = new PdfReader(inputPdfStream);
                        var stamper = new PdfStamper(reader, outputPdfStream);
                        var pdfContentByte = stamper.GetOverContent(1);

                        image.ScaleAbsoluteWidth(20);
                        image.ScaleAbsoluteHeight(image.Height / (image.Width / 20));
                        image.SetAbsolutePosition(345, 35);
                        pdfContentByte.AddImage(image);


                        BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        var pod = pdfContentByte.CreateTemplate(120, 60);
                        pod.BeginText();

                        pod.SetFontAndSize(bf, 10);
                        pod.SetTextMatrix(0, 5);
                        pod.ShowText(actRow.POD_name.ToString());
                        pod.EndText();

                        pdfContentByte.SetColorFill(BaseColor.BLACK);
                        pdfContentByte.AddTemplate(pod, 200, 30);

                        var date = pdfContentByte.CreateTemplate(120, 60);
                        date.BeginText();

                        date.SetFontAndSize(bf, 10);
                        date.SetTextMatrix(0, 0);
                        DateTime dt = Models.MEC_MethodMD.TimeZone.ChangeTimeZone(dtUtc, shipto_timezone);
                        date.ShowText(dt.ToString("MM/dd/yyyy HH:mm"));
                        date.EndText();

                        pdfContentByte.SetColorFill(BaseColor.BLACK);
                        pdfContentByte.AddTemplate(date, 493, 35);

                        inputPdfStream.Close();

                        stamper.Close();

                        #region 上傳pdf至s3
                        IAmazonS3 client;
                        saveKey = "pod/" + dispatch_id + "/" + fileName + ".pdf";
                        using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(Amazon.RegionEndpoint.USWest2))
                        {
                            var request = new PutObjectRequest()
                            {
                                BucketName = _bucketName,
                                Key = saveKey,
                                FilePath = localPath.Replace(fileName, "new_" + fileName)//SEND THE FILE STREAM
                            };

                            PutObjectResponse response = client.PutObject(request);
                        }
                        #endregion

                        #region 刪除暫存於server的資料
                        if (System.IO.File.Exists(localPath))
                        {
                            File.Delete(localPath);
                        }

                        if (System.IO.File.Exists(localPath.Replace(fileName, "new_" + fileName)))
                        {
                            File.Delete(localPath.Replace(fileName, "new_" + fileName));
                        }

                        #endregion
                    }
                    #endregion
                }
                else
                {

                }
                #endregion
                jo.Add("step4", "start modify DB");



                #region 記錄檔案上傳至 T30_fileManage (非WEB)
                string sysFileId = null;
                if (actRow.type.ToString() != "30")
                {
                    strSql.Clear();

                    string statusx = "";
                    if (actRow.type.ToString() == "10")
                        statusx = "10";
                    else
                        statusx = "40";

                    #region 設置 要傳入的 SqlParameter 資料
                    SqlParameter[] param_file = {
                    new SqlParameter("sysFileId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, "0"),
                    new SqlParameter("fileType", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.fileType.ToString()),
                    new SqlParameter("statusType", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "E1"),
                    new SqlParameter("statusx", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, statusx),
                    new SqlParameter("fileName", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, fileName + actRow.fileType),
                    new SqlParameter("fileNameSys", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, fileName + actRow.fileType),
                    new SqlParameter("route", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, saveKey),
                    //new SqlParameter("memo", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ""),
                    new SqlParameter("oriObjType", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "02"),
                    new SqlParameter("statusType2", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "No"),
                    new SqlParameter("statusx2", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                    new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed,userData.sysUserId.ToString()),
                    new SqlParameter("creatTime", SqlDbType.DateTime, 8, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                };
                    #endregion

                    strSql.AppendLine(" Insert into T30_fileManage (fileType, statusType, statusx, fileName, fileNameSys, route, oriObjType, statusType2, statusx2, creatUser, creatTime) ");
                    strSql.AppendLine(" values (@fileType, @statusType, @statusx, @fileName, @fileNameSys,@route, @oriObjType,@statusType2,@statusx2,@creatUser, @creatTime) ");
                    strSql.AppendLine(" SELECT @sysFileId = SCOPE_IDENTITY() ");

                    errStr = ZhClass.SqlTool.ExecuteNonQuery(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), param_file);
                    if (errStr != "") throw new Exception(errStr);

                    sysFileId = param_file[0].Value.ToString();
                }
                #endregion

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                if (string.IsNullOrEmpty(userData.UUID)) userData.UUID = "";
                //userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion

                strSql.Clear();
                strSql.AppendLine(" select t2.dcId,t1.bol_no, t1.arriveSeqD, t2.detailCount, t1.dcShip, t2.dispatch_id, t1.taskStatus, t3.arriveSeqD arriveSeqD_ori from T30_carinv t1 ");
                strSql.AppendLine(" left join T30_carshdu t2 on t2.dcShip=(select dcShip from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "') ");
                strSql.AppendLine(" left join T30_carinv t3 on t3.bol_no='" + actRow.bol_no.ToString() + "' ");
                strSql.AppendLine(" where t1.dcShip=(select dcShip from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "')  ");
                strSql.AppendLine(" and t1.arriveSeqD=(select arriveSeqD + 1 from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "') ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0)
                {
                    strSql.Clear();
                    strSql.AppendLine(" select t2.dcId,t1.bol_no, t1.arriveSeqD, t2.detailCount, t1.dcShip, t2.dispatch_id, t1.taskStatus, t3.arriveSeqD arriveSeqD_ori from T30_carinv t1 ");
                    strSql.AppendLine(" left join T30_carshdu t2 on t2.dcShip=(select dcShip from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "') ");
                    strSql.AppendLine(" left join T30_carinv t3 on t3.bol_no='" + actRow.bol_no.ToString() + "' ");
                    strSql.AppendLine(" where t1.dcShip=(select dcShip from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "')  ");
                    strSql.AppendLine(" and t1.arriveSeqD=(select arriveSeqD from T30_carinv where bol_no='" + actRow.bol_no.ToString() + "') ");
                    tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");
                }


                strSql.Clear();
                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        if (actRow.statusId3.ToString() == "null") actRow.statusId3 = "";

                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param = {
                                    new SqlParameter("bol_no", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.bol_no.ToString()),
                                    new SqlParameter("POD_Name", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.POD_name.ToString()),
                                    new SqlParameter("statusType2", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  "PO"),
                                    new SqlParameter("statusId2", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  actRow.type.ToString()),
                                    new SqlParameter("statusType3", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  actRow.statusType3.ToString()),
                                    new SqlParameter("statusId3", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  actRow.statusId3.ToString()),
                                    //new SqlParameter("FileId_02", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  sysFileId),
                                    new SqlParameter("memo", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  actRow.memo.ToString()),
                                    new SqlParameter("taskStatus", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.taskStatus.ToString()),
                                    new SqlParameter("realArriveTime2", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString())
                                };
                        #endregion

                        strSql.Clear();
                        if (actRow.type.ToString() != "30") strSql.AppendLine(" Update T30_carinv set POD_Name=@POD_Name, FileId_02='" + sysFileId + "', statusType2=@statusType2, statusId2=@statusId2, statusType3=@statusType3, statusId3=@statusId3, memo=@memo, taskStatus=@taskStatus, realArriveTime2=@realArriveTime2, actUser=@actUser, actTime=@actTime where bol_no=@bol_no ");
                        else strSql.AppendLine(" Update T30_carinv set POD_Name=@POD_Name, statusType2=@statusType2, statusId2=@statusId2, statusType3=@statusType3, statusId3=@statusId3, memo=@memo, taskStatus=@taskStatus, realArriveTime2=@realArriveTime2, actUser=@actUser, actTime=@actTime where bol_no=@bol_no ");

                        if (tbl_QueryData1.Rows[0]["detailCount"].ToString() == tbl_QueryData1.Rows[0]["arriveSeqD"].ToString() && tbl_QueryData1.Rows[0]["taskStatus"].ToString() == "25")
                        {
                            strSql.AppendLine("  Update T30_carshdu set realFinishTime=@actTime, taskStatus=@taskStatus where dcShip='" + tbl_QueryData1.Rows[0]["dcShip"].ToString() + "' ");
                        }
                        else
                        {
                            strSql.AppendLine(" Update T30_carinv set taskStatus='25' where bol_no='" + tbl_QueryData1.Rows[0]["bol_no"].ToString() + "' ");
                        }

                        strSql.AppendLine(" Insert into App_deviceActivity (appSysId, sysUserId, UUID, longitudeDev, latitudeDev, dcShip, bol_no, dcId, arriveSeqD, memo, creatTime) ");
                        strSql.AppendLine(" values ('" + actRow.appSysId.ToString() + "', '" + userData.sysUserId.ToString() + "', '" + userData.UUID.ToString() + "', '" + actRow.longitudeDev.ToString() +
                            "', '" + actRow.latitudeDev.ToString() + "', '" + tbl_QueryData1.Rows[0]["dcShip"].ToString() + "', '" + actRow.bol_no.ToString() +
                            "', '" + tbl_QueryData1.Rows[0]["dcId"].ToString() + "', '" + tbl_QueryData1.Rows[0]["arriveSeqD_ori"].ToString() + "', 'bol_no=" + actRow.bol_no.ToString() + "', getutcDate()) ");

                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);
                    }
                    int osd_code = string.IsNullOrEmpty(actRow.memo.ToString()) ? 0 : 1;
                    using (System.Transactions.TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                    {
                        using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                        {
                            cn1.Open();
                            if (actRow.statusId3.ToString() == "") actRow.statusId3 = "0";
                            #region 設置 要傳入的 SqlParameter 資料
                            SqlParameter[] param = {
                                    new SqlParameter("bol_no", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.bol_no.ToString()),
                                    new SqlParameter("podname", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.POD_name.ToString()),
                                    new SqlParameter("pod_file_name", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, fileName + actRow.fileType.ToString()),
                                    new SqlParameter("pod_d_utc", SqlDbType.VarChar, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc.ToString("yyyyMMdd")),
                                    new SqlParameter("pod_t_utc", SqlDbType.VarChar, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  dtUtc.ToString("HHmm")),
                                    new SqlParameter("status", SqlDbType.Char, 10, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.taskStatus.ToString()),
                                    new SqlParameter("osd_code", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, osd_code),
                                    new SqlParameter("pod_note", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.memo.ToString()),
                                    new SqlParameter("epod_update_time_utc", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                            #endregion

                            strSql.Clear();
                            strSql.AppendLine(" Update bol set podname=@podname, pod_file_name=@pod_file_name, pod_d_utc=@pod_d_utc, pod_t_utc=@pod_t_utc, status=@status, osd_code=@osd_code, pod_note=@pod_note, epod_update_time_utc=@epod_update_time_utc where bol_no=@bol_no ");
                            //if (actRow.statusId3.ToString() != "0")
                            //{
                            //    strSql.AppendLine(" Update bol set podname=@podname, pod_file_name=@pod_file_name, pod_d_utc=@pod_d_utc, pod_t_utc=@pod_t_utc, status=@status, Osd_code=@Osd_code, Pod_note=@Pod_note, epod_update_time_utc=@epod_update_time_utc where bol_no=@bol_no ");
                            //}
                            //else
                            //{
                            //    strSql.AppendLine(" Update bol set podname=@podname, pod_file_name=@pod_file_name, pod_d_utc=@pod_d_utc, pod_t_utc=@pod_t_utc, status=@status, Pod_note=@Pod_note, epod_update_time_utc=@epod_update_time_utc where bol_no=@bol_no ");
                            //}


                            if (tbl_QueryData1.Rows[0]["detailCount"].ToString() == tbl_QueryData1.Rows[0]["arriveSeqD"].ToString() && tbl_QueryData1.Rows[0]["taskStatus"].ToString() == "25")
                            {
                                strSql.AppendLine(" Update dispatch set status='30', epod_update_time_utc=@epod_update_time_utc where dispatch_id='" + tbl_QueryData1.Rows[0]["dispatch_id"].ToString() + "' ");
                            }
                            else
                            {
                                strSql.AppendLine(" Update bol set status='25', ship_order='" + tbl_QueryData1.Rows[0]["arriveSeqD"].ToString() + "', new_ship_order='" + tbl_QueryData1.Rows[0]["arriveSeqD"].ToString() + "' where bol_no='" + tbl_QueryData1.Rows[0]["bol_no"].ToString() + "' ");
                            }
                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn1, strSql.ToString(), param);
                            if (errStr != "") throw new Exception(errStr);
                            cn1.Close();

                        }
                        scopeNew.Complete();
                    }
                    scope.Complete();
                }

                jo.Add("dispatch_id", tbl_QueryData1.Rows[0]["dispatch_id"].ToString());
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


        #region M10_GetDeliveryTask 20180730 取得配送單
        [HttpPost]
        public HttpResponseMessage M10_GetDeliveryTask(Models.api_Delivery.MD_T30_carshdu actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                DateTime dtUtc = DateTime.UtcNow;

                strSql.Clear();
                strSql.AppendLine(" select t1.dcShip, t1.dispatch_id, t1.taskStatus, convert(varchar, t1.actTime, 120) actTime, t2.statusName from T30_carshdu t1 ");
                strSql.AppendLine(" left join S00_statusId t2 on t2.statusId=t1.taskStatus and t2.statusType='TS' ");
                strSql.AppendLine(" where t1.taskStatus in ('20', '22', '25') and t1.sysUserId_run='" + userData.sysUserId + "'  "); //(t1.driverId='" + userData.userId + "' or t1.driverId2='" + userData.userId + "')
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");


                JArray ja = new JArray();
                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    var itemObject = new JObject();
                    foreach (DataColumn dc in tbl_QueryData1.Columns)
                    {
                        itemObject.Add(dc.ColumnName, dr[dc].ToString());
                    }
                    ja.Add(itemObject);
                }

                jo.Add("rows", ja);
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

        #region T19_GetBolPdfFile 20180827 取得bol PDF檔 By.Ray
        [HttpPost]
        public HttpResponseMessage T19_GetBolPdfFile(/*string bol_no*/)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            #region 取得 User FormsAuthenticationTicket
            var ticket = HttpContext.Current.Request.Params["ticket"];
            if (string.IsNullOrEmpty(ticket))
            {
                ticket = HttpContext.Current.Request.Params["webTicket"];
            }
            userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
            #endregion

            clientIp = GetClientIp();
            JObject jo = new JObject();
            JArray ja = new JArray();

            string bol_no = HttpContext.Current.Request.Params["bol_no"];
            string viewStatus = HttpContext.Current.Request.Params["viewStatus"];
            try
            {
                string _bucketName = System.Web.Configuration.WebConfigurationManager.AppSettings["S3_BucketName"];
                string dispatch_id = ZhClass.SqlTool.GetOneDataValue("select dispatch_id from T30_carshdu where dcShip=(select dcShip from T30_carinv where bol_no='" + bol_no.ToString() + "')").ToString();

                #region PDF to Image Byte || Image to Image Byte       20180828 By.Ray

                //IAmazonS3 client;
                Models.MEC_MethodMD.Aws aws = new Models.MEC_MethodMD.Aws();
                //string url = @"http://www.ycps.tp.edu.tw/mediafile/1934/news/154/2013-12/2013-12-19-9-55-10-nf1.pdf";
                string url = "";
                #region 由網址抓檔案轉換成MemoryStream
                if (viewStatus == "10")
                {
                    string bolPdfPath = "dispatchorder/" + dispatch_id + "/" + bol_no.ToString() + ".pdf";
                    #region 確認server有沒有檔案
                    //bool isExist = Models.MEC_MethodMD.Aws.ChkObjectIsExistInS3(_bucketName, bolPdfPath + ".pdf");
                    //if (!isExist) throw new Exception("Object does not exist");
                    #endregion 
                    //跟S3要網址
                    url = aws.GetObjectUrlInS3(_bucketName, bolPdfPath);
                    jo.Add("pdfFileUrl", url);
                }
                else
                {
                    strSql.Clear();
                    strSql.AppendLine(" select t2.fileType,t2.route,t2.statusx from T30_carinv t1 ");
                    strSql.AppendLine(" inner join T30_fileManage t2 on t2.sysFileId=t1.FileId_02 ");
                    strSql.AppendLine(" where t1.bol_no='" + bol_no + "' and t1.taskStatus='30' ");
                    DataTable tbl_queryData1 = SqlTool.GetDataTable(strSql.ToString(), "tbl_queryData1");

                    string key = tbl_queryData1.Rows.Count > 0 ? tbl_queryData1.Rows[0]["route"].ToString() : "";
                    if (key == "") throw new Exception(" file not found ");
                    #region 如果是圖檔直接轉換並傳回
                    if (tbl_queryData1.Rows[0]["statusx"].ToString() == "10")
                    {
                        url = aws.GetObjectUrlInS3(_bucketName, key);
                        byte[] imageBytes2base64 = new System.Net.WebClient().DownloadData(url);
                        string base64string = Convert.ToBase64String(imageBytes2base64);
                        JObject itemObject = new JObject();
                        itemObject.Add("img", "data:image/png;base64," + base64string);
                        ja.Add(itemObject);
                        jo.Add("pdfFile", ja);
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

                    url = aws.GetObjectUrlInS3(_bucketName, key);
                }
                byte[] imageBytes = new System.Net.WebClient().DownloadData(url);
                MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                ms.Write(imageBytes, 0, imageBytes.Length);
                #endregion


                const int desired_x_dpi = 96;
                const int desired_y_dpi = 96;
                var _lastInstalledVersion =
                GhostscriptVersionInfo.GetLastInstalledVersion(
                GhostscriptLicense.GPL | GhostscriptLicense.AFPL,
                GhostscriptLicense.GPL);
                using (var _rasterizer = new GhostscriptRasterizer())
                {
                    System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.SaveFlag;
                    ImageCodecInfo encoderInfo = ImageCodecInfo.GetImageEncoders().First(i => i.MimeType == "image/png");
                    //ImageCodecInfo encoderInfo = ImageCodecInfo.GetImageEncoders().First(i => i.MimeType == "image/tiff");
                    EncoderParameters encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(encoder, (long)EncoderValue.MultiFrame);

                    _rasterizer.Open(ms, _lastInstalledVersion, false);
                    System.Drawing.Image allImg = null;
                    int pageCount = _rasterizer.PageCount;
                    for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
                    {
                        JObject itemObject = new JObject();

                        MemoryStream imageMs = new MemoryStream();
                        //string tiffFilePath = System.Web.HttpContext.Current.Server.MapPath("/PDF/page_" + pageNumber + ".png");

                        if (pageCount == 1)
                        {
                            //只有一頁，所以直接存檔即可
                            allImg = _rasterizer.GetPage(desired_x_dpi, desired_y_dpi, pageNumber);
                            //allImg.Save(tiffFilePath, ImageFormat.Png);
                            allImg.Save(imageMs, ImageFormat.Png);
                            byte[] base64 = imageMs.ToArray();
                            string base64string = Convert.ToBase64String(base64);
                            itemObject.Add("img", "data:image/png;base64," + base64string);
                        }
                        else if (pageNumber == 1 && pageCount > 1)
                        {
                            allImg = _rasterizer.GetPage(desired_x_dpi, desired_y_dpi, pageNumber);
                            //allImg.Save(tiffFilePath, encoderInfo, encoderParameters);
                            allImg.Save(imageMs, ImageFormat.Png);
                            byte[] base64 = imageMs.ToArray();
                            string base64string = Convert.ToBase64String(base64);
                            itemObject.Add("img", "data:image/png;base64," + base64string);
                        }
                        else
                        {
                            allImg = _rasterizer.GetPage(desired_x_dpi, desired_y_dpi, pageNumber);
                            //allImg.Save(tiffFilePath, encoderInfo, encoderParameters);
                            allImg.Save(imageMs, ImageFormat.Png);
                            byte[] base64 = imageMs.ToArray();
                            string base64string = Convert.ToBase64String(base64);
                            itemObject.Add("img", "data:image/png;base64," + base64string);
                        }
                        ja.Add(itemObject);
                    }
                };
                jo.Add("pdfFile", ja);
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

        #region M10_ActOFD 20180730 執行OFD
        [HttpPost]
        public HttpResponseMessage M10_ActOFD(Models.api_Delivery.MD_T30_carshdu actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                //userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                #endregion

                #region ACall_checkIsDBNull 
                actRow.trailerId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.trailerId);
                actRow.dispatch_id = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dispatch_id);
                actRow.appSysId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.appSysId);
                actRow.latitudeDev = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.latitudeDev);
                actRow.longitudeDev = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.longitudeDev);
                #endregion

                if (string.IsNullOrEmpty(actRow.dcShip.ToString())) actRow.dcShip = "";
                if (string.IsNullOrEmpty(actRow.dispatch_id.ToString())) actRow.dispatch_id = "";
                if (string.IsNullOrEmpty(actRow.trailerId.ToString())) actRow.trailerId = "";
                if (string.IsNullOrEmpty(actRow.latitudeDev.ToString())) actRow.latitudeDev = "";
                if (string.IsNullOrEmpty(actRow.longitudeDev.ToString())) actRow.longitudeDev = "";

                if (string.IsNullOrEmpty(userData.UUID)) userData.UUID = "";

                if (actRow.longitudeDev.ToString().Length >= 11) actRow.longitudeDev = actRow.longitudeDev.ToString().Substring(0, 11);
                if (actRow.latitudeDev.ToString().Length >= 11) actRow.latitudeDev = actRow.latitudeDev.ToString().Substring(0, 11);


                DateTime dtUtc = DateTime.UtcNow;

                strSql.Clear();
                strSql.AppendLine(" select t1.bol_no, t1.arriveSeqD, t2.dcId from T30_carinv t1 ");
                strSql.AppendLine(" left join T30_carshdu t2 on t2.dcShip=t1.dcShip ");
                strSql.AppendLine(" where t1.dcShip='" + actRow.dcShip.ToString() + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception("Data not found.");
                //if (tbl_QueryData1.Rows.Count == 0) throw new Exception("查無資料");

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param = {
                                    new SqlParameter("dcShip", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.dcShip.ToString()),
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.dispatch_id.ToString()),
                                    new SqlParameter("realDelDate", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                                    new SqlParameter("taskStatus", SqlDbType.Char, 10, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "25"),
                                    new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("creatTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                        #endregion

                        strSql.Clear();
                        strSql.AppendLine(" Update T30_carshdu set realDelDate=@realDelDate, taskStatus=@taskStatus, actUser=@actUser, actTime=@actTime where dispatch_id=@dispatch_id ");
                        strSql.AppendLine(" Update T30_carinv set taskStatus='22', actTime=@actTime, actUser=@actUser where dcShip=@dcShip and arriveSeqD<>1 ");
                        strSql.AppendLine(" Update T30_carinv set taskStatus=@taskStatus, actTime=@actTime, actUser=@actUser where dcShip=@dcShip and arriveSeqD=1 ");


                        strSql.AppendLine(" Insert into App_deviceActivity (appSysId, sysUserId, UUID, longitudeDev, latitudeDev, dcShip, dcId, creatTime) ");
                        strSql.AppendLine(" values ('" + actRow.appSysId.ToString() + "', '" + userData.sysUserId.ToString() + "', '" + userData.UUID.ToString() + "', '" + actRow.longitudeDev.ToString() +
                            "', '" + actRow.latitudeDev.ToString() + "', '" + actRow.dcShip.ToString() + "', '" + tbl_QueryData1.Rows[0]["dcId"].ToString() + "', getutcDate()) ");

                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);

                        cn.Close();
                    }

                    using (System.Transactions.TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                    {
                        using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                        {
                            cn1.Open();


                            #region 設置 要傳入的 SqlParameter 資料
                            SqlParameter[] param = {
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.dispatch_id.ToString()),
                                    new SqlParameter("ofd_d_utc", SqlDbType.VarChar, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc.ToString("yyyyMMdd")),
                                    new SqlParameter("ofd_t_utc", SqlDbType.VarChar, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  dtUtc.ToString("HHmm")),
                                    new SqlParameter("status", SqlDbType.Char, 10, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "25"),
                                    new SqlParameter("driver_name", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.userName.ToString()),
                                    new SqlParameter("truck_no", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  actRow.carId),
                                    new SqlParameter("trailer_no", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  actRow.trailerId),
                                    new SqlParameter("epod_update_time_utc", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                            #endregion

                            strSql.Clear();
                            strSql.AppendLine(" Update DISPATCH set ofd_d_utc=@ofd_d_utc, ofd_t_utc=@ofd_t_utc, status=@status,driver_name=@driver_name,truck_no=@truck_no,trailer_no=@trailer_no, epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id ");
                            foreach (DataRow dr in tbl_QueryData1.Rows)
                            {
                                if (dr["arriveSeqD"].ToString() == "1")
                                {
                                    strSql.AppendLine(" Update BOL set status='25', epod_update_time_utc=@epod_update_time_utc where bol_no='" + dr["bol_no"].ToString() + "' ");
                                }
                                else
                                {
                                    strSql.AppendLine(" Update BOL set status='22', epod_update_time_utc=@epod_update_time_utc where bol_no='" + dr["bol_no"].ToString() + "' ");
                                }
                            }

                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn1, strSql.ToString(), param);
                            if (errStr != "") throw new Exception(errStr);

                            cn1.Close();
                        }
                        scopeNew.Complete();
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

        #region T21_ActChangeDriver 20180828 換司機運送
        [HttpPost]
        public HttpResponseMessage T21_ActChangeDriver(/*Models.api_Delivery.MD_T30_carshdu actRow*/)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {
                #region 取得 User FormsAuthenticationTicket
                var ticket = HttpContext.Current.Request.Params["ticket"];
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                string dispatch_id = HttpContext.Current.Request.Params["dispatch_id"];
                string driverId2 = HttpContext.Current.Request.Params["driverId2"];
                string isChange = HttpContext.Current.Request.Params["isChange"];

                if (isChange == "N") driverId2 = userData.userId;
                else
                {
                    if (driverId2 == userData.userId) throw new Exception(" you can't change yourself ");
                }

                DateTime dtUtc = DateTime.UtcNow;

                string tranCompId = SqlTool.GetOneDataValue(" select tranCompId from T10_driver where driverId='" + userData.userId + "' ").ToString();
                string tranCompId_us2 = SqlTool.GetOneDataValue(" select tranCompId from T10_driver where driverId='" + driverId2 + "' ").ToString();

                if (tranCompId != tranCompId_us2) throw new Exception(" You can't change another truck company driver. ");

                DataTable tbl_QueryData1 = SqlTool.GetDataTable(" select * from  T30_carshdu where dispatch_Id='" + dispatch_id + "' ", "tbl_QueryData1");
                DataTable tbl_QueryData2 = SqlTool.GetDataTable(" select * from  T10_driver t1 inner join S10_users t2 on t2.sysUserId=t1.sysUserId where t1.driverId='" + driverId2 + "' ", "tbl_QueryData2");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception(" dispatch not found ");
                if (tbl_QueryData2.Rows.Count == 0) throw new Exception(" Please check driver account ");

                DataTable T30_carshdu = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, " select * from T30_carshdu where dispatch_id='" + dispatch_id + "' ", "T30_carshdu");
                if (T30_carshdu.Rows.Count == 0) throw new Exception(" dispatch not found ");
                if (string.IsNullOrEmpty(T30_carshdu.Rows[0]["tranCompId2"].ToString()) && string.IsNullOrEmpty(T30_carshdu.Rows[0]["carId2"].ToString()) && string.IsNullOrEmpty(T30_carshdu.Rows[0]["driverId2"].ToString())) //string.IsNullOrEmpty(T30_carshdu.Rows[0]["trailerId2"].ToString()) &&
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                        {

                            cn.Open();

                            #region 設置 要傳入的 SqlParameter 資料
                            SqlParameter[] param = {
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, dispatch_id),
                                    new SqlParameter("tranCompId2", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["tranCompId"].ToString()),
                                    new SqlParameter("carId2", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["carId"].ToString()),
                                    new SqlParameter("trailerId2", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["trailerId"].ToString()),
                                    new SqlParameter("bindEndTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                                    new SqlParameter("sysUserId_run", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData2.Rows[0]["sysUserId"].ToString()),
                                    new SqlParameter("driverId2", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, driverId2),
                                    new SqlParameter("contactTel2", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData2.Rows[0]["contactTel"].ToString()),
                                    new SqlParameter("driverSeq", SqlDbType.SmallInt, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, 2),
                                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                            #endregion

                            strSql.Clear();
                            strSql.AppendLine(" Update T30_carshdu set driverId2=@driverId2, contactTel2=@contactTel2, tranCompId2=@tranCompId2, carId2=@carId2, trailerId2=@trailerId2, bindEndTime=@bindEndTime, bindTime2=@bindEndTime, sysUserId_run=@sysUserId_run, sysUserId2=@sysUserId_run, driverSeq=@driverSeq, actUser=@actUser, actTime=@actTime where dispatch_id=@dispatch_Id ");
                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                            if (errStr != "") throw new Exception(errStr);


                            cn.Close();
                        }

                        using (System.Transactions.TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                        {
                            using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                            {
                                cn1.Open();
                                #region 設置 要傳入的 SqlParameter 資料
                                SqlParameter[] param = {
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, dispatch_id),
                                    new SqlParameter("sec_driver_name", SqlDbType.NVarChar, 255, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData2.Rows[0]["userName"].ToString()),
                                    new SqlParameter("epod_update_time_utc", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                                #endregion

                                strSql.Clear();
                                strSql.AppendLine(" Update DISPATCH set sec_driver_name=@sec_driver_name, epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id ");
                                errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn1, strSql.ToString(), param);
                                if (errStr != "") throw new Exception(errStr);

                                cn1.Close();
                            }
                            scopeNew.Complete();
                        }
                        scope.Complete();
                    }
                }
                else
                {
                    string edit_sysUserId_run = "";

                    if (T30_carshdu.Rows[0]["sysUserId_run"].ToString() == T30_carshdu.Rows[0]["sysUserId"].ToString())
                    {
                        edit_sysUserId_run = T30_carshdu.Rows[0]["sysUserId2"].ToString();
                    }
                    else
                    {
                        edit_sysUserId_run = T30_carshdu.Rows[0]["sysUserId"].ToString();
                    }
                    #region 設置 要傳入的 SqlParameter 資料
                    SqlParameter[] param = {
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, dispatch_id),
                                    new SqlParameter("sysUserId_run", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, edit_sysUserId_run),
                                    new SqlParameter("driverSeq", SqlDbType.SmallInt, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, (T30_carshdu.Rows[0]["driverSeq"].ToString() == "1" ? "2" : "1")),
                                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                    #endregion
                    strSql.Clear();
                    strSql.AppendLine(" Update T30_carshdu set sysUserId_run=@sysUserId_run, driverSeq=@driverSeq, actUser=@actUser, actTime=@actTime where dispatch_id=@dispatch_id ");
                    errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                    if (errStr != "") throw new Exception(errStr);
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


        #region T30_ActBindDispatch 20180730 綁定配送單
        [HttpPost]
        public HttpResponseMessage T30_ActBindDispatch(Models.api_Delivery.MD_T30_carshdu actRow)
        {
            var resp = new HttpResponseMessage();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            clientIp = GetClientIp();
            JObject jo = new JObject();

            try
            {

                #region 取得 User FormsAuthenticationTicket
                actRow.ticket = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.ticket).ToString();
                //userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(actRow.ticket.ToString());
                var ticket = actRow.ticket.ToString();
                if (string.IsNullOrEmpty(ticket))
                {
                    ticket = HttpContext.Current.Request.Params["webTicket"];
                }
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserDataApp(ticket);
                #endregion

                #region ACall_checkIsDBNull 
                actRow.dispatch_id = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dispatch_id);
                actRow.tranCompId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.tranCompId);
                actRow.carId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.carId);
                actRow.trailerId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.trailerId);
                actRow.driverId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(userData.userId);
                actRow.contactTel = ZhConfig.ZhIniObj.ACall_checkIsDBNull(userData.contactTel);
                #endregion

                if (string.IsNullOrEmpty(actRow.trailerId.ToString())) actRow.trailerId = "";

                DateTime dtUtc = DateTime.UtcNow;

                strSql.Clear();
                strSql.AppendLine(" select dcShip, driverCnt, carId, carId2,* from T30_carshdu where dispatch_id='" + actRow.dispatch_id + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows[0]["carId"].ToString() != "" /*&& tbl_QueryData1.Rows[0]["tranCompId2"].ToString() != ""*/)
                {
                    throw new Exception("this car id has been bind,can't use.");
                    //throw new Exception("該車次已綁定，無法選擇");
                }

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {

                        cn.Open();

                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param = {
                                    new SqlParameter("dcShip", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, tbl_QueryData1.Rows[0]["dcShip"].ToString()),
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.dispatch_id.ToString()),
                                    new SqlParameter("tranCompId", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.tranCompId.ToString()),
                                    new SqlParameter("carId", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.carId.ToString()),
                                    new SqlParameter("trailerId", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.trailerId.ToString()),
                                    new SqlParameter("driverId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.driverId.ToString()),
                                    new SqlParameter("contactTel", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.contactTel.ToString()),
                                    new SqlParameter("bindTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                                    new SqlParameter("taskStatus", SqlDbType.Char, 10, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "20"),
                                    new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("creatTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                        #endregion

                        strSql.Clear();
                        if (tbl_QueryData1.Rows[0]["carId"].ToString() == "")
                        {
                            //strSql.AppendLine(" Update T30_carshdu set carId=@carId, trailerId=@trailerId, driverId=@driverId, contactTel=@contactTel, bindTime=@bindTime, taskStatus=@taskStatuz, actTime=@actTime, actUser=@actUser, sysUserId_run='" + userData.sysUserId + "', sysUserId='" + userData.sysUserId + "', driverSeq='1', where dispatch_id=@dispatch_Id ");
                            strSql.AppendLine(" Update T30_carshdu set carId=@carId, tranCompId=@tranCompId, trailerId=@trailerId, driverId=@driverId, contactTel=@contactTel, bindTime=@bindTime, taskStatus=@taskStatus, sysUserId=@sysUserId, sysUserId_run=@sysUserId, driverSeq='1', actTime=@actTime, actUser=@actUser where dispatch_id=@dispatch_Id ");

                            //將明細表一併更改狀態
                            strSql.AppendLine(" Update T30_carinv set taskStatus=@taskStatus, actTime=@actTime where dcShip=@dcShip ");
                        }
                        else
                        {
                            //strSql.AppendLine(" Update T30_carshdu set carId=@carId, tranCompId2=@tranCompId, trailerId2=@trailerId, driverId2=@driverId, contactTel2=@contactTel, bindEndTime=@actTime, bindTime2=@actTime, driverSeq='2', taskStatus=@taskStatus, actTime=@actTime, actUser=@actUser where dispatch_id=@dispatch_Id ");
                            strSql.AppendLine(" Update T30_carshdu set carId=@carId, tranCompId2=@tranCompId, trailerId2=@trailerId, driverId2=@driverId, contactTel2=@contactTel, bindEndTime=@actTime, taskStatus=@taskStatus, actTime=@actTime, actUser=@actUser where dispatch_id=@dispatch_Id ");
                        }

                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);

                        cn.Close();
                    }

                    using (System.Transactions.TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                    {
                        using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                        {
                            cn1.Open();


                            #region 設置 要傳入的 SqlParameter 資料
                            SqlParameter[] param = {
                                    new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.dispatch_id.ToString()),
                                    new SqlParameter("driver_name", SqlDbType.NVarChar, 255, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, userData.userName.ToString()),
                                    new SqlParameter("truck_no", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.carId.ToString()),
                                    new SqlParameter("trailer_no", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.trailerId.ToString()),
                                    new SqlParameter("status", SqlDbType.Char, 10, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "20"),
                                    new SqlParameter("epod_update_time_utc", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                                };
                            #endregion

                            strSql.Clear();
                            if (tbl_QueryData1.Rows[0]["carId"].ToString() == "")
                            {
                                strSql.AppendLine(" Update DISPATCH set driver_name=@driver_name, truck_no=@truck_no, trailer_no=@trailer_no, status=@status, epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id ");
                                strSql.AppendLine(" Update BOL set status=@status, epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id ");
                            }
                            else
                            {
                                strSql.AppendLine(" Update DISPATCH set sec_driver_name=@driver_name, truck_no=@truck_no, trailer_no=@trailer_no, status=@status, epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id ");
                            }

                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn1, strSql.ToString(), param);
                            if (errStr != "") throw new Exception(errStr);

                            cn1.Close();
                        }
                        scopeNew.Complete();
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


        #region 測試簽名跟PDF合併
        public HttpResponseMessage test()
        {
            var resp = new HttpResponseMessage();
            JObject jo = new JObject();
            string fileName = "ORD20180906001";
            string localPath = System.Web.HttpContext.Current.Server.MapPath("\\PDF\\" + fileName + ".pdf");

            MemoryStream stream = new MemoryStream();
            System.Drawing.Image img = System.Drawing.Image.FromFile(System.Web.HttpContext.Current.Server.MapPath("\\PDF\\Stest.png"));
            img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            byte[] data = stream.ToArray();
            //MemoryStream ms = new MemoryStream(data, 0, data.Length);
            //ms.Write(data, 0, data.Length);

            //byte[] data =System.Convert.FromBase64String(datas);
            DateTime dtUtc = DateTime.UtcNow;

            #region 取得bol pdf，並進行合併
            using (Stream inputPdfStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Stream inputImageStream = new MemoryStream(data)) //new MemoryStream(data)
            using (Stream outputPdfStream = new FileStream(localPath.Replace(fileName, "new_" + fileName), FileMode.Create, FileAccess.Write, FileShare.None))
            {
                iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(inputImageStream);

                var reader = new PdfReader(inputPdfStream);
                var stamper = new PdfStamper(reader, outputPdfStream);
                var pdfContentByte = stamper.GetOverContent(1);

                image.ScaleAbsoluteWidth(20);
                image.ScaleAbsoluteHeight(image.Height / (image.Width / 20));
                image.SetAbsolutePosition(345, 35);
                pdfContentByte.AddImage(image);


                BaseFont bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                var pod = pdfContentByte.CreateTemplate(120, 60);
                pod.BeginText();

                pod.SetFontAndSize(bf, 10);
                pod.SetTextMatrix(0, 5);
                pod.ShowText("Ray");
                pod.EndText();

                pdfContentByte.SetColorFill(BaseColor.BLACK);
                pdfContentByte.AddTemplate(pod, 200, 30);

                var date = pdfContentByte.CreateTemplate(120, 60);
                date.BeginText();

                date.SetFontAndSize(bf, 10);
                date.SetTextMatrix(0, 0);
                DateTime dt = Models.MEC_MethodMD.TimeZone.ChangeTimeZone(dtUtc, "America/Chicago");
                date.ShowText(dt.ToString("MM/dd/yyyy HH:mm"));
                date.EndText();

                pdfContentByte.SetColorFill(BaseColor.BLACK);
                pdfContentByte.AddTemplate(date, 493, 35);

                inputPdfStream.Close();

                stamper.Close();

                #region 上傳pdf至s3
                //IAmazonS3 client;
                //saveKey = "pod/" + dispatch_id + "/" + fileName + ".pdf";
                //using (client = Amazon.AWSClientFactory.CreateAmazonS3Client(Amazon.RegionEndpoint.USWest2))
                //{
                //    var request = new PutObjectRequest()
                //    {
                //        BucketName = _bucketName,
                //        Key = saveKey,
                //        FilePath = localPath.Replace(fileName, "new_" + fileName)//SEND THE FILE STREAM
                //    };

                //    PutObjectResponse response = client.PutObject(request);
                //}
                #endregion
            }
            #endregion
            jo.Add("resultCode", resultCode);
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
        #endregion
    }
}
