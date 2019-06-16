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
    public class A30020Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "A30020";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);
        List<SelectListItem> selItem_lbl_cmb1;
        #endregion

        // GET: A30020
        public ActionResult A30020()
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
            //    selItem_lbl_cmb1.Add(new SelectListItem { Value = "dispatch_Id", Text = "Dispatch Id" });
            //}

            //ViewBag.selItem_lbl_cmb1 = selItem_lbl_cmb1;

            //ViewBag.selItem_statusId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.No, "", "N0", "");
            ViewBag.selItem_statusId = Models.CmbObjMD.selItem_statusId(ZhConfig.IsAddIndexZero.Yes, "", "TS", "");
            ViewBag.selItem_dcId = Models.CmbObjMD.selItem_dcId(ZhConfig.IsAddIndexZero.No, "", " and dcId in ('" + userData.dcId.ToString().Replace(",", "','") + "') ");
            ViewBag.selItem_tranCompId = Models.CmbObjMD.selItem_tranCompId(ZhConfig.IsAddIndexZero.Yes, "", " and statusId='10' and dcId in ('" + userData.dcId.ToString().Replace(",", "','") + "') ");

            return View();
        }

        #region Control_GetGridJSON
        public ActionResult GetGridJSON(int page, int rows, string order, string tranCompId, string taskStatusId, string value_dcId, string dispatch_id)
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

                string strCond = " where 1=1 ";

                if (string.IsNullOrEmpty(value_dcId))
                {
                    strCond += " and dcId in ('" + value_dcId + "') ";
                }
                else
                {
                    strCond += " and dcId in ('" + userData.dcId.Replace(",", "','") + "') ";
                }


                //if (!string.IsNullOrEmpty(lbl_cmb1) && !string.IsNullOrEmpty(value_cmb1))
                //{
                //    strCond += " and " + lbl_cmb1 + " like '%" + value_cmb1 + "%' ";
                //}

                //if (!string.IsNullOrEmpty(userData.dcId))
                //{
                //    strCond += " and dcId in ('" + userData.dcId.Replace(",", "','") + "') ";
                //}

                if (!string.IsNullOrEmpty(dispatch_id))
                {
                    strCond += " and dispatch_Id='" + dispatch_id + "' ";
                }

                if (!string.IsNullOrEmpty(tranCompId))
                {
                    strCond += " and tranCompId='" + tranCompId + "' ";
                }

                if (!string.IsNullOrEmpty(taskStatusId))
                {
                    strCond += " and taskStatus='" + taskStatusId + "' ";
                }

                string colBtnView = "";
                colBtnView += "'<a href=\"javascript:void(0)\" onclick=\"btnView(''' + rtrim(dcShip) + ''')\" class=\"btn btn-primary\">View</a>'";

                strSql.Clear();
                strSql.Append(" select *, " + colBtnView + " btnView from ( SELECT  ROW_NUMBER() OVER (ORDER BY " + order + ") AS RowNum, * FROM ");
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
                    itemObject.Add("bindDelTime", dr["bindTime"].ToString() + "<br/>" + dr["realDelDate"].ToString());
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


        #region GetBolData
        public ActionResult GetBolData(string pk)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                strSql.Clear();
                strSql.AppendLine(" select * from U_A30020 where dcShip='" + pk + "'");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception("Data not found.");

                foreach (DataColumn dc in tbl_QueryData1.Columns)
                {
                    switch (dc.ColumnName)
                    {
                        case "dcShip":
                            strSql.Clear();
                            strSql.AppendLine(" select * from T30_carinv where dcShip='" + tbl_QueryData1.Rows[0][dc].ToString() + "' order by arriveSeqD ");
                            DataTable tbl_BolData = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_BolData");

                            JArray dja = new JArray();
                            foreach (DataRow ddr in tbl_BolData.Rows)
                            {
                                var itemObject = new JObject();
                                foreach (DataColumn ddc in tbl_BolData.Columns)
                                {
                                    switch (ddc.ColumnName)
                                    {
                                        case "bol_no":
                                            itemObject.Add(ddc.ColumnName, ddr[ddc].ToString());

                                            strSql.Clear();
                                            strSql.AppendLine(" select hawb, dn, dnqty, dnplt, aw  from dnmain where bol_no='" + ddr[ddc].ToString() + "' ");
                                            DataTable tbl_dn = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection2, strSql.ToString(), "tbl_dn");
                                            JArray dd_ja = new JArray();
                                            foreach (DataRow dddr in tbl_dn.Rows)
                                            {
                                                var dd_itemObject = new JObject();
                                                foreach (DataColumn dddc in tbl_dn.Columns)
                                                {
                                                    dd_itemObject.Add(dddc.ColumnName, dddr[dddc].ToString());
                                                }
                                                dd_ja.Add(dd_itemObject);
                                            }

                                            itemObject.Add("dn", dd_ja);
                                            break;
                                        default:
                                            itemObject.Add(ddc.ColumnName, ddr[ddc].ToString());
                                            break;
                                    }
                                }
                                dja.Add(itemObject);
                            }
                            jo.Add("rows", dja);

                            jo.Add(dc.ColumnName, tbl_QueryData1.Rows[0][dc].ToString());
                            break;
                        default:
                            jo.Add(dc.ColumnName, tbl_QueryData1.Rows[0][dc].ToString());
                            break;
                    }
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

        #region GetBolData
        public ActionResult GetFile(string pk, string pk2)
        {
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();
            JObject jo = new JObject();
            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                strSql.Clear();
                strSql.AppendLine(" select * from T30_fileManage where sysFileId='" + pk + "'");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                if (tbl_QueryData1.Rows.Count == 0) throw new Exception("Data not found.");

                Models.MEC_MethodMD.Aws aws = new Models.MEC_MethodMD.Aws();

                string bucketName = System.Web.Configuration.WebConfigurationManager.AppSettings["S3_BucketName"]; ;
                string key = "pod/" + pk2 + "/" + tbl_QueryData1.Rows[0]["fileNameSys"].ToString();
                string url = aws.GetObjectUrlInS3(bucketName, key);

                jo.Add("url", url);
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

    }
}