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
    public class A10012Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A10012";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);

        List<SelectListItem> selItem_lbl_cmb1;
        List<SelectListItem> selItem_lbl_cmb2;

        #endregion
        // GET: A10012
        public ActionResult A10012()
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
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "appGroupId", Text = "Group Id", Selected = true });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "appGroupName", Text = "Group Name" });
            }
            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            if (selItem_lbl_cmb2 == null)
            {
                selItem_lbl_cmb2 = new List<SelectListItem>();
                selItem_lbl_cmb2.Add(new SelectListItem { Value = "sysUserId", Text = "sysUserId", Selected = true });
                selItem_lbl_cmb2.Add(new SelectListItem { Value = "userId", Text = "User Id" });
                selItem_lbl_cmb2.Add(new SelectListItem { Value = "userName", Text = "User Name" });
            }
            ViewBag.selItem_lbl_cmb2 = selItem_lbl_cmb2;


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
                    order = " appGroupId ";
                }

                string strCond = " where 1=1 and statusId in ('10', '20') ";

                if (!string.IsNullOrEmpty(lbl_cmb1) && !string.IsNullOrEmpty(value_cmb1))
                {
                    strCond += " and " + lbl_cmb1 + " like '%" + value_cmb1 + "%' ";
                }

                string colBtnEdit = "";
                colBtnEdit += "'<a href=\"javascript:void(0)\" onclick=\"btnEdit(''' + rtrim(appGroupId) + ''')\" class=\"btn btn-primary\">Edit</a>'";

                strSql.Clear();
                strSql.Append(" select *, " + colBtnEdit + " btnEdit from ( SELECT  ROW_NUMBER() OVER (ORDER BY " + order + ") AS RowNum, * FROM ");
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

        #region Control_GetGridJSON
        public ActionResult GetGridJSONDetail(int page, int rows, string order, string appGroupId, string lbl_cmb2, string value_cmb2)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            StringBuilder tmpStrSql = new StringBuilder(200);
            try
            {
                string tableName = "spU_" + funcId;

                if (string.IsNullOrEmpty(order))
                {
                    order = " appGroupId ";
                }

                string tmpStr = "";

                switch (lbl_cmb2)
                {
                    case "":
                        tmpStr += ",'', '', ''";
                        break;
                    case "sysUserId":
                        tmpStr += ",'" + value_cmb2 + "', '', ''";
                        break;
                    case "userId":
                        tmpStr += ",'', '" + value_cmb2 + "', ''";
                        break;
                    case "userName":
                        tmpStr += ",'', '', '" + value_cmb2 + "'";
                        break;
                }
                //exec spU_S00030 'G01',1,20
                //par1:App群組 appGroupId
                //para2:使用者代碼 sysUserId
                //para3:使用者帳號 userId
                //para4:使用者姓名  userName
                //para5:資列起始編號
                //para6:資料結束編號

                strSql.Append("spU_" + funcId + " '" + appGroupId + "'" + tmpStr + ",'" + ((page - 1) * rows + 1).ToString() + "','" + page * rows + "'");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "spU_A10012");
                tmpStrSql.AppendLine(strSql.ToString());

                strSql.Clear();
                strSql.Append("spU_" + funcId + "Count '" + appGroupId + "'" + tmpStr);
                DataTable tbl_QueryData2 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "spU_A10012Count");
                tmpStrSql.AppendLine(strSql.ToString());

                JArray ja = new JArray();

                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    var itemObject = new JObject();
                    foreach (DataColumn dc in tbl_QueryData1.Columns)
                    {
                        switch (dc.ColumnName)
                        {
                            case "isExist":
                                if (dr[dc].ToString() == "True")
                                {
                                    itemObject.Add("isExist", true);
                                }
                                else
                                {
                                    itemObject.Add("isExist", false);
                                }
                                break;
                            default:
                                itemObject.Add(dc.ColumnName, dr[dc].ToString());
                                break;
                        }
                    }
                    ja.Add(itemObject);
                }

                int totalCount = Convert.ToInt32(tbl_QueryData2.Rows[0][0].ToString());

                int totalPage = (int)Math.Ceiling((double)totalCount / (double)rows);

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
                //operDr["strSql"] = tmpStrSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }
        #endregion

        #region Form_Act

        public ActionResult ActSingle(Models.Base.MD_App_group actRow)//接收單筆資料方式
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                actRow.appGroupId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.appGroupId);
                actRow.chkAll = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.chkAll);
                actRow.chk = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.chk);

                DateTime dtNow = DateTime.Now;

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param = {
                            new SqlParameter("appGroupId", SqlDbType.Char, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.appGroupId),
                            new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId),
                            new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtNow.ToString("yyyy-MM-dd HH:mm:ss"))
                        };
                        #endregion

                        #region 先刪除datagrid 中的選項
                        strSql.Clear();
                        strSql.AppendLine(" delete from App_userGroups where sysUserId in (" + actRow.chkAll + ") and appGroupId=@appGroupId ");
                        #endregion

                        #region 將已勾選的使用者加入至群組
                        string[] chks = actRow.chk.ToString().Split(',');
                        for (int i = 0; i < chks.Length; i++)
                        {
                            if(chks[i] != "") strSql.AppendLine(" Insert into App_userGroups (appGroupId, sysUserId) values (@appGroupId, '" + chks[i] + "') ");
                        }
                        #endregion

                        #region 更新App_groups異動人員與時間
                        strSql.AppendLine(" Update App_group set actUser=@actUser, actTime=@actTime where appGroupId=@appGroupId ");
                        #endregion

                        operStatusId = "40";

                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);
                    }
                    scope.Complete();
                }


                JArray ja = new JArray();
                var itemObject = new JObject();
                itemObject.Add("actUser", userData.userName);
                itemObject.Add("actTime", dtNow.ToString());
                ja.Add(itemObject);

                jo.Add("row", ja);

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
                //查詢是否有使用者隸屬於該群組下
                int count = Convert.ToInt32(SqlTool.GetOneDataValue("SELECT COUNT(*) FROM App_userGroups WHERE appGroupId='" + pks + "'"));

                #region 刪除 Server端的資料
                if (count == 0)
                {


                    strSql.Clear();
                    strSql.Append("update App_group set statusId='30' where appGroupId=@appGroupId");

                    SqlParameter[] param ={
                        new SqlParameter("appGroupId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pks)};

                    errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);

                }
                else
                {
                    errStr = "Existing users have used this group and cannot be deleted.";
                    //errStr = "Existing users have used this group and cannot be deleted.";
                    throw new Exception(errStr);
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