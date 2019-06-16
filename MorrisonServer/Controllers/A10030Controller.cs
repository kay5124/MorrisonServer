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
    public class A10030Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A10030";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);
        List<SelectListItem> selItem_lbl_cmb1;
        #endregion

        // GET: A10030
        public ActionResult A10030()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"] == "")
                ViewBag.ServerName = "";
            else
                ViewBag.ServerName = "/" + System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"];

            if (selItem_lbl_cmb1 == null)
            {
                selItem_lbl_cmb1 = new List<SelectListItem>();
                //selItem_lbl_cmb1.Add(new SelectListItem { Value = "systemId", Text = "系統代碼", Selected = true });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "systemName", Text = "System Name" });
            }
           
            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            ViewBag.selItem_value_appSysId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.No, "", "DP", "");


            return View();
        }

        #region Control_GetGridJSON
        public ActionResult GetGridJSON(int page, int rows, string order, string lbl_cmb1, string value_cmb1)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                string tableName = "U_" + funcId;

                if (string.IsNullOrEmpty(order))
                {
                    order = " appSysId ";
                }

                string strCond = " where 1=1 and statusId2 in ('10', '20') ";

                if (!string.IsNullOrEmpty(lbl_cmb1) && !string.IsNullOrEmpty(value_cmb1))
                {
                    strCond += " and " + lbl_cmb1 + " like '%" + value_cmb1 + "%' ";
                }


                string colBtnEdit = "";
                colBtnEdit += "'<a href=\"javascript:void(0)\" onclick=\"btnEdit(''' + rtrim(appSysId) + ''')\" class=\"btn btn-primary\">Edit</a>'";

                string colBtnDelete = "";
                colBtnDelete += "'<a href=\"javascript:void(0)\" onclick=\"btnDelete(''' + rtrim(appSysId) + ''')\" class=\"btn btn-danger\">Delete</a>'";

                strSql.Clear();
                strSql.Append(" select *, " + colBtnEdit + " btnEdit, " + colBtnDelete + " btnDelete from ( SELECT  ROW_NUMBER() OVER (ORDER BY " + order + ") AS RowNum, * FROM ");
                strSql.Append(tableName + strCond);
                strSql.Append(" )  AS t1 ");
                strSql.Append(" where (t1.RowNum > " + ((rows * page) - rows) + " and t1.RowNum <= " + (rows * page) + ") ");

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                int totalCount = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(ZhConfig.GlobalSystemVar.StrConnection1, "select count(*) from " + tableName + strCond).ToString());
                int totalPage = (int)Math.Ceiling((double)totalCount / (double)rows);

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
                jo.Add("totalCount", totalCount);
                jo.Add("totalPage", totalPage);
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
                operDr["statusId2"] = operStatusId2;
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


        #region Form_GetEditData
        public ActionResult GetEditData(string pk)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                strSql.Clear();
                strSql.AppendLine(" select * from S00_systemConfig where appSysId='" + pk + "' ");
                //strSql.AppendLine(" select * from S00_systemConfig where systemId='" + pk + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception("Check this information or this information has been pushed,can't change.");
                //if (tbl_QueryData1.Rows.Count == 0) throw new Exception("查無此資料或此筆資料已發送推播，無法修改");

                foreach (DataColumn dc in tbl_QueryData1.Columns)
                {
                    jo.Add(dc.ColumnName, tbl_QueryData1.Rows[0][dc].ToString());
                }

                if(tbl_QueryData1.Rows[0]["statusId"].ToString() == "10")
                {
                    DataTable tbl_url = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, " select * from S00_systemConfigLink where appSysId='" + tbl_QueryData1.Rows[0]["appSysId"].ToString() + "'", "tbl_userGroups");
                    DataRow[] drs = tbl_url.Select("platform='Android'");
                    if(drs.Length > 0)
                    {
                        jo.Add("aUrl", drs[0]["url"].ToString());
                    }
                    else
                    {
                        jo.Add("aUrl", "");
                    }

                    drs = tbl_url.Select("platform='iOS'");
                    if (drs.Length > 0)
                    {
                        jo.Add("iUrl", drs[0]["url"].ToString());
                    }
                    else
                    {
                        jo.Add("iUrl", "");
                    }
                }
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }


            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

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

                actRow.appSysId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.appSysId);
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
                    sql = " select * from S00_systemConfig where appSysId='" + actRow.appSysId.ToString() + "' ";
                    //sql = " select * from S00_systemConfig where systemId='" + actRow.systemId.ToString() + "' ";
                    DataTable tmpTbl = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, sql, "tmpTbl");
                    if (tmpTbl.Rows.Count > 0)
                    {
                        throw new Exception("System code is repeated, Please adjust.");

                    }
                }
                #endregion

                DateTime dtNow = DateTime.Now;

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                    new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.appSysId),
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
                        //strSql.AppendLine(" insert into S00_systemConfig (systemId,systemName,version,statusId) values (@systemId,@systemName,@version,@statusId) ");
                        //strSql.AppendLine(" insert into S00_systemConfigLink (systemId, platform, url) values (@systemId, 'Android', @aUrl) ");
                        //strSql.AppendLine(" insert into S00_systemConfigLink (systemId, platform, url) values (@systemId, 'iOS', @iUrl) ");
                        strSql.AppendLine(" insert into S00_systemConfig (appSysId,systemName,version,statusId) values (@appSysId,@systemName,@version,@statusId) ");
                        strSql.AppendLine(" insert into S00_systemConfigLink (appSysId, platform, url) values (@appSysId, 'Android', @aUrl) ");
                        strSql.AppendLine(" insert into S00_systemConfigLink (appSysId, platform, url) values (@appSysId, 'iOS', @iUrl) ");
                        operStatusId = "30";
                        break;
                    case "M":
                        //strSql.AppendLine(" update S00_systemConfig set systemName=@systemName,version=@version where systemId=@systemId ");
                        //strSql.AppendLine(" update S00_systemConfigLink set url=@aUrl where systemId=@systemId and platform='Android' ");
                        //strSql.AppendLine(" update S00_systemConfigLink set url=@iUrl where systemId=@systemId and platform='iOS' ");
                        strSql.AppendLine(" update S00_systemConfig set systemName=@systemName,version=@version where appSysId=@appSysId ");
                        //strSql.AppendLine(" update S00_systemConfigLink set url=@aUrl where appSysId=@appSysId and platform='Android' ");
                        //strSql.AppendLine(" update S00_systemConfigLink set url=@iUrl where appSysId=@appSysId and platform='iOS' ");
                        strSql.AppendLine(" Delete from S00_systemConfigLink where appSysId=@appSysId and platform='Android' ");
                        strSql.AppendLine(" Delete from S00_systemConfigLink where appSysId=@appSysId and platform='iOS' ");
                        strSql.AppendLine(" insert into S00_systemConfigLink (appSysId, platform, url) values (@appSysId, 'Android', @aUrl) ");
                        strSql.AppendLine(" insert into S00_systemConfigLink (appSysId, platform, url) values (@appSysId, 'iOS', @iUrl) ");

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
                operDr["statusId2"] = operStatusId2;
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
                operDr["statusId2"] = operStatusId2;
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