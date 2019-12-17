using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class NoResponseCacheFilter : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // 让每一次的etag都不一样，防止iis级别上的Etag缓存设置
            filterContext.HttpContext.Response.AddHeader("ETag", Guid.NewGuid().ToString());
            // abandon cache
            filterContext.HttpContext.Response.Expires = -1;
            filterContext.HttpContext.Response.ExpiresAbsolute = DateTime.Now.AddSeconds(-1);
            filterContext.HttpContext.Response.CacheControl = "no-cache";
            filterContext.HttpContext.Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
            filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();
        }
    }
}
