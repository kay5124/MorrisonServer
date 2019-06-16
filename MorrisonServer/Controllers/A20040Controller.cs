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
    public class A20040Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A20040";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);
        List<SelectListItem> selItem_lbl_cmb1;
        #endregion

        // GET: A20040
        public ActionResult A20040()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"] == "")
                ViewBag.ServerName = "";
            else
                ViewBag.ServerName = "/" + System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"];


            ViewBag.selItem_statusId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.Yes, "", "N0", "and statusId <> '30' ");

            ViewBag.selItem_groupId = Models.CmbObjMD.selItem_groupId(ZhConfig.IsAddIndexZero.No, "Select");


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
                string tableName = "U_A20040";

                if (string.IsNullOrEmpty(order))
                {
                    order = " sysUserId ";
                }

                //strSql.Remove(0, strSql.Length);

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

                strSql.Clear();
                strSql.Append(" select * from ( SELECT  ROW_NUMBER() OVER (ORDER BY " + order + ") AS RowNum, * FROM ");
                strSql.Append(tableName + strCond);
                strSql.Append(" )  AS t1 ");
                strSql.Append(" where (t1.RowNum > " + ((rows * page) - rows) + " and t1.RowNum <= " + (rows * page) + ") ");

                #endregion

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), tableName);

                int totalCount = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(ZhConfig.GlobalSystemVar.StrConnection1, "select count(*) from " + tableName + strCond).ToString());

                int totalPage = (int)Math.Ceiling((double)totalCount / (double)rows);

                DataTable tbl_userGroups = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, "select * from U_S01050v2", tableName);

                foreach (DataRow dr in tbl_QueryData1.Rows)
                {
                    DataRow[] tmpRows = tbl_userGroups.Select("sysUserId='" + dr["sysUserId"].ToString() + "'");

                    string groupsName = "";
                    string groups = "";
                    foreach (DataRow dr2 in tmpRows)
                    {
                        groupsName += dr2["groupName"].ToString() + ",";
                        groups += dr2["groupId"].ToString() + ",";

                    }

                    if (groupsName != "")
                    {
                        groupsName = groupsName.Substring(0, groupsName.Length - 1);
                        groups = groups.Substring(0, groups.Length - 1);

                    }

                    dr["groupsName"] = groupsName;
                    dr["groups"] = groups;

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

        [HttpPost]
        public ActionResult InportFile(HttpPostedFileBase fileName)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                if (fileName == null) throw new Exception("Please select a file. ");

                DataTable tmpTbl = new DataTable();
                string fileLocation = Server.MapPath("~/Upload/") + Request.Files["fileName"].FileName;
                if (Request.Files["fileName"].ContentLength > 0)
                {
                    #region 判斷是否為excel檔
                    string extension = Path.GetExtension(fileName.FileName);
                    if (extension == ".xls" || extension == ".xlsx")
                    {
                        if (System.IO.File.Exists(fileLocation)) // 驗證檔案是否存在
                        {
                            System.IO.File.Delete(fileLocation);
                        }

                        string savePath = Server.MapPath("~/Upload/"); //儲存檔案位置 版本2
                        if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);

                        Request.Files["fileName"].SaveAs(fileLocation); // 存放檔案到伺服器上

                        #region 將excel匯入npoi
                        if (extension == ".xls")
                        {
                            #region excel 為 2003
                            HSSFWorkbook excel;
                            using (FileStream files = new FileStream(fileLocation, FileMode.Open, FileAccess.Read))
                            {
                                excel = new HSSFWorkbook(files);
                            }
                            ISheet sheet = excel.GetSheetAt(0);  // 在第0個活頁簿

                            #region 表頭
                            HSSFRow headerRow = sheet.GetRow(0) as HSSFRow;
                            for (int i = headerRow.FirstCellNum; i < headerRow.LastCellNum; i++)
                            {
                                if (headerRow.GetCell(i) != null)
                                {
                                    DataColumn cols = new DataColumn(headerRow.GetCell(i).StringCellValue);
                                    tmpTbl.Columns.Add(cols);
                                }
                            }
                            #endregion
                            #region 表身
                            for (int i = sheet.FirstRowNum + 1; i <= sheet.LastRowNum; i++)
                            {
                                HSSFRow row = sheet.GetRow(i) as HSSFRow;
                                DataRow rows = tmpTbl.NewRow();
                                for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                                {
                                    if (row.GetCell(j) != null)
                                    {
                                        rows[j] = row.GetCell(j).ToString();
                                    }
                                }
                                tmpTbl.Rows.Add(rows);
                            }
                            #endregion
                            #endregion
                        }
                        else
                        {
                            #region excel 2007
                            XSSFWorkbook excel;
                            using (FileStream files = new FileStream(fileLocation, FileMode.Open, FileAccess.Read))
                            {
                                excel = new XSSFWorkbook(files); // 將剛剛的Excel 讀取進入到工作簿中
                            }
                            ISheet sheet = excel.GetSheetAt(0);  // 在第0個活頁簿

                            #region 表頭
                            XSSFRow headerRow = sheet.GetRow(0) as XSSFRow;
                            for (int i = headerRow.FirstCellNum; i < headerRow.LastCellNum; i++)
                            {
                                if (headerRow.GetCell(i) != null)
                                {
                                    DataColumn cols = new DataColumn(headerRow.GetCell(i).StringCellValue);
                                    tmpTbl.Columns.Add(cols);
                                }
                            }
                            #endregion

                            #region 表身
                            for (int i = sheet.FirstRowNum + 1; i <= sheet.LastRowNum; i++)
                            {
                                XSSFRow row = sheet.GetRow(i) as XSSFRow;
                                DataRow rows = tmpTbl.NewRow();
                                for (int j = row.FirstCellNum; j < row.LastCellNum; j++)
                                {
                                    if (row.GetCell(j) != null)
                                    {
                                        rows[j] = row.GetCell(j).ToString();
                                    }
                                }
                                tmpTbl.Rows.Add(rows);
                            }
                            #endregion
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        errStr = "Please check your file is .xls or .xlsx";
                        //errStr = "請確認您的檔案是否為.xls或.xlsx";
                        if (errStr != "") throw new Exception(errStr);

                    }
                    #endregion
                    //如果資料都存入了datatable就把檔案刪除 避免占空間
                    System.IO.File.Delete(fileLocation);

                    #region 將npoi轉為json格式
                    if (tmpTbl.Columns.Count != 8) throw new Exception(" Please confirm that the format of Excel is correct. ");

                    string dcId = Request.Form.GetValues("dcId")[0];
                    
                    //JArray ja = new JArray();
                    using (TransactionScope scope = new TransactionScope())
                    {
                        using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                        {
                            cn.Open();
                            foreach (DataRow dr in tmpTbl.Rows)
                            {
                                #region ACall_checkIsDBNull 
                                dr[0] = ZhConfig.ZhIniObj.ACall_checkIsDBNull(dr[0]);
                                dr[1] = ZhConfig.ZhIniObj.ACall_checkIsDBNull(dr[1]);
                                dr[2] = ZhConfig.ZhIniObj.ACall_checkIsDBNull(dr[2]);
                                dr[3] = ZhConfig.ZhIniObj.ACall_checkIsDBNull(dr[3]);
                                dr[4] = ZhConfig.ZhIniObj.ACall_checkIsDBNull(dr[4]);
                                dr[5] = ZhConfig.ZhIniObj.ACall_checkIsDBNull(dr[5]);
                                dr[6] = ZhConfig.ZhIniObj.ACall_checkIsDBNull(dr[6]);
                                dr[7] = ZhConfig.ZhIniObj.ACall_checkIsDBNull(dr[7]);
                                #endregion

                                if (!string.IsNullOrEmpty(dr[0].ToString()) && !string.IsNullOrEmpty(dr[1].ToString()) &&
                                    !string.IsNullOrEmpty(dr[2].ToString()) && !string.IsNullOrEmpty(dr[4].ToString()))
                                {
                                    //drive id ||  drive name || drive phone country code || truck company code 都需有值才可RUN

                                    string userCount = SqlTool2.GetOneDataValue(cn, " select count(*) from S10_users where userId='" + dr[0].ToString() + "' ").ToString();
                                    string driverCount = SqlTool2.GetOneDataValue(cn, " select count(*) from T10_driver where driverId='" + dr[0].ToString() + "' ").ToString();
                                    //如果都沒有資料就新增
                                    if (userCount == "0" && driverCount == "0")
                                    {
                                        string tranCompCount = SqlTool2.GetOneDataValue(cn, " SELECT count(*) from T10_dcTranComp where tranCompId='" + dr[4] + "' ").ToString();
                                        if (tranCompCount == "0") throw new Exception(" this truck company code:" + dr[4] + " is not belong " + dcId + " ");
                                        object sysUserId = null;
                                        #region 設置 要傳入的 SqlParameter 資料
                                        SqlParameter[] param = {
                                            new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, sysUserId),
                                            new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                                            new SqlParameter("userId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dr[0]),
                                            new SqlParameter("userName", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dr[1]),
                                            new SqlParameter("password", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, ZhClass.DesClass.DESEncrypt3(dr[0].ToString())),
                                            new SqlParameter("countryCode", SqlDbType.SmallInt, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  int.Parse(dr[2].ToString())),
                                            new SqlParameter("email", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dr[3]),
                                            new SqlParameter("statusType", SqlDbType.Char,2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "N0"),
                                            new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                                            new SqlParameter("deviceLimit", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, 1),
                                            new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId),
                                        };
                                        #endregion

                                        strSql.Clear();
                                        //strSql.AppendLine(" INSERT INTO S10_users (appSysId,userId,userName,password,countryCode,email,statusType,statusId,creatUser,creatTime,deviceLimit) VALUES ");
                                        //strSql.AppendLine(" ('10','" + dr[0] + "','" + dr[1] + "','" + dr[0] + "'," + dr[2] + ",'"+dr[3]+"','N0','10',1,GETUTCDATE(),1) ");
                                        strSql.AppendLine(" INSERT INTO S10_users (appSysId,userId,userName,password,countryCode,email,statusType,statusId,creatUser,creatTime,deviceLimit,statusType2,statusId2) VALUES (@appSysId,@userId,@userName,@password,@countryCode,@email,@statusType,@statusId,@creatUser,GETUTCDATE(),@deviceLimit,NULL,NULL) ");
                                        strSql.AppendLine(" SELECT @sysUserId = SCOPE_IDENTITY() ");
                                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                                        if (errStr != "") throw new Exception(errStr);
                                        sysUserId = param[0].Value;

                                        #region 設置 要傳入的 SqlParameter 資料
                                        SqlParameter[] param2 = {
                                            new SqlParameter("dcId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dcId),
                                            new SqlParameter("tranCompId", SqlDbType.VarChar,20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dr[4]),
                                            new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, sysUserId),
                                            new SqlParameter("driverId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dr[0]),
                                            new SqlParameter("statusType", SqlDbType.Char,2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "N0"),
                                            new SqlParameter("statusId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                                            new SqlParameter("licenseId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dr[6]),
                                            new SqlParameter("expDate", SqlDbType.Date, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, dr[7]),
                                            new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId),
                                        };
                                        #endregion

                                        strSql.Clear();
                                        strSql.Append(" INSERT INTO T10_driver (dcId,tranCompId,sysUserId,driverId,licenseId,expDate,statusType,statusId,creatUser,creatTime) VALUES (@dcId,@tranCompId,@sysUserId,@driverId,@licenseId,@expDate,@statusType,@statusId,@creatUser,GETUTCDATE()) ");
                                        errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param2);
                                        if (errStr != "") throw new Exception(errStr);
                                    }
                                    else
                                    {
                                        throw new Exception(" Driver id:" + dr[0] + " has been used ");
                                    }
                                }
                                else
                                {
                                    throw new Exception(" Please confirm driver id, driver name, driver phone country code, truck company code field is empty. ");
                                }
                            }
                            cn.Close();
                        }
                        scope.Complete(); //當執行這段SQL資料才會insert
                    }
                    #endregion

                    jo.Add("count", tmpTbl.Rows.Count);
                    jo.Add("status", "OK");
                    //jo.Add("rows", ja);
                    //jo.Add("total", tmpTbl.Rows.Count);
                    return Content(JsonConvert.SerializeObject(jo), "application/json");
                }
                jo = new JObject();
                jo.Add("status", "Error");
                jo.Add("error", "File not received");
                return Content(JsonConvert.SerializeObject(jo), "application/json");
            }
            catch (Exception ex)
            {

                jo = new JObject();
                jo.Add("status", "Error");
                jo.Add("error", ex.Message);
                return Content(JsonConvert.SerializeObject(jo), "application/json");
            }
        }


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
                        itemObject.Add(dc.ColumnName, dr[dc].ToString());
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

                DateTime dtNow = DateTime.Now;

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        #region 新增

                        string userCount = SqlTool2.GetOneDataValue(cn, " select count(*) from S10_users where userId='" + actRow.userId + "' ").ToString();
                        string driverCount = SqlTool2.GetOneDataValue(cn, " select count(*) from T10_driver where driverId='" + actRow.userId + "' ").ToString();
                        //如果都沒有資料就新增
                        if (userCount == "0" && driverCount == "0")
                        {
                            string tranCompCount = SqlTool2.GetOneDataValue(cn, " SELECT count(*) from T10_dcTranComp where tranCompId='" + actRow.tranCompId + "' ").ToString();
                            if (tranCompCount == "0") throw new Exception(" This truck company code:" + actRow.tranCompId + " is not belong " + actRow.dcId + " ");
                            object sysUserId = null;
                            #region 設置 要傳入的 SqlParameter 資料
                            SqlParameter[] param = {
                                            new SqlParameter("sysUserId", SqlDbType.Int, 4, ParameterDirection.InputOutput, false, 0, 0, "", DataRowVersion.Proposed, sysUserId),
                                            new SqlParameter("appSysId", SqlDbType.Char, 2, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, "10"),
                                            new SqlParameter("userId", SqlDbType.NVarChar, 50, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userId),
                                            new SqlParameter("userName", SqlDbType.NVarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.userName),
                                            new SqlParameter("password", SqlDbType.VarChar, 255, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.password),
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
                            strSql.AppendLine(" INSERT INTO S10_users (appSysId,userId,userName,password,countryCode,email,statusType,statusId,creatUser,creatTime,deviceLimit,memo) VALUES (@appSysId,@userId,@userName,@password,@countryCode,@email,@statusType,@statusId,@creatUser,GETUTCDATE(),@deviceLimit,@memo) ");
                            strSql.AppendLine(" SELECT @sysUserId = SCOPE_IDENTITY() ");
                            errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                            if (errStr != "") throw new Exception(errStr);
                            sysUserId = param[0].Value;

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
                            strSql.Append(" INSERT INTO T10_driver (dcId,tranCompId,sysUserId,driverId,licenseId,expDate,statusType,statusId,creatUser,creatTime) VALUES (@dcId,@tranCompId,@sysUserId,@driverId,@licenseId,@expDate,@statusType,@statusId,@creatUser,GETUTCDATE()) ");
                            errStr = SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param2);
                            if (errStr != "") throw new Exception(errStr);
                        }
                        else
                        {
                            throw new Exception(" Driver id:" + actRow.userId + " has been userd ");
                        }
                        #endregion

                        cn.Close();

                    }
                    scope.Complete();
                }

                //#region switch A & M
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
                //#endregion


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

        public ActionResult DeleteSingle(string pk, string pk2)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            operStatusId = "50";
            JObject jo = new JObject();
            try
            {
                #region 刪除 Server端的資料
                strSql.Clear();
                //strSql.Append("Update S00_systemConfig set statusId2='30' where systemId=@systemId ");
                strSql.Append("delete from T10_dcTranComp where dcId=@dcId and tranCompId=@tranCompId ");

                SqlParameter[] param ={
                        new SqlParameter("dcId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pk),
                        new SqlParameter("tranCompId", SqlDbType.VarChar, 20, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, pk2) };

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