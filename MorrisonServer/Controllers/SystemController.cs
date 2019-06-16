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
    public class SystemController : Controller
    {
        #region Bacic variables
        string resultCode = "10";
        string errStr = "";
        StringBuilder strSql = new StringBuilder(200);
        #endregion

        // GET: System
        public ActionResult Index()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (userData == null)
            {
                //Session.Remove("ticket");
                return RedirectToAction("Index", "Home");
            }
            else ViewBag.userType = ZhClass.SqlTool.GetOneDataValue(" select userName from S10_users where sysUserId='" + userData.sysUserId + "' ").ToString();

            return View();
        }

        /// <summary>
        /// 登入後取得MenuTree
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult GetTreeExpandAll()
        {
            JObject jo = new JObject();

            try
            {
                Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                strSql.Clear();
                strSql.Append(" select * from S00_funcs where funcId in (select funcId from S10_funcLimit where groupId in (select groupId from S10_userGroups where sysUserId='" + userData.sysUserId + "') and limitId='1' group by funcId) ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");

                DataRow[] dr = tbl_QueryData1.Select("parentFuncId=''");

                string tmpFunc = "";


                for (int i = 0; i < tbl_QueryData1.Rows.Count; i++)
                {

                }

                #region 第一層
                for (int i = 0; i < dr.Length; i++)
                {
                    //組建menu的字串
                    tmpFunc += "<li>";
                    if (dr[i]["url"].ToString().Trim() == "")
                    {
                        tmpFunc += "<a href='javascript:void(0)' data-target='#funcId_" + dr[i]["funcId"].ToString().Trim() + "'  data-toggle='collapse' name='ddl'>";
                    }
                    else
                    {
                        tmpFunc += "<a href='javascript:void(0)' onclick='GoPage(\"" + dr[i]["url"].ToString().Trim() + "\")' data-target='#funcId_" + dr[i]["funcId"].ToString().Trim() + "'  data-toggle='collapse'>";
                        tmpFunc += "";
                    }

                    //設定icon
                    if (dr[i]["icon"].ToString().Trim() != "")
                    {
                        tmpFunc += "<i class='" + dr[i]["icon"].ToString() + "'></i>";
                    }

                    //menu的中文名稱
                    tmpFunc += "<span style='font-family:Microsoft JhengHei;'>&nbsp;" + dr[i]["funcName"].ToString().Trim() + "</span>";

                    #region 第二層
                    DataRow[] dr2 = tbl_QueryData1.Select("parentFuncId='" + dr[i]["funcId"].ToString().Trim() + "'");
                    if (dr2.Length > 0)
                    {
                        tmpFunc += "<span class='fa arrow'></span>";
                    }

                    tmpFunc += "</a>";

                    if (dr2.Length > 0)
                    {
                        tmpFunc += "<ul class='nav nav-second-level collapse' id='funcId_" + dr[i]["funcId"].ToString().Trim() + "'>";
                        for (int j = 0; j < dr2.Length; j++)
                        {
                            tmpFunc += "<li>";
                            if (dr2[j]["url"].ToString().Trim() == "")
                            {
                                tmpFunc += "<a href='javascript:void(0)' data-target='#funcId_" + dr2[j]["funcId"].ToString().Trim() + "'  data-toggle='collapse' name='ddl'>";
                            }
                            else
                            {
                                tmpFunc += "<a href='javascript:void(0)' onclick='GoPage(\"" + dr2[j]["url"].ToString().Trim() + "\")' data-target='#funcId_" + dr2[j]["funcId"].ToString().Trim() + "'  data-toggle='collapse'>";
                            }
                            if (dr2[j]["icon"].ToString().Trim() != "")
                            {
                                tmpFunc += "<i class='" + dr2[j]["icon"].ToString() + "'></i>";
                            }


                            tmpFunc += "<span style='font-family:Microsoft JhengHei;'>&nbsp;" + dr2[j]["funcName"].ToString().Trim() + "</span>";


                            #region 第三層
                            DataRow[] dr3 = tbl_QueryData1.Select("parentFuncId='" + dr2[j]["funcId"].ToString().Trim() + "'");
                            if (dr3.Length > 0)
                            {
                                tmpFunc += "<span class='fa arrow'></span>";
                            }

                            tmpFunc += "</a>";

                            if (dr3.Length > 0)
                            {
                                tmpFunc += "<ul class='nav nav-third-level collapse' id='funcId_" + dr2[j]["funcId"].ToString().Trim() + "'>";
                                for (int k = 0; k < dr3.Length; k++)
                                {
                                    tmpFunc += "<li>";
                                    if (dr3[k]["url"].ToString().Trim() == "")
                                    {
                                        tmpFunc += "<a href='javascript:void(0)'>";
                                    }
                                    else
                                    {
                                        tmpFunc += "<a href='javascript:void(0)' onclick='GoPage(\"" + dr3[k]["url"].ToString().Trim() + "\")'>";
                                    }
                                    if (dr3[k]["icon"].ToString().Trim() != "")
                                    {
                                        tmpFunc += "<i class='" + dr3[k]["icon"].ToString() + "'></i>";
                                    }


                                    tmpFunc += "<span style='font-family:Microsoft JhengHei;'>&nbsp;" + dr3[k]["funcName"].ToString().Trim() + "</span>";
                                    tmpFunc += "</a>";
                                    tmpFunc += "</li>";
                                }
                                tmpFunc += "</ul>";
                            }
                            #endregion
                            tmpFunc += "</li>";
                        }
                        tmpFunc += "</ul>";
                    }
                    #endregion
                    tmpFunc += "</li>";
                }
                #endregion

                jo.Add("tmpFunc", tmpFunc);
                jo.Add("status", "OK");
            }
            catch (Exception ex)
            {
                jo.Add("status", "Error");
                jo.Add("error", ex.Message);
            }
            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }



        public ActionResult GetTreeExpandAllV2()
        {
            JObject jo = new JObject();
            Models.ZhWebClass_AngusV1.UserData userData = new Models.ZhWebClass_AngusV1.UserData();

            try
            {
                userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();

                #region 從DB取得menus
                strSql.Clear();
                //strSql.Append(" select * from S00_funcs ");
                strSql.Append(" select * from S00_funcs where funcId in (select funcId from S10_funcLimit where groupId in (select groupId from S10_userGroups where sysUserId='" + userData.sysUserId + "') and limitId='1' group by funcId) and statusId='10' order by sortValue ");
                DataTable tbl_QueryData1 = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql.ToString(), "tbl_QueryData1");
                #endregion

                #region 開始將每個menu轉換成對應的html
                //暫存每個menu所對應的html碼
                //例：Key:A10、Values:<li>功能1</li>
                Dictionary<string, string> dic = new Dictionary<string, string>();
                //暫存每個parentFuncId下包含的funcId，換言之，暫存所有有下一層的menu集合
                //例：假如 A11、A12、A13皆隸屬於A10，儲存的方式→Key:A10、Values:A11,A12,A13
                Dictionary<string, string> Groups = new Dictionary<string, string>();

                for (int i = 0; i < tbl_QueryData1.Rows.Count; i++)
                {
                    string tmpFunc = "";
                    DataRow[] dr = tbl_QueryData1.Select("parentFuncId='" + tbl_QueryData1.Rows[i]["funcId"].ToString() + "'");
                    //判斷目前menu是否有子menu
                    if (dr.Length > 0)
                    {
                        //組建menu的字串
                        tmpFunc += "<li>";
                        if (tbl_QueryData1.Rows[i]["url"].ToString().Trim() == "")
                        {
                            tmpFunc += "<a href='javascript:void(0)' data-target='#funcId_" + tbl_QueryData1.Rows[i]["funcId"].ToString().Trim() + "'  data-toggle='collapse' name='ddl'>";
                        }
                        else
                        {
                            tmpFunc += "<a href='javascript:void(0)' onclick='GoPage(\"" + tbl_QueryData1.Rows[i]["url"].ToString().Trim() + "\")' data-target='#funcId_" + tbl_QueryData1.Rows[i]["funcId"].ToString().Trim() + "'  data-toggle='collapse'>";
                            tmpFunc += "";
                        }

                        //設定icon
                        if (tbl_QueryData1.Rows[i]["icon"].ToString().Trim() != "")
                        {
                            tmpFunc += "<i class='" + tbl_QueryData1.Rows[i]["icon"].ToString() + "'></i>";
                        }


                        //menu的中文名稱
                        //組建子menu的ul，並插入識別符號「@@」
                        //例：<li>A10<ul>@@A10</ul></li>
                        tmpFunc += "<span style='font-family:Microsoft JhengHei;'>&nbsp;" + tbl_QueryData1.Rows[i]["funcName"].ToString().Trim() + "</span>";
                        tmpFunc += "<span class='fa arrow'></span>";
                        tmpFunc += "</a>";
                        if (tbl_QueryData1.Rows[i]["parentFuncId"].ToString().Trim() == "")
                        {
                            tmpFunc += "<ul class='nav nav-second-level collapse' id='funcId_" + tbl_QueryData1.Rows[i]["funcId"].ToString().Trim() + "'>";
                        }
                        else
                        {
                            tmpFunc += "<ul class='nav nav-third-level collapse' id='funcId_" + tbl_QueryData1.Rows[i]["funcId"].ToString().Trim() + "'>";
                        }
                        tmpFunc += "@@" + tbl_QueryData1.Rows[i]["funcId"].ToString().Trim() + "@@";
                        tmpFunc += "</ul>";
                        tmpFunc += "</li>";
                    }
                    else
                    {

                        tmpFunc += "<li>";
                        if (tbl_QueryData1.Rows[i]["url"].ToString().Trim() == "")
                        {
                            tmpFunc += "<a href='javascript:void(0)'>";
                        }
                        else
                        {
                            tmpFunc += "<a href='javascript:void(0)' onclick='GoPage(\"" + tbl_QueryData1.Rows[i]["url"].ToString().Trim() + "\")'>";
                        }
                        if (tbl_QueryData1.Rows[i]["icon"].ToString().Trim() != "")
                        {
                            tmpFunc += "<i class='" + tbl_QueryData1.Rows[i]["icon"].ToString() + "'></i>";
                        }


                        tmpFunc += "<span style='font-family:Microsoft JhengHei;'>&nbsp;" + tbl_QueryData1.Rows[i]["funcName"].ToString().Trim() + "</span>";
                        tmpFunc += "</a>";
                        tmpFunc += "</li>";
                    }

                    if (dr.Length > 0)
                    {
                        string tmp = "";
                        for (int j = 0; j < dr.Length; j++)
                        {
                            tmp += dr[j]["funcId"].ToString().Trim() + ",";
                        }
                        tmp = tmp.Substring(0, tmp.Length - 1);
                        Groups.Add(tbl_QueryData1.Rows[i]["funcId"].ToString().Trim(), tmp);
                    }

                    dic.Add(tbl_QueryData1.Rows[i]["funcId"].ToString().Trim(), tmpFunc);
                }
                #endregion

                #region 組合各menu的階層
                //暫存每個parentFuncId下包含的funcId的html碼，換言之，暫存所有有下一層menu的html的集合
                //例：Key:A10、Values:<li>A11</li><li>A12</li><li>A13</li>
                Dictionary<string, string> outputGroups = new Dictionary<string, string>();

                //主要是透過暫存於Groups中的集合來進行區分
                foreach (KeyValuePair<string, string> item in Groups)
                {
                    //將對應的子menu轉成字串集合
                    string[] tmpMenus = item.Value.Split(',');
                    string tmpMenu = "";
                    //透過一開始組成的所有menu的html，並依照tmpMenus中的menu，組成menu的字串
                    //例：A10的子menu為A11,A12,A13，將所有子menu轉成字串，並存入outputGroups
                    //Key:A10、Values:<li>A11</li><li>A12</li><li>A13</li>
                    for (int i = 0; i < tmpMenus.Length; i++)
                    {
                        tmpMenu += dic[tmpMenus[i]];
                    }
                    outputGroups.Add(item.Key, tmpMenu);
                }

                //進行所有子menu的組合
                //使用do while方法進行menu的組合
                //當start小於-1則跳出迴圈
                int start = 1;
                do
                {
                    //暫存原始的outputGroups，因為outputGroups會在處理的過程中改變
                    //在foreach執行中不允許指定的集合進行改變，因此要宣告一個outputGroups2
                    Dictionary<string, string> outputGroups2 = new Dictionary<string, string>(outputGroups);
                    foreach (KeyValuePair<string, string> item in outputGroups2)
                    {
                        //@@主要一開始組html字串時，安插在內用來區別有子menu的文字
                        //例：@@A10@@
                        //假設A10的子menu有A11,A12,A13，而A13還有子menu A13-1
                        //此時outputGroups就會有兩個Key
                        //A10:<li>A11</li><li>A12</li><li>A13<ul>@@A13</ul></li>
                        //A13:<li>A13-1</li>

                        //透過下方邏輯，最後outputGroups會輸出
                        //A10:<li>A11</li><li>A12</li><li>A13<ul><li>A13-1</li></ul></li>
                        if (item.Value.IndexOf("@@") > -1)
                        {
                            //尋找第一組@@的位置
                            int first = outputGroups[item.Key].IndexOf("@@") + 2;
                            //尋找第二組@@的位置
                            int second = outputGroups[item.Key].IndexOf("@@", first);
                            //利用第一組與第二組@@的位置得出menu的名稱
                            string menus = outputGroups[item.Key].Substring(first, second - first);
                            //組合outputGroups中各個應的menu字串
                            outputGroups[item.Key] = outputGroups[item.Key].Replace("@@" + menus + "@@", outputGroups[menus]);
                            //移除已經組合完畢的menu
                            outputGroups.Remove(menus);
                            //start設定為1表示迴圈仍未跑完
                            start = 1;
                            break;
                        }
                    }
                    start--;
                }
                while (start > -1);
                #endregion

                #region 輸出menu，將所有menu轉成字串，暫存於tmpFunc
                string outputFuncs = "";
                DataRow[] drs = tbl_QueryData1.Select("parentFuncId=''");
                foreach (DataRow dr in drs)
                {
                    string funcId = dr["funcId"].ToString().Trim();
                    if (outputGroups.ContainsKey(funcId))
                    {
                        outputFuncs += dic[funcId].Replace("@@" + funcId + "@@", outputGroups[funcId]);
                    }
                    else
                    {
                        outputFuncs += dic[funcId];
                    }
                }
                #endregion

                jo.Add("tmpFunc", outputFuncs);
            }
            catch (Exception ex)
            {
                resultCode = "01";
                errStr = ex.Message;
            }

            jo.Add("resultCode", resultCode);
            jo.Add("error", errStr);

            #region operLog 
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
                operDr["statusId"] = "40";
                operDr["resultCode"] = resultCode;
                operDr["errMsg"] = errStr;
                if (resultCode == "01") operDr["strSql"] = strSql.ToString();

                tbl_OperLog.Rows.Add(operDr);
                errStr = Models.ZhWebClass_AngusV1.Log.SaveOperLog(tbl_OperLog);
            }
            #endregion

            return Content(JsonConvert.SerializeObject(jo), "application/json");
        }
    }
}