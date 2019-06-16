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
    public class A30010Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A30010";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);
        List<SelectListItem> selItem_lbl_cmb1;
        List<SelectListItem> selItem_driver;
        #endregion

        // GET: A30010
        public ActionResult A30010()
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
                //selItem_lbl_cmb1.Add(new SelectListItem { Value = "dcName", Text = "DC Name", Selected = true });
                //selItem_lbl_cmb1.Add(new SelectListItem { Value = "tranCompName", Text = "Company" });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "dispatch_Id", Text = "Dispatch Id" });
            }

            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            ViewBag.selItem_statusId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.No, "", "N0", "");
            ViewBag.selItem_dcId = Models.CmbObjMD.selItem_dcId(ZhConfig.IsAddIndexZero.No, "", " and dcId in ('" + userData.dcId.ToString().Replace(",", "','") + "') ");
            ViewBag.selItem_Company = Models.CmbObjMD.selItem_tranCompId(ZhConfig.IsAddIndexZero.Yes, "", " and dcId in ('" + userData.dcId.ToString().Replace(",", "','") + "') ");

            if (selItem_driver == null)
            {
                selItem_driver = new List<SelectListItem>();

                #region sql select
                string strSql = " select t1.driverId value, (t2.userName+'('+t1.driverId+')') text from T10_driver t1 ";
                strSql += " inner join S10_users t2 on t2.sysUserId=t1.sysUserId ";
                #endregion
                DataTable tmpTable = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTable");
                if (tmpTable.Rows.Count > 0)
                {
                    tmpTable.PrimaryKey = new DataColumn[] { tmpTable.Columns["value"] };
                    #region Insert 請選擇
                    DataRow tmpRow = tmpTable.NewRow();
                    tmpRow["value"] = "";
                    tmpRow["text"] = "Select";
                    tmpTable.Rows.InsertAt(tmpRow, 0);
                    #endregion

                    for (int i = 0; i < tmpTable.Rows.Count; i++)
                    {
                        selItem_driver.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = tmpTable.Rows[i]["text"].ToString() });
                    }

                    selItem_driver[0].Selected = true;

                    selItem_lbl_cmb1.Add(new SelectListItem { Value = "dispatch_Id", Text = "Dispatch Id" });
                }
            }

            ViewBag.selItem_driver = selItem_driver;

            return View();
        }

        #region Control_GetGridJSON
        public ActionResult GetGridJSON(int page, int rows, string order, string lbl_cmb1, string value_cmb1, string value_dcId, string dispatch_id, string date)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                string tableName = "U_" + funcId;

                if (string.IsNullOrEmpty(order))
                {
                    order = " dcId, tranCompId ";
                }

                string strCond = " where 1=1 and taskStatus > 10 and taskStatus < 30 ";

                if (!string.IsNullOrEmpty(dispatch_id))
                {
                    strCond += " and dispatch_Id='" + dispatch_id + "' ";
                }

                if (!string.IsNullOrEmpty(date))
                {
                    strCond += " and delDate='" + date + "' ";
                }

                if (!string.IsNullOrEmpty(value_dcId))
                {
                    strCond += " and dcId='" + value_dcId + "' ";
                }
                else
                {
                    strCond += " and dcId in ('" + userData.dcId.Replace(",", "','") + "') ";
                }


                string colBtnClean = "";
                colBtnClean += "'<a href=\"javascript:void(0)\" onclick=\"btnClean(''' + rtrim(dispatch_Id) + ''')\" class=\"btn btn-danger\">Cancel</a>'";

                string colBtnEdit = "'<a href=\"javascript:void(0)\" onclick=\"btnEdit(''' + rtrim(dispatch_Id) + ''')\" class=\"btn btn-primary\">Edit</a>'";

                strSql.Clear();
                strSql.AppendLine(" select *, " + colBtnClean + " btnClean, " + colBtnEdit + " btnEdit from ( SELECT  ROW_NUMBER() OVER (ORDER BY " + order + ") AS RowNum, * FROM ");
                strSql.AppendLine(tableName + strCond);
                strSql.AppendLine(" )  AS t1 ");
                strSql.AppendLine(" where (t1.RowNum > " + ((rows * page) - rows) + " and t1.RowNum <= " + (rows * page) + ") ");

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


        #region 取得dispath資料
        [HttpPost]
        public ActionResult ActDispatchData(Models.Base.MD_T30_carshdu actRow)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                actRow.tranCompId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.tranCompId);
                actRow.trailerId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.trailerId);
                actRow.carId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.carId);
                actRow.driverId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.driverId);
                actRow.driverId2 = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.driverId2);

                if (string.IsNullOrEmpty(actRow.trailerId.ToString())) actRow.trailerId = "";

                DataTable T30_carshdu = SqlTool.GetDataTable(" select * from T30_carshdu where dispatch_id='" + actRow.dispatch_id + "' ", "T30_carshdu");

                DateTime dtUtc = DateTime.UtcNow;

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        DataTable userInfo = null;
                        string sysUserId_run = "";
                        if (T30_carshdu.Rows[0]["driverSeq"].ToString() == "1")
                        {
                            //userInfo = SqlTool2.GetDataTable(cn, " select * from S10_users where userId='" + actRow.driverId + "' ", "userInfo");
                            sysUserId_run = T30_carshdu.Rows[0]["sysUserId"].ToString();
                        }
                        else
                        {
                            //userInfo = SqlTool2.GetDataTable(cn, " select * from S10_users where userId='" + actRow.driverId2 + "' ", "userInfo");
                            sysUserId_run = T30_carshdu.Rows[0]["sysUserId2"].ToString();
                        }


                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param = {
                            new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.dispatch_id),
                            new SqlParameter("tranCompId", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.tranCompId),
                            new SqlParameter("carId", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.carId),
                            new SqlParameter("trailerId", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.trailerId),
                            new SqlParameter("driverId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.driverId),
                            new SqlParameter("sysUserId_run", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, sysUserId_run),
                            new SqlParameter("driverId2", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.driverId2),
                            new SqlParameter("contactTel", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  T30_carshdu.Rows[0]["contactTel"].ToString()),
                            new SqlParameter("contactTel2", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  T30_carshdu.Rows[0]["contactTel2"].ToString()),
                            new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  userData.sysUserId),
                            new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc),
                        };
                        #endregion

                        strSql.Clear();
                        if (int.Parse(T30_carshdu.Rows[0]["driverCnt"].ToString()) > 1)
                        {
                            strSql.AppendLine(" UPDATE T30_carshdu set tranCompId=@tranCompId, tranCompId2=@tranCompId, carId=@carId, carId2=@carId, trailerId=@trailerId, trailerId2=@trailerId, driverId=@driverId, driverId2=@driverId2, sysUserId_run=@sysUserId_run, contactTel2=@contactTel2, actUser=@actUser, actTime=@actTime where dispatch_id=@dispatch_id ");
                        }
                        else
                        {
                            strSql.AppendLine(" UPDATE T30_carshdu set tranCompId=@tranCompId, carId=@carId, trailerId=@trailerId, driverId=@driverId, sysUserId_run=@sysUserId_run, contactTel=@contactTel, actUser=@actUser, actTime=@actTime where dispatch_id=@dispatch_id ");
                        }
                        errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);
                        cn.Close();

                    }
                    using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                    {
                        cn1.Open();
                        string driverName = SqlTool.GetOneDataValue(ZhConfig.GlobalSystemVar.StrConnection1, " select userName from S10_users where userId='" + actRow.driverId + "' ").ToString();
                        string driver2Name = SqlTool.GetOneDataValue(ZhConfig.GlobalSystemVar.StrConnection1, " select userName from S10_users where userId='" + actRow.driverId2 + "' ").ToString();
                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param = {
                            new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.dispatch_id),
                            new SqlParameter("truck_company_code", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.tranCompId),
                            new SqlParameter("truck_no", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.carId),
                            new SqlParameter("trailer_no", SqlDbType.NVarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.trailerId),
                            new SqlParameter("driver_name", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, driverName),
                            new SqlParameter("sec_driver_name", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, driver2Name),
                            new SqlParameter("epod_update_time_utc", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dtUtc)
                        };
                        #endregion

                        strSql.Clear();
                        if (int.Parse(T30_carshdu.Rows[0]["driverCnt"].ToString()) > 1)
                        {
                            strSql.AppendLine(" UPDATE dispatch set truck_Company_code=@truck_company_code, truck_no=@truck_no, trailer_no=@trailer_no, driver_name=@driver_name, sec_driver_name=@sec_driver_name, epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id ");
                        }
                        else
                        {
                            strSql.AppendLine(" UPDATE dispatch set truck_Company_code=@truck_company_code, truck_no=@truck_no, trailer_no=@trailer_no, driver_name=@driver_name, epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id ");
                        }
                        errStr = SqlTool2.ExecuteNonQuery(cn1, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);

                        cn1.Close();
                    }
                    scope.Complete(); //當執行這段SQL資料才會insert
                }

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

        #region 取得dispath資料
        [HttpPost]
        public ActionResult GetDispatchData(string dispatch_id)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                strSql.Clear();
                strSql.AppendLine(" SELECT t2.statusName'taskStatus',t1.* from T30_carshdu t1  ");
                strSql.AppendLine(" inner join S00_statusId t2 on t2.statusType=t1.statusType and t2.statusId=t1.taskStatus ");
                strSql.AppendLine(" WHERE t1.dispatch_id='" + dispatch_id + "' ");
                DataTable tbl_QueryData1 = SqlTool.GetDataTable(strSql.ToString(), "tbl_QueryData1");

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

                jo.Add("dispatch", ja);
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
        public ActionResult DeleteSingle(string pk)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            operStatusId = "50";
            DateTime dcUtc = DateTime.UtcNow;
            JObject jo = new JObject();
            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();


                        #region 刪除 Server端的資料
                        strSql.Clear();
                        strSql.Append("Update T30_carinv set taskStatus='90', actUser=@actUser, actTime=@actTime where dcShip=(select dcShip from T30_carshdu where dispatch_id=@dispatch_id) ");
                        strSql.Append("Update T30_carshdu set taskStatus='90', actUser=@actUser, actTime=@actTime where dispatch_id=@dispatch_id ");

                        SqlParameter[] param ={
                        new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pk),
                        new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId.ToString()),
                        new SqlParameter("actTime", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dcUtc)
                };

                        #endregion

                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);


                        strSql.Clear();
                        using (System.Transactions.TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                        {
                            using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                            {
                                cn1.Open();

                                strSql.Clear();
                                strSql.AppendLine(" Update dnmain set bol_no=NULL, epod_update_time_utc=@epod_update_time_utc where bol_no in (select bol_no from bol where dispatch_id=@dispatch_id) ");
                                strSql.AppendLine(" Update bol set dispatch_id='', status='90', epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id "); //ship_order='', new_ship_order='', 20180820 移除 By.Ray
                                strSql.AppendLine(" Update dispatch set status='90', epod_update_time_utc=@epod_update_time_utc where dispatch_id=@dispatch_id "); //del_flag='Y', 20180820 移除 By.Ray

                                SqlParameter[] param2 ={
                        new SqlParameter("dispatch_id", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pk),
                        new SqlParameter("epod_update_time_utc", SqlDbType.DateTime, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dcUtc)
                };

                                errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn1, strSql.ToString(), param2);
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

            #region operLog 當使用者登入的時間逾時，便不在記錄log
            if (userData != null)
            {
                DataTable tbl_OperLog = userData.Get_tbl_operLogV1();
                DataRow operDr = tbl_OperLog.NewRow();
                operDr["actSerial"] = userData.actSerial;
                operDr["logDate"] = dcUtc;
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