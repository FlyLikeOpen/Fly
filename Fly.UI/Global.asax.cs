using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.SessionState;
using Fly.Framework.Common;
using Fly.APIs.Common;
using Fly.UI.Common;

namespace Fly.UI
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            ContextManager.SetContextType(typeof(WebContext));
            RegisterViewEngines(ViewEngines.Engines);
            FilterConfig.RegisterGlobalFilters();
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes();
            BundleConfig.RegisterBundles();
            RegisterApis();
            StartSocketServer("MMServer", 17170);
            BindSocketEvents();
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
                Fly.Objects.Common.StaffUser user = Api<Fly.APIs.Common.IStaffUserApi>.Instance.Get(uid);
                if (user == null)
                {
                    return u;
                }
                return "[" + user.LoginId + "] " + user.DisplayName;
            });
            RegisterQueues();
            RegisterJobs();
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            WebGlobalErrorHandler.Handle(this);
            StopSocketServer("MMServer");
        }

        protected void Application_End(object sender, EventArgs e)
        {
            StopSocketServer("MMServer");
        }

        public static void RegisterViewEngines(ViewEngineCollection viewEngines)
        {
            viewEngines.Clear();
            viewEngines.Add(new CustomViewLocationRazorViewEngine());
        }

        public static void RegisterApis()
        {
            Api<IWebSocketServerManager>.Register("MMServer", "Fly.APIImpls.Common.WebSocketServerManager, Fly.APIImpls");
            }

        private void RegisterQueues()
        {
            }

        private void RegisterJobs()
        {
        }

        private int TryGetConfigValueOrDefault(string nodeName, int defaultValue)
        {
            int s1;
            string tmp1 = ConfigurationManager.AppSettings[nodeName];
            if (string.IsNullOrWhiteSpace(tmp1) == false
                && int.TryParse(tmp1, out s1)
                && s1 > 0)
            {
                return s1;
            }
            return defaultValue;
        }

        public static void StartSocketServer(string registerName, int? serverPort = null)
        {
            var wseServer = Api<IWebSocketServerManager>.GetInstance(registerName);
            if (wseServer == null)
            {
                throw new ApplicationException(string.Format("IOC容器没有注册名字为{0}的对象,请检查Api是否被提前注册。", registerName));
            }
            var t = Task.Run(() =>
            {
                try
                {
                    if (serverPort.HasValue)
                    {
                        wseServer.Start(serverPort.Value);
                    }
                    else
                    {
                        wseServer.Start();
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
                finally
                {
                    wseServer.Dispose();
                }
            });
        }
        public static void BindSocketEvents()
        {
            Api<IStaffUserMessageManagerApi>.Instance.BindSocketEvents();
        }
        private static void StopSocketServer(string registerName)
        {
            var wseServer = Api<IWebSocketServerManager>.GetInstance(registerName);
            if (wseServer != null)
                wseServer.Dispose();
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