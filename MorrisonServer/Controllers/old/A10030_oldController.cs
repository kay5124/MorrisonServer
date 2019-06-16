using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using ZhClass;


namespace MorrisonServer.Controllers
{
    public class A10030_oldController : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A10030_old";
        StringBuilder strSql = new StringBuilder(200);
        List<SelectListItem> selItem_lbl_cmb1;
        #endregion

        // GET: A10030_old
        public ActionResult A10030_old()
        {
            if(selItem_lbl_cmb1 == null)
            {
                selItem_lbl_cmb1 = new List<SelectListItem>();
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "systemId", Text = "系統代碼", Selected = true });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "systemName", Text = "系統名稱" });
            }
           
            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            ViewBag.selItem_value_appSysId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.No, "", "DP", "");


            return View();
        }

        #region Control_GetGridJSON
        public ActionResult GetGridJSON(int page, int rows, string sort, string order, string lbl_cmb1, string value_cmb1)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                string sortId = "systemId";
                string tableName = "U_" + funcId;

                if (!string.IsNullOrEmpty(sort))
                {
                    sortId = sort;
                }
                if (!string.IsNullOrEmpty(order))
                {
                    sortId = sort + " " + order;
                }

                string strCond = " where 1=1 and statusId2 in ('10', '20') ";

                if (!string.IsNullOrEmpty(lbl_cmb1) && !string.IsNullOrEmpty(value_cmb1))
                {
                    strCond += " and " + lbl_cmb1 + " like '%" + value_cmb1 + "%' ";
                }

                strSql.Clear();
                strSql.Append(" select * from (select ROW_NUMBER() over (order by " + sortId + ") as RowNum, * from " + tableName + strCond + " ) AS NewTable ");
                strSql.Append(" WHERE RowNum >= " + ((page - 1) * rows + 1).ToString() + " AND RowNum <=" + page * rows);

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                int totalCount = int.Parse(ZhClass.SqlTool.GetOneDataValue("select count(*) from " + tableName + strCond).ToString());

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
                jo.Add("total", totalCount);
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

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
                operDr["statusId"] = operStatusId;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql;

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }
        #endregion

        #region Control_GetGridJSON
        public ActionResult GetAppUrl(string systemId)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                strSql.Clear();
                strSql.AppendLine(" select * from S00_systemConfigLink where systemId='" + systemId + "' ");

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                string aUrl = "", iUrl = "";


                if(tbl_QueryData1.Rows.Count > 0)
                {
                    for(int i = 0; i < tbl_QueryData1.Rows.Count; i++)
                    {
                        switch (tbl_QueryData1.Rows[i]["platform"].ToString())
                        {
                            case "Android":
                                aUrl = tbl_QueryData1.Rows[i]["url"].ToString();
                                break;
                            case "iOS":
                                iUrl = tbl_QueryData1.Rows[i]["url"].ToString();
                                break;
                        }
                    }
                }

                jo.Add("aUrl", aUrl);
                jo.Add("iUrl", iUrl);
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

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
                operDr["statusId"] = operStatusId;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql;

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }
        #endregion


        #region Form_Act

        public ActionResult ActSingle(Models.Base.MD_S00_systemConfig actRow)//接收單筆資料方式
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                //actRow.systemId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.systemId);
                actRow.systemName = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.systemName);
                actRow.version = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.version);
                actRow.statusId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusId);
                actRow.statusName = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusName);
                actRow.aUrl = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.aUrl);
                actRow.iUrl = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.iUrl);

                #region 判斷群組編號,不可重複
                string sql = "";
                if (actRow.RowStatus.ToString() == "A")
                {
                    //sql = " select * from S00_systemConfig where systemId='" + actRow.systemId.ToString() + "' ";
                    DataTable tmpTbl = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, sql, "tmpTbl");
                    if (tmpTbl.Rows.Count > 0)
                    {
                        throw new Exception("系統代碼重覆，請調整");

                    }
                }
                #endregion

                DateTime dtNow = DateTime.Now;

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                    //new SqlParameter("systemId", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.systemId),
                    new SqlParameter("systemName", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.systemName),
                    new SqlParameter("version", SqlDbType.VarChar, 16, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.version),
                    new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.statusId),
                    new SqlParameter("aUrl", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.aUrl),
                    new SqlParameter("iUrl", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.iUrl)
                    };
                #endregion

                strSql.Clear();
                switch (actRow.RowStatus.ToString())
                {
                    case "A":
                        strSql.AppendLine(" insert into S00_systemConfig (systemId,systemName,version,statusId) values (@systemId,@systemName,@version,@statusId) ");
                        strSql.AppendLine(" insert into S00_systemConfigLink (systemId, platform, url) values (@systemId, 'Android', @aUrl) ");
                        strSql.AppendLine(" insert into S00_systemConfigLink (systemId, platform, url) values (@systemId, 'iOS', @iUrl) ");
                        operStatusId = "30";
                        break;
                    case "M":
                        strSql.AppendLine(" update S00_systemConfig set systemName=@systemName,version=@version where systemId=@systemId ");
                        strSql.AppendLine(" update S00_systemConfigLink set url=@aUrl where systemId=@systemId and platform='Android' ");
                        strSql.AppendLine(" update S00_systemConfigLink set url=@iUrl where systemId=@systemId and platform='iOS' ");
                        operStatusId = "40";
                        break;
                }

                errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);

                if (errStr != "") throw new Exception(errStr);

                jo.Add("statusName", actRow.statusName.ToString());
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

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
                operDr["statusId"] = operStatusId;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql;

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }
        #endregion

        #region Form_Delete
        [HttpPost]

        public ActionResult DeleteSingle(string pks)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            operStatusId = "50";
            JObject jo = new JObject();
            try
            {
                #region 刪除 Server端的資料
                strSql.Clear();
                //strSql.Append("Update S00_systemConfig set statusId2='30' where systemId=@systemId ");
                strSql.Append("delete from S00_systemConfig where systemId=@systemId ");
                strSql.Append("delete from S00_systemConfigLink where systemId=@systemId ");

                SqlParameter[] param ={
                        new SqlParameter("systemId", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pks)};

                errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                #endregion
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

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
                operDr["statusId"] = operStatusId;
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                //operDr["strSql"] = strSql;

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }
        #endregion
    }
}