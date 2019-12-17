using Fly.Framework.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Fly.WebService.MvcFilters
{
    public class HandleExceptionAttribute : HandleErrorAttribute
    {
        protected virtual bool HandleException(Exception ex)
        {
            //if ((ex is BizException) == false
            //    && (ex is NoAuthorizationException) == false
            //    && (ex is NoAuthenticationException) == false
            //    && (ex is ResourceNotFoundException) == false)
            //{
            //    Logger.Error(ex);
            //}
            Logger.Error(ex);
            return true;
        }

        protected virtual string GetExceptionInfo(Exception ex)
        {
            if (ex is BizException)
            {
                return ex.Message;
            }
            return ex.ToString();
        }

        protected virtual bool TrySkipIisCustomErrors
        {
            get { return true; }
        }

        protected virtual ActionResult BuildActionResult(Exception ex)
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
                msg = GetExceptionInfo(ex);
            }
            return new ContentResult() { Content = msg, ContentType = "text/html", ContentEncoding = Encoding.UTF8 };
        }

        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
            {
                return;
            }
            filterContext.Result = BuildActionResult(filterContext.Exception);
            filterContext.HttpContext.Response.Clear();
            filterContext.HttpContext.Response.StatusCode = 500;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = TrySkipIisCustomErrors;
            filterContext.ExceptionHandled = HandleException(filterContext.Exception);
        }
    }
}