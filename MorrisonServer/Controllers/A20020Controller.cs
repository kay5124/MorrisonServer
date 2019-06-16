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
using NPOI;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System.IO;

namespace MorrisonServer.Controllers
{
    public class A20020Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A20020";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);
        List<SelectListItem> selItem_lbl_cmb1;
        #endregion

        // GET: A20020
        public ActionResult A20020()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"] == "")
                ViewBag.ServerName = "";
            else
                ViewBag.ServerName = "/" + System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"];


            ViewBag.selItem_statusId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.Yes, "", "N0", "and statusId <> '30' ");

            ViewBag.selItem_groupId = Models.CmbObjMD.selItem_groupId(ZhConfig.IsAddIndexZero.No, "Select");

            ViewBag.selItem_tranCompId = Models.CmbObjMD.selItem_tranCompId(ZhConfig.IsAddIndexZero.Yes, "", " and dcId in ('" + userData.dcId.ToString().Replace(",", "','") + "') ");

            ViewBag.selItem_dcId = Models.CmbObjMD.selItem_dcId(ZhConfig.IsAddIndexZero.No, "", " and dcId in ('" + userData.dcId.ToString().Replace(",", "','") + "') ");

            //ViewBag.selItem_value_appSysId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.Yes, "", "DP", "");

            if (selItem_lbl_cmb1 == null)
            {
                selItem_lbl_cmb1 = new List<SelectListItem>();
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "userId", Text = "User Id" });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "userName", Text = "User Name" });
            }

            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            return View();
        }

        #region Control_GetGridJSON
        public ActionResult GetGridJSON(int page, int rows, string order, string lbl_cmb1, string value_cmb1, string dcId)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                #region Gen strSql by ROW_NUMBER
                string tableName = "U_A20020";

                if (string.IsNullOrEmpty(order))
                {
                    order = " sysUserId ";
                }

                #region strCond
                string strCond = " where 1=1 and statusId in ('10','20') and appSysId='10' ";

                if (!string.IsNullOrEmpty(lbl_cmb1) && !string.IsNullOrEmpty(value_cmb1))
                {
                    strCond += " and " + lbl_cmb1 + " like '%" + value_cmb1 + "%'";
                }
                if (!string.IsNullOrEmpty(dcId))
                {
                    strCond += " and dcId='" + dcId + "'";
                }
                #endregion


                string colBtnEdit = "";
                colBtnEdit += "'<a href=\"javascript:void(0)\" onclick=\"btnEdit(''' + rtrim(sysUserId) + ''')\" class=\"btn btn-primary\">Edit</a>'";

                string colBtnDelete = "";
                colBtnDelete += "'<a href=\"javascript:void(0)\" onclick=\"btnDelete(''' + rtrim(sysUserId) + ''')\" class=\"btn btn-danger\">Delete</a>'";

                strSql.Clear();
                strSql.AppendLine(" select *," + colBtnEdit + " btnEdit," + colBtnDelete + " btnDelete from ( SELECT  ROW_NUMBER() OVER (ORDER BY " + order + ") AS RowNum, * FROM ");
                strSql.AppendLine(tableName + strCond);
                strSql.AppendLine(" )  AS t1 ");
                strSql.AppendLine(" where (t1.RowNum > " + ((rows * page) - rows) + " and t1.RowNum <= " + (rows * page) + ") ");

                #endregion

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), tableName);

                int totalCount = tbl_QueryData1.Rows.Count;

                int totalPage = (int)Math.Ceiling((double)totalCount / (double)rows);

                //DataTable tbl_userGroups = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, "select * from U_S01050v2", tableName);

                //foreach (DataRow dr in tbl_QueryData1.Rows)
                //{
                //    DataRow[] tmpRows = tbl_userGroups.Select("sysUserId='" + dr["sysUserId"].ToString() + "'");

                //    string groupsName = "";
                //    string groups = "";
                //    foreach (DataRow dr2 in tmpRows)
                //    {
                //        groupsName += dr2["groupName"].ToString() + ",";
                //        groups += dr2["groupId"].ToString() + ",";

                //    }

                //    if (groupsName != "")
                //    {
                //        groupsName = groupsName.Substring(0, groupsName.Length - 1);
                //        groups = groups.Substring(0, groups.Length - 1);

                //    }

                //    dr["groupsName"] = groupsName;
                //    dr["groups"] = groups;

                //}

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
                ////operDr["strSql"] = strSql;

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
                strSql.AppendLine("  SELECT CONVERT(VARCHAR(100),t2.expDate,120)'expDate',* from S10_users t1 inner join T10_driver t2 on t2.sysUserId=t1.sysUserId where t1.sysUserId='" + pk + "' ");
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
                strSql.AppendLine(" select tranCompId value, tranCompName text from T10_tranComp where tranCompId in (select tranCompId from T10_dcTranComp where dcId='" + pk + "')  ");
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

        public ActionResult ActSingle(Models.Base.MD_A20040 actRow)//接收單筆資料方式
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                actRow.sysUserId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.sysUserId);
                actRow.dcId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.dcId);
                actRow.tranCompId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.tranCompId);
                actRow.countryCode = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.countryCode);
                actRow.userName = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userName);
                actRow.userId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userId);
                actRow.password = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.password);
                actRow.email = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.email);
                actRow.license = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.license);
                actRow.expDate = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.expDate);
                actRow.statusId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusId);
                actRow.memo = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.memo);
                actRow.contactTel = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.contactTel);
                actRow.contactTel2 = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.contactTel2);

                DateTime dtNow = DateTime.Now;

                string tranCompCount = SqlTool.GetOneDataValue(" SELECT count(*) from T10_dcTranComp where tranCompId='" + actRow.tranCompId + "' ").ToString();
                if (tranCompCount == "0") throw new Exception(" This truck company code:" + actRow.tranCompId + " is not belong " + actRow.dcId + " ");

                //Boolean isExist;
                //if (!string.IsNullOrEmpty(actRow.password.ToString()))
                //{
                //    isExist = SqlTool.GetOneDataValue(" select * from S10_users t1 inner join T10_driver t2 on t2.driverId=t1.userId where t1.userId='" + actRow.userId + "' and t1.password='" + actRow.password + "' ").ToString() == "1" ? true : false;
                //    if (isExist == false) throw new Exception(" Please check password. ");
                //}

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        #region 新增
                        object sysUserId = null;
                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param = {
                                            new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.sysUserId),
                                            new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                                            new SqlParameter("userId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userId),
                                            new SqlParameter("userName", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userName),
                                            new SqlParameter("password", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.password),
                                            new SqlParameter("contactTel", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.contactTel),
                                            new SqlParameter("contactTel2", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.contactTel2),
                                            new SqlParameter("countryCode", SqlDbType.SmallInt, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  int.Parse(actRow.countryCode.ToString())),
                                            new SqlParameter("email", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.email),
                                            new SqlParameter("statusType", SqlDbType.Char,2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "N0"),
                                            new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                                            new SqlParameter("deviceLimit", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, 1),
                                            new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId),
                                            new SqlParameter("memo", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.memo),
                                        };
                        #endregion

                        strSql.Clear();
                        //strSql.AppendLine(" INSERT INTO S10_users (appSysId,userId,userName,password,countryCode,email,statusType,statusId,creatUser,creatTime,deviceLimit,memo) VALUES (@appSysId,@userId,@userName,@password,@countryCode,@email,@statusType,@statusId,@creatUser,GETUTCDATE(),@deviceLimit,@memo) ");
                        //strSql.AppendLine(" SELECT @sysUserId = SCOPE_IDENTITY() ");
                        #region switch A & M
                        switch (actRow.RowStatus.ToString())
                        {
                            case "A":
                                strSql.AppendLine(" INSERT INTO S10_users (appSysId,userId,userName,password,countryCode,email,statusType,statusId,creatUser,creatTime,deviceLimit,memo,contactTel,contactTel2) VALUES (@appSysId,@userId,@userName,@password,@countryCode,@email,@statusType,@statusId,@creatUser,GETUTCDATE(),@deviceLimit,@memo,@contactTel,@contactTel2) ");
                                strSql.AppendLine(" SELECT @sysUserId = SCOPE_IDENTITY() ");
                                operStatusId = "30";
                                break;
                            case "M":
                                strSql.AppendLine(" update S10_users set userId=@userId,userName=@userName,countryCode=@countryCode,email=@email,statusType=@statusType,statusId=@statusId,actUser=@creatUser,actTime=GETUTCDATE(),deviceLimit=@deviceLimit,memo=@memo,contactTel=@contactTel,contactTel2=@contactTel2 where sysUserid=@sysUserId");
                                strSql.AppendLine(" SELECT @sysUserId = SCOPE_IDENTITY() ");
                                operStatusId = "40";
                                break;
                        }
                        #endregion
                        errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);
                        sysUserId = param[0].Value;
                        if (string.IsNullOrEmpty(sysUserId.ToString())) sysUserId = actRow.sysUserId;

                        #region 設置 要傳入的 SqlParameter 資料
                        SqlParameter[] param2 = {
                                            new SqlParameter("dcId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.dcId),
                                            new SqlParameter("tranCompId", SqlDbType.VarChar,20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.tranCompId),
                                            new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, sysUserId),
                                            new SqlParameter("driverId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userId),
                                            new SqlParameter("statusType", SqlDbType.Char,2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "N0"),
                                            new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                                            new SqlParameter("licenseId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.license),
                                            new SqlParameter("expDate", SqlDbType.Date, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.expDate),
                                            new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId),
                                        };
                        #endregion

                        strSql.Clear();

                        #region switch A & M
                        switch (actRow.RowStatus.ToString())
                        {
                            case "A":
                                strSql.Append("insert into T10_driver (dcId,tranCompId,sysUserId,driverId,licenseId,expDate,statusType,statusId,creatUser,creatTime) VALUES (@dcId,@tranCompId,@sysUserId,@driverId,@licenseId,@expDate,@statusType,@statusId,@creatUser,GETUTCDATE()) ");
                                operStatusId = "30";
                                break;
                            case "M":
                                strSql.Append("update T10_driver set dcId=@dcId,tranCompId=@tranCompId,driverId=@driverId,licenseId=@licenseId,expDate=@expDate,statusType=@statusType,statusId=@statusId,actUser=@creatUser,actTime=GETUTCDATE() where sysUserid=@sysUserId");
                                operStatusId = "40";
                                break;
                        }
                        #endregion
                        //strSql.Append(" INSERT INTO T10_driver (dcId,tranCompId,sysUserId,driverId,licenseId,expDate,statusType,statusId,creatUser,creatTime) VALUES (@dcId,@tranCompId,@sysUserId,@driverId,@licenseId,@expDate,@statusType,@statusId,@creatUser,GETUTCDATE()) ");
                        errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param2);
                        if (errStr != "") throw new Exception(errStr);
                        #endregion

                        cn.Close();

                    }
                    scope.Complete();
                }

                #region switch A & M
                //strSql.Clear();
                //switch (actRow.RowStatus.ToString())
                //{
                //    case "A":

                //        strSql.Append("insert into T10_dcTranComp (dcId,tranCompId,memo,statusType,statusId,creatUser) values (@dcId,@tranCompId,@memo,@statusType,@statusId,@creatUser)");
                //        operStatusId = "30";
                //        break;
                //    case "M":
                //        strSql.Append("update T10_dcTranComp set dcId=@dcId,tranCompId=@tranCompId,memo=@memo,statusType=@statusType,statusId=@statusId,actUser=@actUser,actTime=getdate() where dcId=@pk_dcId and tranCompId=@pk_tranCompId");
                //        operStatusId = "40";
                //        break;
                //}
                #endregion


                //errStr = SqlTool.ExecuteNonQuery(strSql.ToString(), param);

                //if (errStr != "") throw new Exception(errStr);

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
            JObject jo = new JObject();
            try
            {
                #region 更改狀態
                strSql.Clear();
                strSql.Append(" update S10_users set statusId='30' where sysUserId='" + pk + "' ");
                errStr = SqlTool.ExecuteNonQuery(strSql.ToString());
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