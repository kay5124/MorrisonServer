using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using ZhClass;

namespace MorrisonServer.Controllers
{
    public class A20010Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A20010";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);
        List<SelectListItem> selItem_lbl_cmb1;
        #endregion

        // GET: A20010
        public ActionResult A20010()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"] == "")
                ViewBag.ServerName = "";
            else
                ViewBag.ServerName = "/" + System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"];


            //if (selItem_lbl_cmb1 == null)
            //{
            //    selItem_lbl_cmb1 = new List<SelectListItem>();
            //    selItem_lbl_cmb1.Add(new SelectListItem { Value = "dcName", Text = "DC Name", Selected = true });
            //    selItem_lbl_cmb1.Add(new SelectListItem { Value = "tranCompName", Text = "Company" });
            //}

            //ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            ViewBag.selItem_dcId = Models.CmbObjMD.selItem_dcId(ZhConfig.IsAddIndexZero.No, "", " and dcId in ('" + userData.dcId.ToString().Replace(",", "','") + "') ");
            ViewBag.selItem_tranCompId = Models.CmbObjMD.selItem_tranCompId(ZhConfig.IsAddIndexZero.Yes, "", " and dcId in ('" + userData.dcId.ToString().Replace(",", "','") + "') ");
            ViewBag.selItem_statusId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.No, "", "N0", "");

            return View();
        }

        #region Control_GetGridJSON
        public ActionResult GetGridJSON(int page, int rows, string order, string dcId, string tranCompId)
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

                string strCond = " where 1=1 and statusId in ('10', '20') ";

                if (!string.IsNullOrEmpty(dcId))
                {
                    strCond += " and dcId='" + dcId + "' ";
                }

                if (!string.IsNullOrEmpty(tranCompId))
                {
                    strCond += " and tranCompId='" + tranCompId + "' ";
                    //strCond += " and dcId in ('" + userData.dcId.Replace(",", "','") + "') ";
                }


                string colBtnEdit = "";
                colBtnEdit += "'<a href=\"javascript:void(0)\" onclick=\"btnEdit(''' + rtrim(dcId) + ''',''' + rtrim(tranCompId) + ''')\" class=\"btn btn-primary\">Edit</a>'";

                string colBtnDelete = "";
                colBtnDelete += "'<a href=\"javascript:void(0)\" onclick=\"btnDelete(''' + rtrim(dcId) + ''',''' + rtrim(tranCompId) + ''')\" class=\"btn btn-danger\">Delete</a>'";

                strSql.Clear();
                strSql.AppendLine(" select *, " + colBtnEdit + " btnEdit, " + colBtnDelete + " btnDelete from ( SELECT  ROW_NUMBER() OVER (ORDER BY " + order + ") AS RowNum, * FROM ");
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


        #region Form_GetEditData
        public ActionResult GetEditData(string pk, string pk2)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                strSql.Clear();
                strSql.AppendLine("  select t1.*, t2.dcName, t3.tranCompName from T10_dcTranComp t1 with (nolock) ");
                strSql.AppendLine("  left join C10_dc t2 with (nolock) on t2.dcId=t1.dcId ");
                strSql.AppendLine("  left join T10_tranComp t3 with (nolock) on t3.tranCompId=t1.tranCompId ");
                strSql.AppendLine("  where t1.dcId='" + pk + "'  and t1.tranCompId='" + pk2 + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception("Data not found.");

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


        #region Form_GetEditData
        public ActionResult GetTranCompData_NotExist(string pk)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                //if(userData.dc)

                if (pk == "") throw new Exception("Data not found.");

                strSql.Clear();
                strSql.AppendLine(" select tranCompId value, tranCompName text from T10_tranComp where tranCompId not in (select tranCompId from T10_dcTranComp where dcId='" + pk + "')  ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception("Data not found.");

                JArray ja = new JArray();

                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    var itemObject = new JObject();
                    foreach (DataColumn dc in tbl_QueryData1.Columns)
                    {
                        if (dc.ToString() == "text") itemObject.Add(dc.ColumnName, "(" + dr[0] + ")" + dr[dc].ToString());
                        else itemObject.Add(dc.ColumnName, dr[dc].ToString());

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

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }
        #endregion


        #region Form_Act

        public ActionResult ActSingle(Models.Base.MD_T10_dcTranComp actRow)//接收單筆資料方式
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                actRow.dcId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dcId);
                actRow.tranCompId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.tranCompId);
                actRow.statusId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusId);
                actRow.memo = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.memo);

                jo.Add("step1", "actRow loaded");

                DateTime dtNow = DateTime.Now;

                #region 設置 要傳入的 SqlParameter 資料
                SqlParameter[] param = {
                            new SqlParameter("dcId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.dcId),
                            new SqlParameter("tranCompId", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.tranCompId),
                            new SqlParameter("memo", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.memo),
                            new SqlParameter("statusType", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "N0"),
                            new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.statusId),
                            new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed,userData.sysUserId),
                            new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed,userData.sysUserId),
                            new SqlParameter("pk_dcId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "dcId", DataRowVersion.Original, actRow.dcId),
                            new SqlParameter("pk_tranCompId", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "tranCompId", DataRowVersion.Original, actRow.tranCompId)};
                #endregion

                string companyName = SqlTool.GetOneDataValue(ZhConfig.GlobalSystemVar.StrConnection1, " select tranCompName from T10_tranComp where tranCompId='" + actRow.tranCompId + "' ").ToString();

                jo.Add("step2", "param loaded");

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        #region switch A & M
                        strSql.Clear();
                        switch (actRow.RowStatus.ToString())
                        {
                            case "A":

                                strSql.Append("insert into T10_dcTranComp (dcId,tranCompId,memo,statusType,statusId,creatUser) values (@dcId,@tranCompId,@memo,@statusType,@statusId,@creatUser)");
                                operStatusId = "30";
                                break;
                            case "M":
                                strSql.Append("update T10_dcTranComp set dcId=@dcId,tranCompId=@tranCompId,memo=@memo,statusType=@statusType,statusId=@statusId,actUser=@actUser,actTime=getdate() where dcId=@pk_dcId and tranCompId=@pk_tranCompId");
                                operStatusId = "40";
                                break;
                        }
                        #endregion
                        errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);

                        jo.Add("step3", "T10_dcTranComp insert or update finish");

                        cn.Close();
                    }
                    jo.Add("step3-1", "ready for pod truckcompany");
                    using (TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                    {
                        using (SqlConnection cn1 = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                        {
                            jo.Add("step3-1.4", ZhConfig.GlobalSystemVar.StrConnection2);
                            jo.Add("step3-1.5", "come in cn1");
                            cn1.Open();
                            jo.Add("step3-2", "pod connect correct");

                            strSql.Clear();
                            int count = int.Parse(SqlTool2.GetOneDataValue(cn1, " select count(*) from truckcompany where truck_company_code='" + actRow.tranCompId + "' and station_code='" + actRow.dcId + "' ").ToString());
                            jo.Add("step3-3", "truckcompany count :" + count);
                            if (count == 0)
                            {
                                strSql.Append("insert into truckcompany (truck_company_code,station_code,truck_company_name,create_time_utc) values ('" + actRow.tranCompId + "','" + actRow.dcId + "','" + companyName + "',getutcdate())");
                            }
                            else
                            {
                                strSql.Append("UPDATE truckcompany set del_flag='N',update_time_utc=getutcdate() where truck_company_code='" + actRow.tranCompId + "' and station_code='" + actRow.dcId + "' ");
                            }
                            errStr = SqlTool2.ExecuteNonQuery(cn1, strSql.ToString());
                            if (errStr != "") throw new Exception(errStr);

                            jo.Add("step4", "truckcompany insert or update finish");

                            cn1.Close();
                        }
                        scopeNew.Complete();
                    }
                    scope.Complete();
                }

                jo.Add("step5", "all done");

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

        public ActionResult DeleteSingle(string pk, string pk2)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            operStatusId = "50";
            JObject jo = new JObject();
            try
            {
                #region 刪除 Server端的資料


                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        int count = int.Parse(SqlTool2.GetOneDataValue(cn, " select count(*) from T10_driver where tranCompId='" + pk2 + "' ").ToString());
                        if (count > 0) throw new Exception(" This company is being used. Can't delete. ");
                        SqlParameter[] param ={
                        new SqlParameter("dcId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pk),
                        new SqlParameter("tranCompId", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pk2) };

                        strSql.Clear();
                        //strSql.Append("Update S00_systemConfig set statusId2='30' where systemId=@systemId ");
                        strSql.Append("delete from T10_dcTranComp where dcId=@dcId and tranCompId=@tranCompId ");
                        errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);

                        cn.Close();
                    }
                    using (TransactionScope scopeNew = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeOption.RequiresNew))
                    {
                        using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection2))
                        {
                            cn.Open();

                            strSql.Clear();
                            strSql.Append("UPDATE truckcompany set del_flag='Y',update_time_utc=getutcdate() where truck_company_code='" + pk2 + "' and station_code='" + pk + "' ");
                            errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString());
                            if (errStr != "") throw new Exception(errStr);

                            cn.Close();
                        }
                        scopeNew.Complete();
                    }
                    scope.Complete();
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