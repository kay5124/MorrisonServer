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
    public class A10060Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A10060";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);

        List<SelectListItem> selItem_lbl_cmb1;
        List<SelectListItem> selItem_lbl_cmb2;
        List<SelectListItem> selItem_lbl_cmb3;
        List<SelectListItem> selItem_lbl_cmb4;

        #endregion

        // GET: A10060
        public ActionResult A10060()
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
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "sysUserId", Text = "sysUserId" });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "userId", Text = "User Id" });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "userName", Text = "User Name" });
            }

            if (selItem_lbl_cmb2 == null)
            {
                selItem_lbl_cmb2 = new List<SelectListItem>();
                selItem_lbl_cmb2.Add(new SelectListItem { Value = "", Text = "Select", Selected = true });
                selItem_lbl_cmb2.Add(new SelectListItem { Value = "True", Text = "Enable" });
                selItem_lbl_cmb2.Add(new SelectListItem { Value = "False", Text = "Disable" });
            }

            if (selItem_lbl_cmb3 == null)
            {
                selItem_lbl_cmb3 = new List<SelectListItem>();
                selItem_lbl_cmb3.Add(new SelectListItem { Value = "True", Text = "Enable" });
                selItem_lbl_cmb3.Add(new SelectListItem { Value = "False", Text = "Disable" });
            }

            if (selItem_lbl_cmb4 == null)
            {
                selItem_lbl_cmb4 = new List<SelectListItem>();
                selItem_lbl_cmb4.Add(new SelectListItem { Value = "", Text = "Select", Selected = true });
                selItem_lbl_cmb4.Add(new SelectListItem { Value = "Android", Text = "Android" });
                selItem_lbl_cmb4.Add(new SelectListItem { Value = "iOS", Text = "IOS" });
            }

            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;
            ViewBag.selItem_lbl_cmb2 = selItem_lbl_cmb2;
            ViewBag.selItem_lbl_cmb3 = selItem_lbl_cmb3;
            ViewBag.selItem_lbl_cmb4 = selItem_lbl_cmb4;

            return View();
        }


        #region 提供 datagrid資料
        public ActionResult GetGridJSON(int page, int rows, string order, string lbl_cmb1, string value_cmb1, string value_isNotice, string value_isUse, string value_platform)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                #region Gen strSql by ROW_NUMBER
                string tableName = "U_" + funcId;
                if (string.IsNullOrEmpty(order))
                {
                    order = " sysUserId ";
                }


                //strSql.Remove(0, strSql.Length);

                #region strCond
                string strCond = " where 1=1 ";

                if (!string.IsNullOrEmpty(lbl_cmb1) && !string.IsNullOrEmpty(value_cmb1))
                {
                    strCond += " and " + lbl_cmb1 + " like '%" + value_cmb1 + "%'";
                }

                if (!string.IsNullOrEmpty(value_isUse))
                {
                    strCond += " and isUse='" + value_isUse + "'";
                }

                if (!string.IsNullOrEmpty(value_isNotice))
                {
                    strCond += " and isNotice='" + value_isNotice + "'";
                }

                if (!string.IsNullOrEmpty(value_platform))
                {
                    strCond += " and platform='" + value_platform + "'";
                }
                #endregion

                string colBtnEdit = "";
                colBtnEdit += "'<a href=\"javascript:void(0)\" onclick=\"btnEdit(''' + rtrim(sysUserId) + ''',''' + rtrim(UUID) + ''')\" class=\"btn btn-primary\">Edit</a>'";

                string colBtnDelete = "";
                colBtnDelete += "'<a href=\"javascript:void(0)\" onclick=\"btnDelete(''' + rtrim(sysUserId) + ''',''' + rtrim(UUID) + ''')\" class=\"btn btn-danger\">Delete</a>'";


                strSql.Clear();
                strSql.Append(" select *, " + colBtnEdit + " btnEdit, " + colBtnDelete + " btnDelete from ( SELECT  ROW_NUMBER() OVER (ORDER BY " + order + ") AS RowNum, * FROM ");
                strSql.Append(tableName + strCond);
                strSql.Append(" )  AS t1 ");
                strSql.Append(" where (t1.RowNum > " + ((rows * page) - rows) + " and t1.RowNum <= " + (rows * page) + ") ");

                #endregion

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), tableName);

                int totalCount = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(ZhConfig.GlobalSystemVar.StrConnection1, "select count(*) from " + tableName + strCond).ToString());

                int totalPage = (int)Math.Ceiling((double)totalCount / (double)rows);

                JArray ja = new JArray();
                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    var itemObject = new JObject();

                    foreach (DataColumn dc in tbl_QueryData1.Columns)
                    {
                        if (dc.ColumnName == "RowNum")
                        {
                            continue;
                        }
                        itemObject.Add(dc.ColumnName, dr[dc].ToString());
                    }

                    ja.Add(itemObject);
                }

                jo.Add("totalCount", totalCount);
                jo.Add("totalPage", totalPage);
                jo.Add("rows", ja);
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
        public ActionResult GetEditData(string pk, string pk2)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                strSql.Clear();
                strSql.AppendLine(" select t1.*, us1.userName, us1.userId from App_userDevice t1  ");
                strSql.AppendLine(" left join S10_users us1 on us1.sysUserId=t1.sysUserId ");
                strSql.AppendLine(" where t1.sysUserId='" + pk + "' and t1.UUID='" + pk2 + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception("Data not found.,can't modify.");

                foreach (DataColumn dc in tbl_QueryData1.Columns)
                {
                    jo.Add(dc.ColumnName, tbl_QueryData1.Rows[0][dc].ToString());
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

        #region Gen ActionResult ActSingle/DeleteSingle

        public ActionResult ActSingle(Models.Base.MD_App_userDevice actRow)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            StringBuilder tmpStrSql = new StringBuilder();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                #region ACall_checkIsDBNull
                actRow.sysUserId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.sysUserId);
                actRow.UUID = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.UUID);
                actRow.isNotice = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.isNotice);
                actRow.isUse = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.isUse);
                #endregion


                #region 新增或修改 資料到後端資料庫

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param = {
                                new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.sysUserId),
                                new SqlParameter("UUID", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.UUID),
                                new SqlParameter("isUse", SqlDbType.Bit, 1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.isUse),
                                new SqlParameter("isNotice", SqlDbType.Bit, 1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.isNotice),
                                new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString())

                        };

                        #endregion
                        strSql.Clear();
                        strSql.AppendLine("update App_userDevice set isNotice=@isNotice,isUse=@isUse, actUser=@actUser, actTime=getDate() where sysUserId=@sysUserId and UUID=@UUID ");
                        operStatusId = "40";
                        actRow.actUser = userData.userName;
                        actRow.actTime = DateTime.Now;

                        errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);

                        tmpStrSql.AppendLine(strSql.ToString());
                        #endregion
                    }
                    scope.Complete();
                }

                #region return Info

                JArray ja = new JArray();

                var itemObject = new JObject();

                itemObject.Add("sysUserId", actRow.sysUserId.ToString());
                itemObject.Add("actUser", actRow.actUser.ToString());
                itemObject.Add("actTime", actRow.actTime.ToString());

                ja.Add(itemObject);

                jo.Add("row", ja);
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
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }

        public ActionResult DeleteSingle(string sysUserId, string UUID)
        {
            JObject jo = new JObject();
            operStatusId = "50";
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                #region 刪除 後端資料庫

                strSql.Append("Delete from App_userDevice where sysUserId=@pk_sysUserId and UUID=@pk_UUID ");

                #region 設置 要傳入的 SqlParameter 資料

                SqlParameter[] param = {
                    new SqlParameter("pk_sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "sysUserId", DataRowVersion.Original,sysUserId),
                    new SqlParameter("pk_UUID", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Original,UUID)};

                #endregion

                errStr = ZhClass.SqlTool.ExecuteNonQuery(strSql.ToString(), param);
                if (errStr != "") throw new Exception(errStr);
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
                //operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");

        }

        #endregion
    }
}