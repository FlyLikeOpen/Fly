using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class DeviceRedirectFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string userAgent = filterContext.HttpContext.Request.UserAgent;
            if (string.IsNullOrWhiteSpace(userAgent)    // http头里的user agent为空的
                || filterContext.HttpContext.Request.BySearchEngine() // 来自搜索引擎的爬虫的请求
                || filterContext.HttpContext.Request.ByApp() // 来自App客户端的请求
                || filterContext.HttpContext.Request.IsAjaxRequest()  // 来自于Ajax的请求
                || string.Equals(filterContext.HttpContext.Request.HttpMethod, "GET", StringComparison.InvariantCultureIgnoreCase) == false // 来自非Get方式的请求（如Post）
            )
            {
                // 不做任何基于设备类型的页面跳转
                return;
            }
            DeviceType? dType = RouteHelper.CurrentSupportedDeviceTypeInConfig;
            if (dType == null) // 没有特别设置当前页面所支持的设备
            {
                return;
            }
            DeviceType curType = DeviceNameDetector.GetDeviceType(userAgent);
            if (curType == dType.Value) // 当前访问设备就是特别设置的支持的设备
            {
                return;
            }
            string url = RouteHelper.RedirectUrlForUnsupportedDevices;
            if (string.IsNullOrWhiteSpace(url)) // 没有设定Redirect到的地址
            {
                return;
            }
            if (url.IndexOf('{') >= 0 && url.IndexOf('}') > 0)
            {
                var rdata = RouteHelper.GetCurrentRouteData();
                var collection = rdata.Values;
                if (collection != null && collection.Count > 0)
                {
                    // 可能配置的跳转目标是一个url模板，那么需要使用当前请求的模板参数的值来应用到跳转到的模板url里
                    foreach (var entry in collection)
                    {
                        url = url.Replace("{" + entry.Key + "}", entry.Value == null ? string.Empty : entry.Value.ToString());
                    }
                }
            }
            if (filterContext.HttpContext.Request.QueryString.Count > 0) // 当前请求带有QueryString参数，那么需要将querystring参数排序后拼接到跳转目标的url上去
            {
                url = url + filterContext.HttpContext.Request.GetSortedQueryString(true);
            }
            filterContext.Result = new RedirectResult(url, false);
        }
    }
}
