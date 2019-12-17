using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace Fly.Framework.Common
{
    public class RouteConfig
    {
        private static RouteConfigurationSection s_Configuration = null;
        private static object s_SyncObj = new object();

        public static void RegisterRoutes(bool cutOutUrlFirstSection = false)
        {
            RegisterRoutes(RouteTable.Routes, cutOutUrlFirstSection);
        }

        public static void RegisterRoutes(RouteCollection routes, bool cutOutUrlFirstSection = false)
        {
            ControllerBuilder.Current.SetControllerFactory(new CustomControllerFactory());
            RouteConfigurationSection section = GetRouteConfiguration();
            routes.MapRoute(section, cutOutUrlFirstSection);
        }

        internal static RouteConfigurationSection GetRouteConfiguration()
        {
            if(s_Configuration == null)
            {
                lock(s_SyncObj)
                {
                    if(s_Configuration == null)
                    {
                        s_Configuration = (RouteConfigurationSection)ConfigurationManager.GetSection("routeConfig");
                    }
                }
            }
            return s_Configuration;
        }
    }
}