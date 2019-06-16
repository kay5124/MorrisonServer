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
    public class A10040_oldController : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A10040_old";
        StringBuilder strSql = new StringBuilder(200);

        List<SelectListItem> selItem_lbl_cmb1;

        #endregion

        // GET: A10040_old
        public ActionResult A10040_old()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            ViewBag.selItem_statusId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.Yes, "", "N0", "and statusId <> '30' ");

            if (selItem_lbl_cmb1 == null)
            {
                selItem_lbl_cmb1 = new List<SelectListItem>();
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "userId", Text = "User Id" });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "userName", Text = "User Name" });
            }

            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            return View();
        }


        #region 提供 datagrid資料
        public ActionResult GetGridJSON(int page, int rows, string sort, string order, string lbl_cmb1, string value_cmb1, string value_sysCorpId)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                #region Gen strSql by ROW_NUMBER
                string tableName = "U_" + funcId;
                string sortId = "sysUserId";

                if (!string.IsNullOrEmpty(sort))
                {
                    sortId = sort;
                }
                if (!string.IsNullOrEmpty(order))
                {
                    sortId = sort + " " + order;
                }
 
                
                //strSql.Remove(0, strSql.Length);

                #region strCond
                string strCond = " where 1=1 and statusId in ('10','20') and appSysId='10' ";

                if (!string.IsNullOrEmpty(lbl_cmb1) && !string.IsNullOrEmpty(value_cmb1))
                {
                    strCond += " and " + lbl_cmb1 + " like '%" + value_cmb1 + "%'";
                }
                if (!string.IsNullOrEmpty(value_sysCorpId) && Convert.ToInt32(value_sysCorpId) > 0)
                {
                    strCond += " and sysCorpId='" + value_sysCorpId + "'";
                }
                #endregion

                strSql.Append("SELECT  * FROM (SELECT  ROW_NUMBER() OVER (ORDER BY " + sortId + ") AS RowNum, * FROM " + tableName + strCond + ") AS NewTable ");
                strSql.Append(" WHERE RowNum >= " + ((page - 1) * rows + 1).ToString() + " AND RowNum <=" + page * rows);

                #endregion

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), tableName);

                int totalCount = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue("select count(*) from " + tableName + strCond));


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

                jo.Add("total", totalCount);
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


        #region Gen ActionResult ActSingle/DeleteSingle

        public ActionResult ActSingle(Models.Base.MD_S10_users actRow)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            StringBuilder tmpStrSql = new StringBuilder();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                #region 判斷啟用中 值,不可重複
                string sql = "";
                if (actRow.RowStatus.ToString() == "M")
                {
                    sql = " select count(*) from S10_users where userId='" + actRow.userId.ToString() + "' and statusId in ('10','20') and sysUserId <> '" + actRow.sysUserId.ToString() + "' ";
                }
                else
                {
                    sql = " select count(*) from S10_users where userId='" + actRow.userId.ToString() + "' and statusId in ('10','20') ";
                }
                int userCount = int.Parse(ZhClass.SqlTool.GetOneDataValue(sql).ToString());
                if (userCount > 0)
                {
                    throw new Exception("目前已有該帳號，請重新輸入。");
                }
                #endregion

                #region ACall_checkIsDBNull
                actRow.sysUserId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.sysUserId);
                actRow.userName = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userName);
                actRow.userId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.userId);
                actRow.password = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.password);
                actRow.email = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.email);
                actRow.contactTel = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.contactTel);
                actRow.contactTel2 = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.contactTel2);
                actRow.statusId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusId);
                actRow.deviceLimit = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.deviceLimit);
                actRow.chkUnSavePW = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.chkUnSavePW);
                actRow.memo = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.memo);
                actRow.creatUser = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.creatUser);
                actRow.actUser = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.actUser);
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
                                new SqlParameter("userName", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userName),
                                new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "00"),
                                new SqlParameter("userId", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userId),
                                new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.password.ToString()),
                                //new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ZhClass.DesClass.DESEncrypt3(actRow.password.ToString())),
                                new SqlParameter("email", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.email),
                                new SqlParameter("contactTel", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.contactTel),
                                new SqlParameter("contactTel2", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.contactTel2),
                                new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.statusId),
                                new SqlParameter("deviceLimit", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.deviceLimit),
                                new SqlParameter("memo", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.memo),
                                new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,userData.sysUserId),//actRow.creatUser
                                new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId),//actRow.actUser
                                new SqlParameter("pk_sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "sysUserId", DataRowVersion.Original,  actRow.sysUserId)};

                        #endregion
                        strSql.Clear();
                        switch (actRow.RowStatus.ToString())
                        {
                            case "A"://sysUserId,@sysUserId,,actUser,actTime ,@actUser,getdate()
                                strSql.AppendLine("insert into S10_users (userName,appSysId,userId,password,email,contactTel,contactTel2,statusId,deviceLimit,memo,creatUser,creatTime) values (@userName,@appSysId,@userId,@password,@email,@contactTel,@contactTel2,@statusId,@deviceLimit,@memo,@creatUser,getdate())");
                                strSql.AppendLine("SELECT @sysUserId = SCOPE_IDENTITY()");
                                actRow.creatUser = userData.userName;
                                actRow.creatTime = DateTime.Now;
                                operStatusId = "30";
                                break;
                            case "M"://sysUserId=@sysUserId,

                                if(actRow.chkUnSavePW.ToString() == "True")
                                {
                                    strSql.AppendLine("update S10_users set userName=@userName,userId=@userId,email=@email,contactTel=@contactTel,contactTel2=@contactTel2,statusId=@statusId,deviceLimit=@deviceLimit,memo=@memo,actUser=@actUser,actTime=getdate() where sysUserId=@pk_sysUserId");
                                }
                                else
                                {
                                    strSql.AppendLine("update S10_users set userName=@userName,userId=@userId,password=@password,email=@email,contactTel=@contactTel,contactTel2=@contactTel2,statusId=@statusId,deviceLimit=@deviceLimit,memo=@memo,actUser=@actUser,actTime=getdate() where sysUserId=@pk_sysUserId");
                                }
                                actRow.actUser = userData.userName;
                                actRow.actTime = DateTime.Now;
                                operStatusId = "40";
                                break;
                        }

                        errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);
                        actRow.sysUserId = param[0].Value;

                        tmpStrSql.AppendLine(strSql.ToString());
                        #endregion
                    }
                    scope.Complete();
                }

                #region return Info
              
                JArray ja = new JArray();

                var itemObject = new JObject();

                itemObject.Add("sysUserId", actRow.sysUserId.ToString());

                if (actRow.RowStatus.ToString() == "M")
                {
                    itemObject.Add("actUser", actRow.actUser.ToString());
                    itemObject.Add("actTime", actRow.actTime.ToString());
                }
                else //A
                {
                    itemObject.Add("creatUser", actRow.creatUser.ToString());
                    itemObject.Add("creatTime", actRow.creatTime.ToString());
                    itemObject.Add("appSysName", ZhClass.SqlTool.GetOneDataValue("select statusName from S00_statusId where statusType='DP' and statusId='" + actRow.appSysId.ToString() + "'").ToString());
                }

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
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }

        public ActionResult DeleteSingle(string pks)
        {
            JObject jo = new JObject();
            operStatusId = "50";
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                string[] pk = pks.Split('/');
                object pk_sysUserId = pk[0];

                #region 刪除 後端資料庫

                strSql.Append("Update S10_users set statusId='30' where sysUserId=@pk_sysUserId");

                #region 設置 要傳入的 SqlParameter 資料

                SqlParameter[] param = {
                    new SqlParameter("pk_sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "sysUserId", DataRowVersion.Original,pk_sysUserId)};

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
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");

        }

        #endregion
    }
}