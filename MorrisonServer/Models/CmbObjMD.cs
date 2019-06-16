using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZhClass;

namespace MorrisonServer.Models
{
    public class CmbObjMD
    {


        public static DataTable Get_cmb_limit(ZhConfig.IsAddIndexZero isAddIndexZero)
        {
            string strSql = "SELECT limitId,limitName,limitId+':'+limitName as limitName2 FROM S00_limit where isUse=1  ";


            DataTable tmpTable = SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "S00_limit");
            tmpTable.PrimaryKey = new DataColumn[] { tmpTable.Columns["limitId"] };

            if (isAddIndexZero == ZhConfig.IsAddIndexZero.Yes)
            {
                DataRow tmpRow = tmpTable.NewRow();
                tmpRow["limitId"] = "0";

                ZhConfig.ZhIniObj.addZeroRowColumnInfo(tmpRow, "limitName", "limitName2");

                tmpTable.Rows.InsertAt(tmpRow, 0);
            }

            return tmpTable;

        }


        public static List<SelectListItem> selItem_groupId(ZhConfig.IsAddIndexZero isAddIndexZero, string firstItem)
        {
            List<SelectListItem> selItem = new List<SelectListItem>();

            #region sql select
            string strSql = "select groupId as value, groupName as text from S10_group where statusId='10'  order by groupId ";
            #endregion

            DataTable tmpTable = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTable");
            tmpTable.PrimaryKey = new DataColumn[] { tmpTable.Columns["value"] };

            if (isAddIndexZero == ZhConfig.IsAddIndexZero.Yes)
            {
                #region Insert 請選擇
                DataRow tmpRow = tmpTable.NewRow();
                tmpRow["value"] = "";
                if (firstItem != "")
                {
                    tmpRow["text"] = firstItem;
                }
                else
                {
                    tmpRow["text"] = "Select";
                }
                //ZhConfig.ZhIniObj.addZeroRowColumnInfo(tmpRow, "text");

                tmpTable.Rows.InsertAt(tmpRow, 0);
                #endregion
            }


            for (int i = 0; i < tmpTable.Rows.Count; i++)
            {
                selItem.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = tmpTable.Rows[i]["text"].ToString() });
            }

            selItem[0].Selected = true;

            return selItem;
        }


        public static List<SelectListItem> selItem_appGroupId(ZhConfig.IsAddIndexZero isAddIndexZero, string firstItem)
        {
            List<SelectListItem> selItem = new List<SelectListItem>();

            #region sql select
            string strSql = "select appGroupId as value, appGroupName as text from App_group where statusId='10'  order by appGroupId ";
            #endregion

            DataTable tmpTable = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTable");
            tmpTable.PrimaryKey = new DataColumn[] { tmpTable.Columns["value"] };

            if (isAddIndexZero == ZhConfig.IsAddIndexZero.Yes)
            {
                #region Insert 請選擇
                DataRow tmpRow = tmpTable.NewRow();
                tmpRow["value"] = "";
                if (firstItem != "")
                {
                    tmpRow["text"] = firstItem;
                }
                else
                {
                    tmpRow["text"] = "Select";
                }
                //ZhConfig.ZhIniObj.addZeroRowColumnInfo(tmpRow, "text");

                tmpTable.Rows.InsertAt(tmpRow, 0);
                #endregion
            }


            for (int i = 0; i < tmpTable.Rows.Count; i++)
            {
                selItem.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = tmpTable.Rows[i]["text"].ToString() });
            }

            selItem[0].Selected = true;

            return selItem;
        }


        /// <summary>
        /// 取得狀態的下拉式選單
        /// </summary>
        /// <param name="isAddIndexZero">是否需要空值的初始選項</param>
        /// <param name="firstItem">第一個參數必須為isAddIndexZero.Yes，目前此參數為設定第一個項目為空值所顯示的文字</param>
        /// <param name="statusType">狀態的類型</param>
        /// <param name="strCond">從db中取得的條件，例 "and statusId <> '30'"</param>
        /// <returns></returns>
        public static List<SelectListItem> selItem_statusId(ZhConfig.IsAddIndexZero isAddIndexZero, string firstItem, string statusType, string strCond)
        {
            List<SelectListItem> selItem = new List<SelectListItem>();

            #region sql select
            string strSql = "select statusId as value, statusName as text from S00_statusId where statusType='" + statusType + "' and isUse='1' " + strCond + " ";
            #endregion

            DataTable tmpTable = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTable");
            tmpTable.PrimaryKey = new DataColumn[] { tmpTable.Columns["value"] };

            if (isAddIndexZero == ZhConfig.IsAddIndexZero.Yes)
            {
                #region Insert 請選擇
                DataRow tmpRow = tmpTable.NewRow();
                tmpRow["value"] = "";
                if (firstItem != "")
                {
                    tmpRow["text"] = firstItem;
                }
                else
                {
                    tmpRow["text"] = "Select";
                }
                //ZhConfig.ZhIniObj.addZeroRowColumnInfo(tmpRow, "text");

                tmpTable.Rows.InsertAt(tmpRow, 0);
                #endregion
            }


            for (int i = 0; i < tmpTable.Rows.Count; i++)
            {
                selItem.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = tmpTable.Rows[i]["text"].ToString() });
            }

            selItem[0].Selected = true;

            return selItem;
        }

        /// <summary>
        /// 取得funcId各對應的名稱
        /// </summary>
        /// <param name="isAddIndexZero">是否需要空值的初始選項</param>
        /// <param name="firstItem">第一個參數必須為isAddIndexZero.Yes，目前此參數為設定第一個項目為空值所顯示的文字</param>
        /// <param name="strCond">塞選條件</param>
        /// <param name="IsOther">顯示funcs以外的功能名稱</param>
        /// <returns></returns>
        public static List<SelectListItem> selItem_menuTree(ZhConfig.IsAddIndexZero isAddIndexZero, string firstItem, string strCond, bool IsOther)
        {
            List<SelectListItem> selItem = new List<SelectListItem>();

            #region sql select
            string strSql = "select funcId as value, funcName as text from S00_funcs where (url <> '' and url is not null) and statusId='10' " + strCond + "  order by sortValue ";
            #endregion

            DataTable tmpTable = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTable");
            tmpTable.PrimaryKey = new DataColumn[] { tmpTable.Columns["value"] };

            if (IsOther)
            {
                DataRow tmpRow2 = tmpTable.NewRow();
                tmpRow2["value"] = "Home";
                tmpRow2["text"] = "Home Page";
                tmpTable.Rows.InsertAt(tmpRow2, 0);

                DataRow tmpRow3 = tmpTable.NewRow();
                tmpRow3["value"] = "System";
                tmpRow3["text"] = "Login Page";
                tmpTable.Rows.InsertAt(tmpRow3, 0);
            }

            if (isAddIndexZero == ZhConfig.IsAddIndexZero.Yes)
            {
                #region Insert 請選擇
                DataRow tmpRow = tmpTable.NewRow();
                tmpRow["value"] = "";
                if (firstItem != "")
                {
                    tmpRow["text"] = firstItem;
                }
                else
                {
                    tmpRow["text"] = "Select";
                }
                //ZhConfig.ZhIniObj.addZeroRowColumnInfo(tmpRow, "text");

                tmpTable.Rows.InsertAt(tmpRow, 0);
                #endregion
            }


            for (int i = 0; i < tmpTable.Rows.Count; i++)
            {
                selItem.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = tmpTable.Rows[i]["text"].ToString() });
            }

            selItem[0].Selected = true;

            return selItem;
        }



        /// <summary>
        /// 取得狀態的下拉式選單
        /// </summary>
        /// <param name="isAddIndexZero">是否需要空值的初始選項</param>
        /// <param name="firstItem">第一個參數必須為isAddIndexZero.Yes，目前此參數為設定第一個項目為空值所顯示的文字</param>
        /// <param name="strCond">從db中取得的條件，例 "and statusId <> '30'"</param>
        /// <returns></returns>
        public static List<SelectListItem> selItem_dcId(ZhConfig.IsAddIndexZero isAddIndexZero, string firstItem, string strCond)
        {
            List<SelectListItem> selItem = new List<SelectListItem>();

            #region sql select
            string strSql = "select dcId as value, dcName as text from C10_dc with (nolock) where statusId='10' " + strCond + " ";
            #endregion

            DataTable tmpTable = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTable");
            tmpTable.PrimaryKey = new DataColumn[] { tmpTable.Columns["value"] };

            if (isAddIndexZero == ZhConfig.IsAddIndexZero.Yes)
            {
                #region Insert 請選擇
                DataRow tmpRow = tmpTable.NewRow();
                tmpRow["value"] = "";
                if (firstItem != "")
                {
                    tmpRow["text"] = firstItem;
                }
                else
                {
                    tmpRow["text"] = "Select";
                }
                //ZhConfig.ZhIniObj.addZeroRowColumnInfo(tmpRow, "text");

                tmpTable.Rows.InsertAt(tmpRow, 0);
                #endregion
            }


            for (int i = 0; i < tmpTable.Rows.Count; i++)
            {
                selItem.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = tmpTable.Rows[i]["text"].ToString() });
            }

            if (tmpTable.Rows.Count > 0) selItem[0].Selected = true;

            return selItem;
        }


        /// <summary>
        /// 取得狀態的下拉式選單
        /// </summary>
        /// <param name="isAddIndexZero">是否需要空值的初始選項</param>
        /// <param name="firstItem">第一個參數必須為isAddIndexZero.Yes，目前此參數為設定第一個項目為空值所顯示的文字</param>
        /// <param name="strCond">從db中取得的條件，例 "and statusId <> '30'"</param>
        /// <returns></returns>
        public static List<SelectListItem> selItem_tranCompId(ZhConfig.IsAddIndexZero isAddIndexZero, string firstItem, string strCond)
        {
            List<SelectListItem> selItem = new List<SelectListItem>();

            #region sql select
            string strSql = " select tranCompId value, tranCompName text from T10_tranComp where tranCompId in (select tranCompId from T10_dcTranComp where 1=1 " + strCond + ") ";
            #endregion

            DataTable tmpTable = ZhClass.SqlTool.GetDataTable(ZhConfig.GlobalSystemVar.StrConnection1, strSql, "tmpTable");
            tmpTable.PrimaryKey = new DataColumn[] { tmpTable.Columns["value"] };

            if (isAddIndexZero == ZhConfig.IsAddIndexZero.Yes)
            {
                #region Insert 請選擇
                DataRow tmpRow = tmpTable.NewRow();
                tmpRow["value"] = "";
                if (firstItem != "")
                {
                    tmpRow["text"] = firstItem;
                }
                else
                {
                    tmpRow["text"] = "Select";
                }
                //ZhConfig.ZhIniObj.addZeroRowColumnInfo(tmpRow, "text");

                tmpTable.Rows.InsertAt(tmpRow, 0);
                #endregion
            }

            for (int i = 0; i < tmpTable.Rows.Count; i++)
            {
                //if(i!=0) selItem.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = "(" + tmpTable.Rows[i]["value"].ToString() + ")" + tmpTable.Rows[i]["text"].ToString() });
                //else selItem.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = tmpTable.Rows[i]["text"].ToString() });
                selItem.Add(new SelectListItem() { Value = tmpTable.Rows[i]["value"].ToString(), Text = tmpTable.Rows[i]["text"].ToString() });

            }

            selItem[0].Selected = true;

            return selItem;
        }
    }
}