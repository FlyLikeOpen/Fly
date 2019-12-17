using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common.Web.Filter
{
    public class PCMobileRedirectionAttribute : ActionFilterAttribute
    {
        private const string BAIDU_SPIDER_KEYWORD = "Baiduspider";
        private const string DISABLE_PC_MOBILE_REDIRECT = "DisablePCMobileRedirect";
        private static string s_MainSiteBaseUrl = ConfigurationManager.AppSettings["MainSiteBaseUrl"].ToLowerInvariant();
        private static string s_MobileSiteBaseUrl = ConfigurationManager.AppSettings["MobileSiteBaseUrl"].ToLowerInvariant();

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            if (request.IsAjaxRequest()
                || request.ByApp()
                || string.IsNullOrWhiteSpace(request.UserAgent)
                || request.UserAgent.IndexOf(BAIDU_SPIDER_KEYWORD, StringComparison.OrdinalIgnoreCase) > -1
                || !string.IsNullOrWhiteSpace(request.Headers[DISABLE_PC_MOBILE_REDIRECT])
                || request.RawUrl.IndexOf("/weixin/", StringComparison.InvariantCultureIgnoreCase) >= 0
                || request.RawUrl.IndexOf("/weibo/", StringComparison.InvariantCultureIgnoreCase) >= 0
            )
            {
                return;
            }

            // redirects between PC and mobile
            string url = request.Url.GetAbsoluteUri().ToLowerInvariant();
            DeviceType deviceType = DeviceNameDetector.GetDeviceType(request.UserAgent);
            if (deviceType == DeviceType.Mobile && !url.StartsWith(s_MobileSiteBaseUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                string redirectUrl = CombineUrls(s_MobileSiteBaseUrl, ResolveNewLocalPathForRedirect(request));
                filterContext.Result = new RedirectResult(redirectUrl);
            }
            else if (deviceType != DeviceType.Mobile && !url.StartsWith(s_MainSiteBaseUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                string redirectUrl = CombineUrls(s_MainSiteBaseUrl, ResolveNewLocalPathForRedirect(request));
                filterContext.Result = new RedirectResult(redirectUrl);
            }
        }

        private static string ResolveNewLocalPathForRedirect(HttpRequestBase request)
        {
            string query = request.Url.Query;
            string localPath = request.Url.GetLocalPath().ToLowerInvariant();
            if (localPath.StartsWith("/cache/") == false)
            {
                return localPath + query;
            }
            string originalLocalPath = request.Headers["X-Original-URL"];
            if (string.IsNullOrWhiteSpace(originalLocalPath) || originalLocalPath.StartsWith("/") == false)
            {
                return "/";
            }
            return originalLocalPath + query;
        }

        private static string CombineUrls(string baseUrl, string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return relativeUrl;
            }
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return baseUrl;
            }
            return string.Format("{0}/{1}", baseUrl.TrimEnd(new char[] { '/', '\\' }), relativeUrl.TrimStart(new char[] { '/', '\\' })).ToLower();
        }
    }
}
