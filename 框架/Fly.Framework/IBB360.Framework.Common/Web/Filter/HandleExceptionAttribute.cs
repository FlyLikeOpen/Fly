using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class HandleExceptionAttribute : HandleErrorAttribute
    {
        protected virtual bool HandleException(Exception ex)
        {
            if ((ex is BizException) == false
                && (ex is NoAuthorizationException) == false
                && (ex is NoAuthenticationException) == false
                && (ex is ResourceNotFoundException) == false)
            {
                Logger.Error(ex);
            }
            return true;
        }

        protected virtual string GetExceptionInfo(Exception ex, bool isLocalRequest, bool isHtml = true)
        {
            if (ex is BizException)
            {
                return ex.Message;
            }
            if (isLocalRequest)
            {
                if (isHtml)
                {
                    return ex.ToString().Replace("\r\n", "<br />");
                }
                return ex.ToString();
            }
            else
            {
                return "对不起，网络出错了，请稍后再试。";
            }
        }

        protected virtual ActionResult BuildAjaxJsonActionResult(Exception ex, bool isLocalRequest)
        {
            string msg;
            if(ex is NoAuthenticationException)
            {
                msg = "No_Authenticated";
            }
            else if (ex is NoAuthorizationException)
            {
                msg = "No_Authorized";
            }
            else
            {
                msg = GetExceptionInfo(ex, isLocalRequest, false);
            }
            return new JsonResult
            {
                Data = new
                {
                    error = true,
                    message = msg
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }

        protected virtual ActionResult BuildAjaxHtmlActionResult(Exception ex, bool isLocalRequest)
        {
            string msg;
            if (ex is NoAuthenticationException)
            {
                msg = "No_Authenticated";
            }
            else if (ex is NoAuthorizationException)
            {
                msg = "No_Authorized";
            }
            else
            {
                msg = GetExceptionInfo(ex, isLocalRequest, false);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"service_error_message_panel\">");
            sb.AppendFormat("<input type=\"hidden\" value=\"{0}\" />", HttpUtility.HtmlEncode(msg));
            sb.Append("</div>");
            return new ContentResult
            {
                Content = sb.ToString(),
                ContentEncoding = Encoding.UTF8,
                ContentType = "text/html"
            };
        }

        protected virtual ActionResult BuildAjaxXmlActionResult(Exception ex, bool isLocalRequest)
        {
            string msg;
            if (ex is NoAuthenticationException)
            {
                msg = "No_Authenticated";
            }
            else if (ex is NoAuthorizationException)
            {
                msg = "No_Authorized";
            }
            else
            {
                msg = GetExceptionInfo(ex, isLocalRequest, false);
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<result>");
            sb.AppendLine("<error>true</error>");
            sb.AppendLine("<message>" + msg.Replace("<", "&lt;").Replace(">", "&gt;") + "</message>");
            sb.AppendLine("</result>");
            return new ContentResult
            {
                Content = sb.ToString(),
                ContentEncoding = Encoding.UTF8,
                ContentType = "application/xml"
            };
        }

        protected virtual ActionResult BuildWebPageActionResult(Exception ex, bool isLocalRequest, ExceptionContext filterContext)
        {
            string errorStr;
            if (ex is NoAuthorizationException)
            {
                errorStr = "对不起，您没有操作权限！";
            }
            else
            {
                errorStr = GetExceptionInfo(ex, isLocalRequest);
            }
            Exception exception = new Exception(errorStr);
            string controller = filterContext.RouteData.Values["controller"].ToString();

            string action = null;
            object actObj;
            if(filterContext.RouteData.DataTokens.TryGetValue("real_action", out actObj) && actObj != null)
            {
                action = actObj.ToString().Trim();
            }
            if (string.IsNullOrWhiteSpace(action))
            {
                action = filterContext.RouteData.Values["action"].ToString();
            }
            exception.HelpLink = ex.GetType().FullName;
            HandleErrorInfo model = new HandleErrorInfo(exception, controller, action);
            return new ViewResult
            {
                ViewName = this.View,
                MasterName = this.Master,
                ViewData = new ViewDataDictionary<HandleErrorInfo>(model),
                TempData = filterContext.Controller.TempData
            };
        }

        protected virtual ActionResult BuildResult(Exception ex, ExceptionContext filterContext, out int statusCode)
        {
            HttpRequestBase request = filterContext.RequestContext.HttpContext.Request;
            ActionResult result;
            if (ex is ResourceNotFoundException)
            {
                statusCode = 404;
                return new ViewResult
                {
                    ViewName = "ResourceNotFound"
                };
            }
            if (request.IsAjaxRequest())
            {
                string acceptType = request.Headers["Accept"];
                if (acceptType != null && acceptType.ToLower().Contains("application/xml"))
                {
                    result = BuildAjaxXmlActionResult(ex, request.IsLocal);
                }
                else if (acceptType != null && acceptType.ToLower().Contains("text/html"))
                {
                    result = BuildAjaxHtmlActionResult(ex, request.IsLocal);
                }
                else
                {
                    result = BuildAjaxJsonActionResult(ex, request.IsLocal);
                }
                statusCode = 200;
            }
            else
            {
                if (ex is NoAuthenticationException)
                {
                    result = new RedirectResult(BuildLoginUrl(HttpUtility.UrlEncode(request.Url.GetAbsoluteUri())));
                    statusCode = 200;
                }
                else
                {
                    result = BuildWebPageActionResult(ex, request.IsLocal, filterContext);
                    statusCode = ex is BizException ? 200 : 500;
                }
            }
            return result;
        }

        private string BuildLoginUrl(string returnUrl)
        {
            //return "/login/?returnUrl=" + HttpUtility.UrlEncode(returnUrl);
            string routeName = ConfigurationManager.AppSettings["LoginRouteName"];
            if (string.IsNullOrWhiteSpace(routeName))
            {
                routeName = "Login";
            }
            return RouteHelper.BuildUrl(routeName, new { returnUrl = HttpUtility.UrlEncode(returnUrl) });
        }

        protected virtual bool TrySkipIisCustomErrors
        {
            get { return true; }
        }

        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
            {
                return;
            }
            int statusCode;
            filterContext.Result = BuildResult(filterContext.Exception, filterContext, out statusCode);
            filterContext.HttpContext.Response.Clear();
            if (filterContext.HttpContext.Response.IsRequestBeingRedirected == false) // 没有Redirect
            {
                filterContext.HttpContext.Response.StatusCode = statusCode;
            }
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = TrySkipIisCustomErrors;
            filterContext.ExceptionHandled = HandleException(filterContext.Exception);
        }
    }
}
