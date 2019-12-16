using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Fly.Framework.Common
{
    internal sealed class CustomRoute : Route
    {
        private bool m_CutOutUrlFirstSection;

        public CustomRoute(string url, IRouteHandler routeHandler, string routeName, bool cutOutUrlFirstSection)
            : base(url, routeHandler)
        {
            DataTokens = new RouteValueDictionary();
            DataTokens["routeName"] = routeName;
            m_CutOutUrlFirstSection = cutOutUrlFirstSection;
        }

        public void CutOutUrlFirstSection(string url, out string firstSection, out string urlWithoutFirstSection)
        {
            url = url.Trim().TrimStart('~', '/');
            urlWithoutFirstSection = url;
            firstSection = null;
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }
            int firstSlash = url.IndexOf('/');
            if (firstSlash > 0)
            {
                firstSection = url.Substring(0, firstSlash);
                urlWithoutFirstSection = url.Substring(firstSlash).TrimStart('/');
            }
            else
            {
                firstSection = url;
                urlWithoutFirstSection = string.Empty;
            }
        }

        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            string firstSection = null;
            if (m_CutOutUrlFirstSection && httpContext.Items["url_first_section"] == null)
            {
                string newUrl;
                CutOutUrlFirstSection(httpContext.Request.Path, out firstSection, out newUrl);
                newUrl = "~/" + newUrl.Trim().Replace("</", "").Replace("<", "").Replace("/>", "").Replace(">", "");
                httpContext.RewritePath(newUrl, true);
            }
            if (string.IsNullOrWhiteSpace(firstSection) == false && httpContext.Items["url_first_section"] == null)
            {
                httpContext.Items["url_first_section"] = firstSection;
            }
            RouteData data = base.GetRouteData(httpContext);
            if (data == null)
            {
                return null;
            }
            string controllerName = data.Values["controller"] as string;
            string actionName = data.Values["action"] as string;
            //data.Values["real_controller"] = controllerName;
            data.DataTokens["real_action"] = actionName;
            TempControllerFactory factory = new TempControllerFactory();
            Type ct = factory.GetTypeOfController(new RequestContext(httpContext, data), controllerName);
            if (ct != null) // 存在真实的Controller类型
            {
                ReflectedControllerDescriptor ds = new ReflectedControllerDescriptor(ct);
                ControllerContext cc = new ControllerContext { RequestContext = new RequestContext(httpContext, data) };
                var act = ds.FindAction(cc, actionName);
                if (act != null) // Controller中存在真实的Action方法
                {
                    return data;
                }
            }

            // 执行到这里说明没有找到实际存在的Controller.Action
            string[] exts = new string[] { ".cshtml", ".vbhtml", ".aspx" };
            string areaName = data.DataTokens["area"] as string;
            string fileNameWithoutExt;
            if (string.IsNullOrWhiteSpace(areaName))
            {
                fileNameWithoutExt = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    "Views", controllerName, actionName);
            }
            else
            {
                fileNameWithoutExt = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    areaName, "Views", controllerName, actionName);
            }
            foreach (string ext in exts)
            {
                string file = fileNameWithoutExt + ext;
                if (File.Exists(file))
                {
                    //string r = Path.Combine("~/Views", controllerName, actionName);
                    data.DataTokens["Namespaces"] = new string[] { "Fly.Framework.Common" };
                    data.DataTokens["UseCustomController"] = "1";
                    data.DataTokens["viewPath"] = string.Format("~{3}/Views/{0}/{1}{2}", controllerName, actionName, ext, (string.IsNullOrWhiteSpace(areaName) ? string.Empty : "/" + areaName));
                    data.Values["action"] = "Common";
                    return data;
                }
            }
            return null;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            VirtualPathData data = base.GetVirtualPath(requestContext, values);
            if (data != null)// && string.IsNullOrWhiteSpace(m_Domain) == false)
            {
                //data.VirtualPath = String.Concat(m_Domain, data.VirtualPath).ToLower();
                if (data.VirtualPath != null)
                {
                    //string p = data.VirtualPath.ToLower().Trim().TrimEnd('/', '\\');
                    string p = data.VirtualPath.Trim().TrimEnd('/', '\\'); // 这里不能直接做ToLower()，因为会导致query string里参数的值也被转小写了，可能导致业务数据错误
                    if (m_CutOutUrlFirstSection)
                    {
                        string firstSection = requestContext.HttpContext.Items["url_first_section"] as string;
                        if (string.IsNullOrWhiteSpace(firstSection) == false)
                        {
                            if (p.Length > 0)
                            {
                                p = string.Concat(firstSection.ToLower(), "/", p);
                            }
                            else
                            {
                                p = firstSection.ToLower();
                            }
                        }
                    }
                    if (p.Length > 0)
                    {
                        int index = p.IndexOf('?');
                        if (index < 0)
                        {
                            data.VirtualPath = (p + "/").ToLower();
                        }
                        else if(index > 0)
                        {
                            string p1 = p.Substring(0, index);
                            p1 = p1.TrimEnd('/', '\\') + "/";
                            string p2 = p.Substring(index);
                            data.VirtualPath = (p1.ToLower() + p2);
                        }
                    }
                }
            }
            return data;
        }

        private class TempControllerFactory : DefaultControllerFactory
        {
            public Type GetTypeOfController(RequestContext requestContext, string controllerName)
            {
                return base.GetControllerType(requestContext, controllerName);
            }
        }
    }
}
