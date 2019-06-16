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
    public class S01030Controller : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "S01030";
        string operStatusId2 = "00";
        StringBuilder strSql = new StringBuilder(200);
        #endregion

        // GET: S01030
        public ActionResult S01030()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null) return RedirectToAction("Index", "Home");

            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"] == "")
                ViewBag.ServerName = "";
            else
                ViewBag.ServerName = "/" + System.Web.Configuration.WebConfigurationManager.AppSettings["ServerName"];

            ViewBag.selItem_groupId = Models.CmbObjMD.selItem_groupId(ZhConfig.IsAddIndexZero.No, "");
            
            return View();
        }

        #region Control_GetGridJSON
        public ActionResult GetGridJSON(int page, int rows, string sort, string order, string value_groupId)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            JObject jo = new JObject();
            try
            {
                if (string.IsNullOrEmpty(value_groupId))
                {
                    return Content("", "application/json");
                }

                //exec spU_S00030 'G01',1,20
                //par1:使用者群組
                //para2:資列起始編號
                //para3:資料結束編號

                strSql.Append("spU_S01030 '" + value_groupId + "','" + ((page - 1) * rows + 1).ToString() + "','" + page * rows + "'");

                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "spU_S00030");

                int totalCount = Convert.ToInt32(ZhClass.SqlTool.GetOneDataValue(ZhConfig.GlobalSystemVar.StrConnection1, "select count(*) from S00_funcs WHERE statusId='10' "));

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



        //public ActionResult DeleteSingle(C10_zip delRec)
        public ActionResult ActRows(List<Models.Base.MD_S10_funcLimit> addRows)
        {
            
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            JObject jo = new JObject();
            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
                operStatusId = "40";

                using (TransactionScope scope = new TransactionScope())
                {
                    using (SqlConnection cn = new SqlConnection(ZhConfig.GlobalSystemVar.StrConnection1))
                    {
                        cn.Open();

                        foreach (Models.Base.MD_S10_funcLimit actRow in addRows)
                        {
                            strSql.Clear();

                            switch (actRow.realRow.ToString())
                            {
                                case "0"://新增
                                    {
                                        actRow.creatUser = userData.userName;
                                        actRow.creatTime = DateTime.Now;

                                        strSql.Append("insert into S10_funcLimit (funcId,groupId,limitId,creatUser,creatTime) values (@funcId,@groupId,@limitId,@creatUser,getdate())");
                                    }
                                    break;
                                case "1"://修改
                                    {
                                        actRow.actUser = userData.userName;
                                        actRow.actTime = DateTime.Now;

                                        strSql.Append("update S10_funcLimit set limitId=@limitId,actUser=@actUser,actTime=getdate() where funcId=@funcId and groupId=@groupId");
                                    }
                                    break;
                            }

                            #region 設置 要傳入的 SqlParameter 資料

                            SqlParameter[] param = {
                                new SqlParameter("funcId", SqlDbType.Char, 8, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.funcId.ToString().Trim()),
                                new SqlParameter("groupId", SqlDbType.Char, 3, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, actRow.groupId.ToString().Trim()),
                                new SqlParameter("limitId", SqlDbType.Char, 1, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,  actRow.limitId),
                                new SqlParameter("creatUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed,userData.sysUserId),
                                new SqlParameter("actUser", SqlDbType.Int, 4, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Proposed, userData.sysUserId)
                            };


                            #endregion
                            errStr = ZhClass.SqlTool2.ExecuteNonQuery(cn, strSql.ToString(), param);
                            if (errStr != "") throw new Exception(errStr);

                        }
                    }
                    scope.Complete();
                }

                jo.Add("rows", getJsonForGrid(addRows));
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
                ////if(resultCode == "01" ) //operDr["strSql"] = strSql;

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }


        private JArray getJsonForGrid(List<Models.Base.MD_S10_funcLimit> addRows)
        {

            JArray ja = new JArray();
            foreach (Models.Base.MD_S10_funcLimit dr in addRows)
            {
                var itemObject = new JObject();

                if (dr.realRow.ToString() == "0")
                {
                    itemObject.Add("creatUser", dr.creatUser.ToString());
                    itemObject.Add("creatTime", dr.creatTime.ToString());
                }
                else
                {
                    itemObject.Add("actUser", dr.actUser.ToString());
                    itemObject.Add("actTime", dr.actTime.ToString());

                }
                itemObject.Add("funcId", dr.funcId.ToString());
                itemObject.Add("realRow", "1");


                ja.Add(itemObject);
            }

            return ja;
        }


        public ActionResult Get_Cmb_strJson()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

            DataTable tmpTable = Models.CmbObjMD.Get_cmb_limit(ZhConfig.IsAddIndexZero.No);

            JArray ja = new JArray();

            #region Gen cmb Json Data

            foreach (DataRow dr in tmpTable.Rows)
            {
                JObject itemObject = new JObject
                        {
                          {"id",dr[0].ToString()},
                          {"text",dr[1].ToString().Trim()}
                        };

                ja.Add(itemObject);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(ja), "application/json");
        }
    }
}