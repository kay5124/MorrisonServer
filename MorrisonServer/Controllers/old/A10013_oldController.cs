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
    public class A10013_oldController : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A10013_old";
        StringBuilder strSql = new StringBuilder(200);

        List<SelectListItem> selItem_lbl_cmb1;

        #endregion

        // GET: A10013_old
        public ActionResult A10013_old()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            ViewBag.selItem_statusId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.Yes, "", "PS", "");

            ViewBag.selItem_appGroupId = Models.CmbObjMD.selItem_appGroupId(ZhConfig.IsAddIndexZero.No, "Select");

            if (selItem_lbl_cmb1 == null)
            {
                selItem_lbl_cmb1 = new List<SelectListItem>();
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "notiId", Text = "訊息代碼" });
                selItem_lbl_cmb1.Add(new SelectListItem { Value = "title", Text = "推播標題" });
            }

            ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            return View();
        }


        #region 提供 datagrid資料
        public ActionResult GetGridJSON(int page, int rows, string sort, string order, string lbl_cmb1, string value_cmb1, string value_statusId)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                #region Gen strSql by ROW_NUMBER
                string tableName = "U_" + funcId;
                string sortId = "notiId";

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
                string strCond = " where 1=1 and statusId2 in ('10', '20') ";

                if (!string.IsNullOrEmpty(lbl_cmb1) && !string.IsNullOrEmpty(value_cmb1))
                {
                    strCond += " and " + lbl_cmb1 + " like '%" + value_cmb1 + "%'";
                }
                if (!string.IsNullOrEmpty(value_statusId))
                {
                    strCond += " and statusId='" + value_statusId + "'";
                }
                #endregion

                strSql.Append("SELECT  * FROM (SELECT  ROW_NUMBER() OVER (ORDER BY " + sortId + ") AS RowNum, * FROM " + tableName + strCond + ") AS NewTable ");
                strSql.Append(" WHERE RowNum >= " + ((page - 1) * rows + 1).ToString() + " AND RowNum <=" + page * rows);

                #endregion

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), tableName);

                int totalCount = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue("select count(*) from " + tableName + strCond));

                DataTable tbl_userGroups = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, "select * from U_" + funcId + "v2", tableName);

                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    DataRow[] tmpRows = tbl_userGroups.Select("notiId='" + dr["notiId"].ToString() + "'");

                    string groupsName = "";
                    string groups = "";
                    foreach (DataRow dr2 in tmpRows)
                    {
                        groupsName += dr2["appGroupName"].ToString() + ",";
                        groups += dr2["appGroupId"].ToString() + ",";

                    }

                    if (groupsName != "")
                    {
                        groupsName = groupsName.Substring(0, groupsName.Length - 1);
                        groups = groups.Substring(0, groups.Length - 1);

                    }

                    dr["appGroupNames"] = groupsName;
                    dr["appGroupIds"] = groups;

                }

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

        public ActionResult ActSingle(Models.Base.MD_App_noti actRow)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            StringBuilder tmpStrSql = new StringBuilder();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();


                #region ACall_checkIsDBNull
                actRow.notiId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.notiId);
                actRow.title = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.title);
                actRow.msg = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.msg);
                actRow.msgHtml = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.msgHtml);
                actRow.pushTime = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.pushTime);
                actRow.statusType = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.statusType);
                actRow.statusId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.isPushAll);
                actRow.isPushAll = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.isPushAll);
                actRow.soundId = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.soundId);
                actRow.param = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.param);
                actRow.appGroups = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.appGroups);
                actRow.appGroupNames = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.appGroupNames);
                actRow.backcolor = ZhConfig.ZhIniObj.ACall_checkIsDBNull(actRow.backcolor);
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
                                new SqlParameter("notiId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, actRow.notiId),
                                new SqlParameter("title", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.title),
                                new SqlParameter("msg", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.msg),
                                new SqlParameter("msgHtml", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, HttpUtility.UrlDecode(actRow.msgHtml.ToString())),
                                new SqlParameter("isPushAll", SqlDbType.Bit, 1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.isPushAll),
                                new SqlParameter("soundId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.soundId),
                                new SqlParameter("param", SqlDbType.NVarChar, -1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.param),
                                new SqlParameter("backcolor", SqlDbType.VarChar, 16, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.backcolor),
                                new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,userData.sysUserId),//actRow.creatUser
                                new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId)//actRow.actUser
                        };

                        #endregion
                        strSql.Clear();
                        switch (actRow.RowStatus.ToString())
                        {
                            case "A"://sysUserId,@sysUserId,,actUser,actTime ,@actUser,getdate()
                                strSql.AppendLine("insert into App_noti (title,msg,msgHtml,isPushAll,param,creatUser,creatTime) values (@title,@msg,@msgHtml,@isPushAll,@param,@creatUser,getdate())");
                                strSql.AppendLine("SELECT @notiId = SCOPE_IDENTITY()");
                                actRow.creatUser = userData.userName;
                                actRow.creatTime = DateTime.Now;
                                operStatusId = "30";
                                break;
                            case "M"://sysUserId=@sysUserId,
                                strSql.AppendLine("update App_noti set title=@title,msg=@msg,msgHtml=@msgHtml,isPushAll=@isPushAll,param=@param,actUser=@actUser,actTime=getdate() where notiId=@notiId");
                                actRow.actUser = userData.userName;
                                actRow.actTime = DateTime.Now;
                                operStatusId = "40";
                                break;
                        }

                        errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                        if (errStr != "") throw new Exception(errStr);
                        actRow.notiId = param[0].Value;

                        tmpStrSql.AppendLine(strSql.ToString());
                        #endregion

                        #region act S10_userGroups
                        strSql.Clear();
                        if (actRow.RowStatus.ToString() == "M")
                        {//若是修改就先刪除再整個新增
                            strSql.AppendLine("delete App_notiGroups where notiId='" + actRow.notiId + "'");
                        }

                        if (actRow.appGroups != null && actRow.appGroups.ToString() != "")
                        {
                            string[] groups = actRow.appGroups.ToString().Split(',');

                            foreach (string gId in groups)
                            {
                                if (gId == "") continue;

                                strSql.AppendLine("insert into  App_notiGroups (notiId,appGroupId) values ('" + actRow.notiId.ToString() + "','" + gId + "') ");
                            }


                            errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString());
                            if (errStr != "") throw new Exception(errStr);
                        }


                        tmpStrSql.AppendLine(strSql.ToString());
                        strSql.Clear();
                        strSql.AppendLine(tmpStrSql.ToString());
                        #endregion

                    }
                    scope.Complete();
                }

                #region return Info
              
                JArray ja = new JArray();

                var itemObject = new JObject();

                itemObject.Add("notiId", actRow.notiId.ToString());
                itemObject.Add("msgHtml", HttpUtility.UrlDecode(actRow.msgHtml.ToString()));

                if (actRow.RowStatus.ToString() == "M")
                {
                    itemObject.Add("actUser", actRow.actUser.ToString());
                    itemObject.Add("actTime", actRow.actTime.ToString());
                }
                else //A
                {
                    itemObject.Add("creatUser", actRow.creatUser.ToString());
                    itemObject.Add("creatTime", actRow.creatTime.ToString());
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
                object pk_notiId = pk[0];

                #region 刪除 後端資料庫
                strSql.Append("Update App_noti set statusId2='30' where notiId=@pk_notiId");

                #region 設置 要傳入的 SqlParameter 資料

                SqlParameter[] param = {
                    new SqlParameter("pk_notiId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "sysUserId", DataRowVersion.Original,pk_notiId)};

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


        #region 發送推播
        public ActionResult ActPush(string pks)
        {
            JObject jo = new JObject();
            operStatusId = "40";
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                string[] pk = pks.Split('/');
                object pk_notiId = pk[0];

                strSql.AppendLine(" select * from App_noti where notiId='" + pks + "' ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count > 0)
                {
                    strSql.Clear();
                    if (tbl_QueryData1.Rows[0]["isPushAll"].ToString() != "True")
                    {
                        strSql.Append(" select sysUserId, '" + pks + "' As notiId from App_userGroups where appGroupId in ( ");
                        strSql.Append(" select appGroupId from App_notiGroups where notiId='" + pks + "' ");
                        strSql.Append(" )group by sysUserId ");
                    }
                    else
                    {
                        strSql.Append(" select sysUserId, '" + pks + "' As notiId from S10_users where appSysId='10' and statusId='10' ");
                    }
                    DataTable tbl_QueryData2 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData2");

                    #region 批次寫入訊息匣
                    using (TransactionScope scope = new TransactionScope())
                    {
                        using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                        {
                            cn.Open();

                            using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(cn))
                            {
                                sqlBulkCopy.DestinationTableName = "dbo.App_userNoti";
                                sqlBulkCopy.WriteToServer(tbl_QueryData2);
                            }

                            #region 更改推播訊息狀態
                            strSql.Clear();
                            strSql.AppendLine(" Update App_noti set pushTime=getDate(), statusId='10' where notiId='" + pk_notiId + "' ");
                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString());
                            #endregion

                        }
                        scope.Complete();
                    }
                    #endregion

                    #region 準備推播資料
                    strSql.Clear();
                    if (tbl_QueryData1.Rows[0]["isPushAll"].ToString() != "True")
                    {
                        strSql.Append(" select t1.sysUserId, t1.platform, t1.RegId, t1.TokenId from App_userDevice t1 ");
                        strSql.Append(" where t1.isNotice=1 and t1.isUse=1 and ((t1.RegId <> '' and t1.RegId is not null) or (t1.TokenId <> '' and t1.TokenId is not null))  ");
                        strSql.Append(" and t1.sysUserId in (select sysUserId from App_userGroups where appGroupId in ( ");
                        strSql.Append(" select appGroupId from App_notiGroups where notiId='" + pks + "' ");
                        strSql.Append(" )group by sysUserId ) ");
            
                    }
                    else
                    {
                        strSql.Append(" select t1.sysUserId, t1.platform, t1.RegId, t1.TokenId from App_userDevice t1 ");
                        strSql.Append(" where t1.isNotice=1 and t1.isUse=1 and ((t1.RegId <> '' and t1.RegId is not null) or (t1.TokenId <> '' and t1.TokenId is not null)) ");
                        strSql.Append(" and t1.sysUserId in (select sysUserId from S10_users where appSysId='10' and statusId='10') ");
                    }

                    tbl_QueryData2 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData2");

                    string title = tbl_QueryData1.Rows[0]["title"].ToString();
                    string msg = tbl_QueryData1.Rows[0]["msg"].ToString();
                    string param = tbl_QueryData1.Rows[0]["param"].ToString();
                    string soundId = "";
                    string count = "";
                    string deviceIds = "";

                    Models.NotiFactoryMD.Push push = new Models.NotiFactoryMD.Push();

                    #region 識別推播的平台
                    #region Android
                    DataRow[] drs = tbl_QueryData2.Select("platform='Android'");
                    if(drs.Length != 0)
                    {
                        #region 組推播字串
                        deviceIds = "";

                        foreach (DataRow dr in drs)
                        {
                            deviceIds += "\"" + dr["RegId"].ToString() + "\",";
                        }
                        #endregion

                        if (deviceIds != "") deviceIds = deviceIds.Substring(0, deviceIds.Length - 1);
                        errStr = push.pushAndroid(title, msg, soundId, param, count, deviceIds);
                    }
                    #endregion


                    #region iOS
                    drs = tbl_QueryData2.Select("platform='iOS'");
                    if (drs.Length != 0)
                    {
                       
                    }
                    #endregion
                    #endregion

                    #endregion
                }
                else
                {
                    throw new Exception("推播失敗");
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