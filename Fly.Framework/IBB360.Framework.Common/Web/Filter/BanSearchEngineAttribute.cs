using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class BanSearchEngineAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.BySearchEngine()
                || (
                    filterContext.HttpContext.Request.UserAgent.IndexOf("ie", StringComparison.OrdinalIgnoreCase) < 0
                    && filterContext.HttpContext.Request.UserAgent.IndexOf("microsoft", StringComparison.OrdinalIgnoreCase) < 0
                    && filterContext.HttpContext.Request.UserAgent.IndexOf("safari", StringComparison.OrdinalIgnoreCase) < 0
                    && filterContext.HttpContext.Request.UserAgent.IndexOf("chrome", StringComparison.OrdinalIgnoreCase) < 0
                    && filterContext.HttpContext.Request.UserAgent.IndexOf("android", StringComparison.OrdinalIgnoreCase) < 0
                    && filterContext.HttpContext.Request.UserAgent.IndexOf("windows", StringComparison.OrdinalIgnoreCase) < 0
                    && filterContext.HttpContext.Request.UserAgent.IndexOf("linux", StringComparison.OrdinalIgnoreCase) < 0
                    && filterContext.HttpContext.Request.UserAgent.IndexOf("applewebkit", StringComparison.OrdinalIgnoreCase) < 0
                ))
            {
                filterContext.Result = new ContentResult() {Content = "Resource Not Found", ContentEncoding = Encoding.UTF8, ContentType = "text/html"};
                filterContext.HttpContext.Response.Clear();
                if (filterContext.HttpContext.Response.IsRequestBeingRedirected == false) // 没有Redirect
                {
                    filterContext.HttpContext.Response.StatusCode = 404;
                }
                return;
            }
        }
    }
}
