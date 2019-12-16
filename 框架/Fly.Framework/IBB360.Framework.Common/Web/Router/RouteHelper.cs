using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;

namespace Fly.Framework.Common
{
    public static class RouteHelper
    {
        #region Current Route

        public static RouteData GetCurrentRouteData()
        {
            if (HttpContext.Current == null || RouteTable.Routes == null)
            {
                return null;
            }
            return Cache.GetWithHttpContextCache("_7721CRD", () => RouteTable.Routes.GetRouteData(new HttpContextWrapper(HttpContext.Current)));
        }

        public static RoutingItem GetCurrentConfigRouteItem()
        {
            return Cache.GetWithHttpContextCache<RoutingItem>("RouteHelper.GetCurrentConfigRouteItem", () =>
            {
                string routeName = CurrentRouteName;
                if (string.IsNullOrWhiteSpace(routeName) == false)
                {
                    return FindRoutingItem(routeName);
                }
                return null;
            });
        }

        public static string GetCurrentConfigExtPropertyValue(string extPropertyName)
        {
            RoutingItem item = GetCurrentConfigRouteItem();
            if(item == null || item.ExtProperties == null || item.ExtProperties.Count <= 0)
            {
                return null;
            }
            var t = item.ExtProperties[extPropertyName];
            if(t == null)
            {
                return null;
            }
            return t.Value;
        }

        public static string CurrentRouteName
        {
            get
            {
                object routeName;
                if (TryGetDataTokenOfCurrentRoute("routeName", out routeName) && routeName != null)
                {
                    return routeName as string;
                }
                return null;
            }
        }

        public static string CurrentControllerName
        {
            get
            {
                var routeData = GetCurrentRouteData();
                return routeData == null ? null : routeData.GetRequiredString("controller");
            }
        }

        public static string CurrentActionName
        {
            get
            {
                object actionName;
                if (TryGetDataTokenOfCurrentRoute("real_action", out actionName) && string.IsNullOrWhiteSpace(actionName as string) == false)
                {
                    return actionName as string;
                }
                var routeData = GetCurrentRouteData();
                return routeData == null ? null : routeData.GetRequiredString("action");
            }
        }

        public static string CurrentAreaName
        {
            get
            {
                object areaName;
                if (TryGetDataTokenOfCurrentRoute("area", out areaName) && areaName != null)
                {
                    return areaName.ToString();
                }
                return string.Empty;
            }
        }

        public static bool TryGetRouteValueOfCurrentRoute(string key, out object value)
        {
            var routeData = GetCurrentRouteData();
            if (routeData == null || routeData.Values == null)
            {
                value = null;
                return false;
            }
            return routeData.Values.TryGetValue(key, out value);
        }

        public static bool TryGetDataTokenOfCurrentRoute(string key, out object value)
        {
            var routeData = GetCurrentRouteData();
            if (routeData == null || routeData.Values == null)
            {
                value = null;
                return false;
            }
            return routeData.DataTokens.TryGetValue(key, out value);
        }

        public static bool TryGetValueOfCurrentRoute(string key, out object value)
        {
            if (TryGetRouteValueOfCurrentRoute(key, out value))
            {
                return true;
            }
            if (TryGetDataTokenOfCurrentRoute(key, out value))
            {
                return true;
            }
            value = null;
            return false;
        }

        public static DeviceType? CurrentSupportedDeviceTypeInConfig
        {
            get
            {
                object device;
                if (TryGetDataTokenOfCurrentRoute("_ibb_device_type", out device) && device != null)
                {
                    return (DeviceType)device;
                }
                return null;
            }
        }

        public static string RedirectUrlForUnsupportedDevices
        {
            get
            {
                object url;
                if (TryGetDataTokenOfCurrentRoute("_ibb_redirectUrlForOtherDevices", out url) && url != null)
                {
                    return ((string)url).Trim();
                }
                return string.Empty;
            }
        }

        #endregion

        private static RoutingItem FindRoutingItem(string routeName)
        {
            routeName = routeName.Trim().ToLower();
            RouteConfigurationSection section = RouteConfig.GetRouteConfiguration();
            RoutingItem curRouteItem = null;
            if (section.Areas != null && section.Areas.Count > 0)
            {
                foreach (AreaItem area in section.Areas)
                {
                    if (area.Map == null || area.Map.Count <= 0)
                    {
                        continue;
                    }
                    foreach (RoutingItem routingItem in area.Map)
                    {
                        if (routingItem.Name.ToLower().Trim() == routeName)
                        {
                            curRouteItem = routingItem;
                            break;
                        }
                    }
                    if (curRouteItem != null)
                    {
                        break;
                    }
                }
            }
            if (curRouteItem == null && section.Map != null && section.Map.Count > 0)
            {
                foreach (RoutingItem routingItem in section.Map)
                {
                    if (routingItem.Name.ToLower().Trim() == routeName)
                    {
                        curRouteItem = routingItem;
                        break;
                    }
                }
            }
            return curRouteItem;
        }
                
        public static string BuildUrlTemplet(string routeName)
        {
            var curRouteItem = FindRoutingItem(routeName);
            if (curRouteItem == null)
            {
                throw new ApplicationException(string.Format("没有找到name为{0}（不区分大小写）的Route配置。", routeName));
            }
            string relUrl = curRouteItem.Url.ToLower().Trim().Trim('/', '\\');
            string first = HttpContext.Current.Items["url_first_section"] as string;
            if (string.IsNullOrWhiteSpace(first) == false)
            {
                relUrl = first + "/" + relUrl;
            }
            string domain = curRouteItem.Domain;
            object tmp;
            if (string.IsNullOrWhiteSpace(domain)
                && TryGetDataTokenOfCurrentRoute("_ibb_domain", out tmp) && tmp != null)
            {
                domain = tmp.ToString().Trim();
            }
            if (string.IsNullOrWhiteSpace(domain))
            {
                relUrl = string.Concat("/", relUrl, "/");
            }
            else
            {
                domain = domain.ToLower().Trim().Trim('/', '\\');
                if (domain.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    domain = String.Concat("http://", domain);
                }
                relUrl = string.Concat(domain, "/", relUrl, "/");
            }
            return relUrl;
        }

        public static string BuildUrl(string routeName, dynamic routeValues = null)
        {
            return InnerBuildUrl(routeName, routeValues == null ? null : new RouteValueDictionary(routeValues));
        }

        public static string BuildUrl(string routeName, IDictionary<string, string> routeValues)
        {
            return InnerBuildUrl(routeName, routeValues == null ? null : new RouteValueDictionary(routeValues));
        }

        private static string InnerBuildUrl(string routeName, RouteValueDictionary routeValues)
        {
            var curRouteItem = FindRoutingItem(routeName);
            if (curRouteItem == null)
            {
                throw new ApplicationException(string.Format("没有找到name为{0}（不区分大小写）的Route配置。", routeName));
            }
            var route = RouteTable.Routes.GetVirtualPath(null, routeName, routeValues);
            if (route == null)
            {
                StringBuilder args = new StringBuilder();
                if (routeValues != null)
                {
                    foreach (var entry in routeValues)
                    {
                        args.AppendFormat("{0} : {1}\r\n", entry.Key, entry.Value);
                    }
                }
                throw new ApplicationException(string.Format("无法找到匹配的路由，路由的名称为：{0}，路由参数为：\r\n{1}", routeName, args));
            }
            string url = route.VirtualPath;
            string domain = curRouteItem.Domain;
            object tmp;
            if (string.IsNullOrWhiteSpace(domain)
                && TryGetDataTokenOfCurrentRoute("_ibb_domain", out tmp) && tmp != null)
            {
                domain = tmp.ToString().Trim();
            }
            if (string.IsNullOrWhiteSpace(domain))
            {
                return url;
            }
            domain = domain.ToLower().Trim().Trim('/', '\\');
            if (domain.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) == false)
            {
                domain = String.Concat("http://", domain);
            }
            return string.Concat(domain, url);
        }
    }
}
