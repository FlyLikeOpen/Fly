using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using System.Web.Mvc;
using System.Collections.Specialized;
using System.Web;
using System.Xml.Linq;
using System.IO;

namespace Fly.Framework.Common
{
    public static class RouteCollectionExtensions
    {
        public static void MapRoute(this RouteCollection routes, RouteConfigurationSection section, bool cutOutUrlFirstSection)
        {
            if(section == null)
            {
                throw new ApplicationException("没有配置config文件中的routeConfig节点");
            }
            // Manipulate the Ignore List
            if (section.Ignore != null)
            {
                foreach (IgnoreItem ignoreItem in section.Ignore)
                {
                    RouteValueDictionary ignoreConstraints = new RouteValueDictionary();
                    if (ignoreItem.Constraints != null)
                    {
                        foreach (Constraint constraint in ignoreItem.Constraints)
                        {
                            ignoreConstraints.Add(constraint.Name, constraint.Value);
                        }
                    }
                    MapIgnoreRoute(routes, ignoreItem.Url, ignoreConstraints);
                }
            }
            if (section.Areas != null)
            {
                foreach (AreaItem area in section.Areas)
                {
                    MapByRoutingCollection(routes, cutOutUrlFirstSection, area.Name, area.Map, area.Namespaces);
                }
            }
            MapByRoutingCollection(routes, cutOutUrlFirstSection, string.Empty, section.Map, section.Namespaces);
        }

        private static void MapByRoutingCollection(RouteCollection routes, bool cutOutUrlFirstSection, string areaName, RoutingCollection routingCollection, NamespacesCollection namespacesCollection)
        {
            if (routingCollection == null || routingCollection.Count <= 0)
            {
                return;
            }
            List<string> namespaces = new List<string>();
            if (namespacesCollection != null)
            {
                foreach (Namespace ns in namespacesCollection)
                {
                    namespaces.Add(ns.Name);
                }
            }
            DeviceType? dtype = null;
            string tmp = routingCollection.Device;
            DeviceType tt;
            if (string.IsNullOrWhiteSpace(tmp) == false && Enum.TryParse<DeviceType>(tmp, true, out tt))
            {
                dtype = tt;
            }
            string re_url = routingCollection.RedirectUrlForOtherDevices;
            string domain = routingCollection.Domain;
            foreach (RoutingItem routingItem in routingCollection)
            {
                RouteValueDictionary defaults = new RouteValueDictionary();
                RouteValueDictionary constraints = new RouteValueDictionary();
                if (string.IsNullOrWhiteSpace(routingItem.Controller) == false)
                {
                    defaults.Add("controller", routingItem.Controller);
                }
                if (string.IsNullOrWhiteSpace(routingItem.Action) == false)
                {
                    defaults.Add("action", routingItem.Action);
                }
                foreach (Parameter param in routingItem.Paramaters)
                {
                    if (string.IsNullOrWhiteSpace(param.Value) == false)
                    {
                        string v = param.Value.Trim();
                        if (v == "UrlParameter.Optional")
                        {
                            defaults.Add(param.Name, UrlParameter.Optional);
                        }
                        else
                        {
                            defaults.Add(param.Name, v);
                        }
                    }
                    if (string.IsNullOrWhiteSpace(param.Constraint) == false)
                    {
                        constraints.Add(param.Name, param.Constraint);
                    }
                }
                MapRoute(routes, cutOutUrlFirstSection, routingItem.Name, routingItem.Url, defaults, constraints, areaName, namespaces.ToArray(),
                    dtype, string.IsNullOrWhiteSpace(routingItem.RedirectUrlForOtherDevices) ? re_url : routingItem.RedirectUrlForOtherDevices, domain, routingItem.AuthKey);
            }
        }

        private static void MapIgnoreRoute(RouteCollection routes, string url, RouteValueDictionary constraints)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            IgnoreRoute ignore = new IgnoreRoute(url);
            ignore.Constraints = constraints;
            routes.Add(ignore);
        }

        private static void MapRoute(
            RouteCollection routes, bool cutOutUrlFirstSection,
            string name,
            string url,
            RouteValueDictionary defaults,
            RouteValueDictionary constraints,
            string area,
            string[] namespaces, DeviceType? dt, string redirectUrl, string domain, string authKey)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            Route route = new CustomRoute(url, new MvcRouteHandler(), name, cutOutUrlFirstSection);
            route.Defaults = defaults;
            route.Constraints = constraints;
            if (string.IsNullOrWhiteSpace(area) == false)
            {
                route.DataTokens["Area"] = area;
            }
            if (namespaces != null && namespaces.Length > 0)
            {
                route.DataTokens["Namespaces"] = namespaces;
                route.DataTokens["UseNamespaceFallback"] = false;
            }
            if (dt.HasValue)
            {
                route.DataTokens["_ibb_device_type"] = dt.Value;
            }
            if (string.IsNullOrWhiteSpace(redirectUrl) == false)
            {
                route.DataTokens["_ibb_redirectUrlForOtherDevices"] = redirectUrl;
            }
            if (string.IsNullOrWhiteSpace(domain) == false)
            {
                route.DataTokens["_ibb_domain"] = domain;
            }
            if (string.IsNullOrWhiteSpace(authKey) == false)
            {
                route.DataTokens["_ibb_authKey"] = authKey;
            }
            routes.Add(name, route);
        }

        private sealed class IgnoreRoute : Route
        {
            // Methods
            public IgnoreRoute(string url)
                : base(url, new StopRoutingHandler())
            {
            }

            public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
            {
                return null;
            }
        }
    }
}
