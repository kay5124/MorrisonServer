using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using ZhClass;

namespace MorrisonServer.Controllers
{
    public class MorrisonWEBController : Controller
    {
        #region Bacic variables
        string errStr = "";
        string resultCode = "10";
        string operStatusId = "20";
        string funcId = "MorrisonWEB";
        string operStatusId2 = "00";
        string dispatch_id_web = "";
        StringBuilder strSql = new StringBuilder(200);

        DataTable userInfo = null;

        #endregion
        // GET: MorrisonWEB
        public ActionResult Index(string dispatch_id)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (Session["ticket"] == null)
            {
                Session["appWEB"] = "Y";
                return RedirectToAction("webLogin", "Home");
            }
            userInfo = SqlTool.GetDataTable(" select * from T10_driver where sysUserId='" + userData.sysUserId + "' ", "userInfo");
            if (userInfo.Rows.Count == 0) return RedirectToAction("webLogin", "Home");

            if (!string.IsNullOrEmpty(dispatch_id))
            {
                dispatch_id_web = dispatch_id;
                return RedirectToAction("T15", "MorrisonWEB", new { dispatch_id = dispatch_id_web });
            }

            return RedirectToAction("M10", "MorrisonWEB");
        }


        //public ActionResult ErrorPage()
        //{
        //    return View();
        //}

        public ActionResult M10()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }

            if (userData != null)
            {
                //string sysUserId = SqlTool.GetOneDataValue(" select sysUserId from S10_users where userId='" + userData.userId + "' and appSysId='10' ").ToString();
                userInfo = SqlTool.GetDataTable(" select * from T10_driver where sysUserId='" + userData.sysUserId + "' ", "userInfo");
                if (userInfo.Rows.Count == 0) return RedirectToAction("webLogin", "Home");
                string tranCompName = SqlTool.GetOneDataValue(" select tranCompName from T10_tranComp where tranCompId=(select tranCompId from T10_driver where sysUserId='" + userData.sysUserId + "' ) ").ToString();
                ViewBag.tranCompName = tranCompName;
                ViewBag.trailerId = userInfo.Rows[0]["trailerId"].ToString();
                ViewBag.carId = userInfo.Rows[0]["carId"].ToString();
                ViewBag.dcId = userData.dcId;
                ViewBag.userName = userData.userName;
                ViewBag.webTicket = Session["ticket"];
                ViewBag.dispatch_id = dispatch_id_web;
            }


            //return Content(JsonConvert.SerializeObject(jo), "application/json");
            return View("M10");
        }

        public ActionResult T10()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }
            if (userData != null)
            {
                userInfo = SqlTool.GetDataTable(" select * from T10_driver where sysUserId='" + userData.sysUserId + "' ", "userInfo");
                if (userInfo.Rows.Count == 0) return RedirectToAction("webLogin", "Home");
                ViewBag.tranCompId = SqlTool.GetOneDataValue(" select tranCompId from T10_driver where sysUserId='" + userData.sysUserId + "' ").ToString();
                ViewBag.trailerId = userInfo.Rows[0]["trailerId"].ToString();
                ViewBag.carId = userInfo.Rows[0]["carId"].ToString();
                ViewBag.sysUserId = userData.sysUserId;
                ViewBag.dcId = userData.dcId;
                ViewBag.webTicket = Session["ticket"];
            }

            //return Content(JsonConvert.SerializeObject(jo), "application/json");
            return View("T10");
        }

        public ActionResult T15(string dispatch_id)
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }
            if (userData != null)
            {
                userInfo = SqlTool.GetDataTable(" select * from T10_driver where sysUserId='" + userData.sysUserId + "' ", "userInfo");
                if (userInfo.Rows.Count == 0) return RedirectToAction("webLogin", "Home");
                ViewBag.tranCompId = SqlTool.GetOneDataValue(" select tranCompId from T10_driver where sysUserId='" + userData.sysUserId + "' ").ToString();
                ViewBag.trailerId = userInfo.Rows[0]["trailerId"].ToString();
                ViewBag.carId = userInfo.Rows[0]["carId"].ToString();
                ViewBag.sysUserId = userData.sysUserId;
                ViewBag.dcId = userData.dcId;
                ViewBag.webTicket = Session["ticket"];
            }

            //return Content(JsonConvert.SerializeObject(jo), "application/json");
            return View("T15");
        }

        public ActionResult T16()
        {
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }
            ViewBag.webTicket = Session["ticket"];

            return View("T16");
        }

        public ActionResult T17()
        {
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }
            ViewBag.webTicket = Session["ticket"];

            return View("T17");
        }

        public ActionResult T19()
        {
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }
            ViewBag.webTicket = Session["ticket"];

            return View("T19");
        }

        public ActionResult T20()
        {
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }
            ViewBag.webTicket = Session["ticket"];

            return View("T20");
        }

        public ActionResult T21()
        {
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }
            ViewBag.webTicket = Session["ticket"];

            return View("T21");
        }

        public ActionResult T30()
        {
            Models.ZhWebClass_AngusV1.UserData userData = Models.ZhWebClass_AngusV1.UserHelper.GetUserData();
            if (Session["ticket"] == null)
            {
                return RedirectToAction("webLogin", "Home");
            }
            if (userData != null)
            {
                userInfo = SqlTool.GetDataTable(" select * from T10_driver where sysUserId='" + userData.sysUserId + "' ", "userInfo");
                if (userInfo.Rows.Count == 0) return RedirectToAction("webLogin", "Home");
                ViewBag.tranCompId = SqlTool.GetOneDataValue(" select tranCompId from T10_driver where sysUserId='" + userData.sysUserId + "' ").ToString();
                ViewBag.trailerId = userInfo.Rows[0]["trailerId"].ToString();
                ViewBag.carId = userInfo.Rows[0]["carId"].ToString();
                ViewBag.sysUserId = userData.sysUserId;
                ViewBag.dcId = userData.dcId;
                ViewBag.webTicket = Session["ticket"];
            }
            //return Content(JsonConvert.SerializeObject(jo), "application/json");
            return View("T30");
        }

    }
}