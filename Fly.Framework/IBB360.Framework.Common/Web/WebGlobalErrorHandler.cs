using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Fly.Framework.Common
{
    public static class WebGlobalErrorHandler
    {
        public static void Handle(HttpApplication app)
        {
            Exception ex = app.Server.GetLastError();
            if (ex != null)
            {
                if (ex is HttpException)
                {
                    var httpError = ex as HttpException;
                    if (httpError != null)
                    {
                        var httpCode = httpError.GetHttpCode();
                        //ASP.NET的400与404错误不记录日志，并都以自定义404页面响应
                        if (httpCode == 400 || httpCode == 404)
                        {
                            app.Response.StatusCode = 404;
                            app.Server.ClearError();
                            return;
                        }
                        Logger.Write("HttpError_" + httpCode, httpError.ToString());
                    }
                }
                //对于路径错误不记录日志，并都以自定义404页面响应
                if (ex.TargetSite.ReflectedType == typeof(System.IO.Path))
                {
                    app.Response.StatusCode = 404;
                    app.Server.ClearError();
                    return;
                }
                Logger.Error(ex);
                app.Response.StatusCode = 500;
                app.Server.ClearError();
            }
        }
    }
}
