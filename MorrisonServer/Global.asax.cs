using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.SessionState;

namespace MorrisonServer
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            SetConfig();
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private static void SetConfig()
        {

            try
            {

                ZhWebClass.WebApiHelper.APIuserId = "zhtech";
                ZhWebClass.WebApiHelper.APIpassword = "24369238";

                #region 設定連線字串
                ZhConfig.GlobalSystemVar.StrConnection1 = System.Configuration.ConfigurationManager.ConnectionStrings["ConnectStr1"].ConnectionString;
                ZhConfig.GlobalSystemVar.StrConnection2 = System.Configuration.ConfigurationManager.ConnectionStrings["ConnectStr2"].ConnectionString;
                #endregion


                ZhClass.ZhAssemblyInfo asmInfo = new ZhClass.ZhAssemblyInfo(System.Reflection.Assembly.GetExecutingAssembly());
                ZhConfig.GlobalSystemVar.Version = asmInfo.AssemblyVersion;
                ZhConfig.GlobalSystemVar.ProductTitle = asmInfo.AssemblyProduct;
            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }


        }
    }
}
