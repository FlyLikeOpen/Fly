using Fly.Framework.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class AuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (CheckAuthentication() == false) // not logined
            {
                NotAuthenticated(filterContext);
            }
        }

        protected virtual bool CheckAuthentication()
        {
            return ContextManager.Current.HasLogined;
        }

        private void NotAuthenticated(ActionExecutingContext filterContext)
        {
            // check if be Ajax request or normal web page request
            HttpRequestBase request = filterContext.RequestContext.HttpContext.Request;
            if (request.IsAjaxRequest())
            {
                string loginUrl = BuildLoginUrl(request.UrlReferrer.GetAbsoluteUri(), filterContext.HttpContext.Request);
                string acceptType = request.Headers["Accept"];
                string message = "No_Authenticated";
                if (acceptType != null && acceptType.ToLower().Contains("application/xml"))
                {
                    filterContext.Result = new ContentResult
                    {
                        Content = string.Format("<?xml version=\"1.0\"?><result><error>true</error><message>{0}</message><loginUrl>{1}</loginUrl></result>", message, loginUrl),
                        ContentEncoding = Encoding.UTF8,
                        ContentType = "application/xml"
                    };
                }
                else
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = new { error = true, message = message, loginUrl = loginUrl },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
            }
            else
            {
                filterContext.Result = new RedirectResult(BuildLoginUrl(request.Url.GetAbsoluteUri(), filterContext.HttpContext.Request));
            }
        }

        private string BuildLoginUrl(string returnUrl, HttpRequestBase request)
        {
            //return "/login/?returnUrl=" + HttpUtility.UrlEncode(returnUrl);
            string routeName = ConfigurationManager.AppSettings["LoginRouteName"];
            if (string.IsNullOrWhiteSpace(routeName))
            {
                routeName = "Login";
            }
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return RouteHelper.BuildUrl(routeName);
            }
            string x = request.ByWeiXinBrowser() ? HttpUtility.UrlEncode(returnUrl) : returnUrl; // 因为微信会自动解码一次，所以需要编码两次
            var url = RouteHelper.BuildUrl(routeName, new { returnUrl = x });
            return url;
        }
    }
}