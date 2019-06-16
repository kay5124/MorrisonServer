using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MorrisonServer.Controllers
{
    public class A10050_oldController : Controller
    {
        #region Bacic variables
        string errStr = "";
        string funcId = "A10050_old";
        string resultCode = "10";
        string operStatusId = "20";
        StringBuilder strSql = new StringBuilder(200);

        List<SelectListItem> selItem_lbl_cmb1;

        #endregion
        // GET: A10050_old
        public ActionResult A10050_old()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            if (selItem_lbl_cmb1 == null)
            {
                selItem_lbl_cmb1 = new List<SelectListItem>();
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "userId", Text = "User Id" });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "userName", Text = "User Name" });
            }

            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            return View();
        }


        public ActionResult GetGridJSON(int page, int rows, string sort, string order, string lbl_cmb1, string value_cmb1, string value_appSysId, string value_date1, string value_date2)
        {
            JObject jo = new JObject();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                string sortId = "actSerial";
                string tableName = "U_" + funcId;

                if (!string.IsNullOrEmpty(sort))
                {
                    sortId = sort;
                }
                if (!string.IsNullOrEmpty(order))
                {
                    sortId = sort + " " + order;
                }

                string strCond = " where 1=1 and creatTime between '" + value_date1 + "' and '" + value_date2 + "' ";

                if (!string.IsNullOrEmpty(value_appSysId))
                {
                    strCond += " and statusId='" + value_appSysId + "' ";
                }


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
    }
}