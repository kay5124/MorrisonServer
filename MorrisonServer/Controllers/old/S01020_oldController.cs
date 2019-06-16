using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using ZhClass;

namespace MorrisonServer.Controllers
{
    public class S01020_oldController : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "S01020_old";
        StringBuilder strSql = new StringBuilder(200);

        List<SelectListItem> selItem_lbl_cmb1;

        #endregion
        // GET: S01020_old
        public ActionResult S01020_old()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            if (selItem_lbl_cmb1 == null)
            {
                selItem_lbl_cmb1 = new List<SelectListItem>();
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "groupId", Text = "Group Id", Selected = true });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "groupName", Text = "Group Name" });
            }
            ViewBag.selItem_lbl_cm1 = selItem_lbl_cmb1;


            return View();
        }

        #region Control_GetGridJSON
        public ActionResult GetGridJSON(int page, int rows, string sort, string order, string lbl_cmb1, string value_cmb1)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                string sortId = "groupId";
                string tableName = "U_" + funcId;

                if (!string.IsNullOrEmpty(sort))
                {
                    sortId = sort;
                }
                if (!string.IsNullOrEmpty(order))
                {
                    sortId = sort + " " + order;
                }

                string strCond = " where 1=1 and statusId in ('10', '20') ";

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

        #region Form_Act

        public ActionResult ActSingle(Models.Base.MD_S10_group actRow)//接收單筆資料方式
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                actRow.groupId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.groupId);
                actRow.groupName = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.groupName);
                actRow.statusId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusId);
                actRow.statusName = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusName);
                actRow.statusType = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusType);
                actRow.memo = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.memo);

                #region 判斷群組編號,不可重複
                string sql = "";
                if (actRow.RowStatus.ToString() == "A")
                {
                    sql = " select * from S10_group where groupId='" + actRow.groupId.ToString() + "' ";
                    DataTable tmpTbl = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, sql, "tmpTbl");
                    if (tmpTbl.Rows.Count > 0)
                    {
                        if(tmpTbl.Rows[0]["statusId"].ToString() == "30")
                        {
                            actRow.RowStatus = "M";
                        }
                        else
                        {
                            throw new Exception("User group code is duplicated, Please adjust");
                        }
                    }
                }
                #endregion

                DateTime dtNow = DateTime.Now;

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                    new SqlParameter("groupId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.groupId),
                    new SqlParameter("groupName", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.groupName),
                    new SqlParameter("memo", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.memo),
                    new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.statusId),
                    new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId),
                    new SqlParameter("creatTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtNow.ToString("yyyy-MM-dd HH:mm:ss")),
                    new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId),
                    new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtNow.ToString("yyyy-MM-dd HH:mm:ss"))
                    };
                #endregion

                strSql.Clear();
                switch (actRow.RowStatus.ToString())
                {
                    case "A":
                        strSql.Append("insert into S10_group (groupId,groupName,memo,statusId,creatUser,creatTime) values (@groupId,@groupName,@memo,@statusId,@creatUser,@creatTime)");
                        operStatusId = "30";
                        break;
                    case "M":
                        strSql.Append("update S10_group set groupName=@groupName,statusId=@statusId,memo=@memo,actUser=@actUser,actTime=@actTime where groupId=@groupId");
                        operStatusId = "40";
                        break;
                }

                errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);

                if (errStr != "") throw new Exception(errStr);

                JArray ja = new JArray();

                var itemObject = new JObject();
                if (actRow.RowStatus.ToString() == "M")
                {
                    itemObject.Add("actUser", userData.userName);
                    itemObject.Add("actTime", dtNow.ToString());
                }
                else // "A"
                {
                    itemObject.Add("creatUser", userData.userName);
                    itemObject.Add("creatTime", dtNow.ToString());
                }
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
                int count = Convert.ToInt32(SqlTool.GetOneDataValue("SELECT COUNT(*) FROM S10_funcLimit WHERE groupId='" + pks + "'"));
                
                #region 刪除 Server端的資料
                if (count == 0)
                {


                    strSql.Clear();
                    strSql.Append("update S10_group set statusId='30' where groupId=@groupId");

                    SqlParameter[] param ={
                        new SqlParameter("groupId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pks)};

                    errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);

                }
                else
                {
                    errStr = "Existing users have used this group and cannot be deleted.";
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