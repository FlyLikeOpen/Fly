using System;
using System.Web.Mvc;
using Fly.Framework.Common;
using Fly.APIs.Common;
using Fly.WebService.Common;

namespace Fly.WebService
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            ContextManager.SetContextType(typeof(WebContext));
            RegisterViewEngines(ViewEngines.Engines);
            FilterConfig.RegisterGlobalFilters();
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes();
            BundleConfig.RegisterBundles();
            Logger.OperatorUserResolver = new Func<string, string>(u =>
            {
                if (string.IsNullOrWhiteSpace(u))
                {
                    return string.Empty;
                }
                Guid uid;
                if (Guid.TryParse(u.Trim(), out uid) == false)
                {
                    return u;
                }
                var user = Api<IStaffUserApi>.Instance.Get(uid);
                if (user == null)
                {
                    return u;
                }
                return "[" + user.LoginId + "] " + user.DisplayName;

            });
            RegisterApis();
            RegisterQueues();
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            WebGlobalErrorHandler.Handle(this);
        }

        public static void RegisterViewEngines(ViewEngineCollection viewEngines)
        {
            viewEngines.Clear();
            viewEngines.Add(new CustomViewLocationRazorViewEngine());
        }

        public static void RegisterApis()
        {
            //todo

        }

        public static void RegisterQueues()
        {

        }
        
    }

    public class CustomViewLocationRazorViewEngine : System.Web.Mvc.RazorViewEngine
    {
        public CustomViewLocationRazorViewEngine()
        {
            this.ViewLocationFormats = new[] 
            {
                "~/Views/{1}/{0}.cshtml", "~/Views/{1}/{0}.vbhtml",
                "~/Views/Shared/Pages/{0}.cshtml", "~/Views/Shared/Pages/{0}.vbhtml"
            };

            this.MasterLocationFormats = new[] 
            {
                "~/Views/{1}/{0}.cshtml", "~/Views/{1}/{0}.vbhtml",
                "~/Views/Shared/Layouts/{0}.cshtml", "~/Views/Shared/Layouts/{0}.vbhtml"
            };

            this.PartialViewLocationFormats = new[] 
            {
                "~/Views/{1}/{0}.cshtml", "~/Views/{1}/{0}.vbhtml",
                "~/Views/{1}/Controls/{0}.cshtml", "~/Views/{1}/Controls/{0}.vbhtml",
                "~/Views/Shared/Controls/{0}.cshtml", "~/Views/Shared/Controls/{0}.vbhtml"
            };
        }
    }
}