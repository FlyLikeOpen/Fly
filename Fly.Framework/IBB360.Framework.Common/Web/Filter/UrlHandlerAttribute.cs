using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class UrlHandlerAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            if (request.IsAjaxRequest() == false // 不是来自ajax的
                && string.Equals(request.HttpMethod, "GET", StringComparison.InvariantCultureIgnoreCase) // 只能是Get的请求
                && request.ByApp() == false // 不是来自手机APP的
                && request.ByWeiXinBrowser() == false // 不是来自WeiXin
                && request.ByWeiBoBrowser() == false // 不是来自手机微博
                && request.ByQQBrowser() == false // 不是来自手机QQ
            )
            {
                if (CheckUrlEndsWithFileName(request.Url) == false // url不是以文件名结尾的
                    && request.Url.GetAbsolutePath().EndsWith("/") == false) // url不是以斜杠/结束的
                {
                    filterContext.Result = new RedirectResult(BuildUrlWithTrailingSlash(request), true);
                    return;
                }
                if (HasUppercaseChar(request.Url.GetAbsolutePath())) // url中含有大写字母的
                {
                    filterContext.Result = new RedirectResult(BuildLowercaseUrl(request), true);
                    return;
                }
                string sortedUrl;
                if (HttpContext.Current.Request.QueryString.Count > 0 // 有QueryString参数
                    && CheckUrlQueryStringSorted(request, out sortedUrl) == false) // QueryString参数顺序不正确
                {
                    filterContext.Result = new RedirectResult(sortedUrl, true);
                    return;
                }
                string configedDomain = GetConfiguredDomain();
                string actualDomain = string.Format("{0}://{1}", request.Url.Scheme, request.Url.Authority);
                if (string.IsNullOrWhiteSpace(configedDomain) == false // 说明有配置domain
                    && string.Equals(configedDomain, actualDomain, StringComparison.InvariantCultureIgnoreCase) == false) // 说明用户访问url里的domain信息和路由配置的domain不一致（包括schema，比如http和https的差别）
                {
                    string realUrl = string.Format("{0}{1}/{2}",
                        configedDomain,
                        request.Url.GetAbsolutePath().TrimEnd('/', '\\').ToLower(),
                        request.GetSortedQueryString(true));
                    filterContext.Result = new RedirectResult(realUrl, true);
                    return;
                }
            }
        }

        // 会在结尾添加/，以及保证url全小写，以及QueryString参数是按key的字母顺序排序
        protected string BuildUrlWithTrailingSlash(HttpRequestBase request)
        {
            return string.Format("{0}://{1}{2}/{3}",
                GetConfiguredSchema(),
                request.Url.Authority,
                request.Url.GetAbsolutePath().TrimEnd('/', '\\').ToLower(),
                request.GetSortedQueryString(true));
        }

        // 保证url全小写，以及QueryString参数是按key的字母顺序排序
        protected string BuildLowercaseUrl(HttpRequestBase request)
        {
            return string.Format("{0}://{1}{2}/{3}",
                GetConfiguredSchema(),
                request.Url.Authority,
                request.Url.GetAbsolutePath().TrimEnd('/', '\\').ToLower(),
                request.GetSortedQueryString(true));
        }

        // 会在结尾添加/，以及保证url全小写，以及QueryString参数是按key的字母顺序排序
        protected bool CheckUrlQueryStringSorted(HttpRequestBase request, out string sortedUrl)
        {
            if (request.QueryString.Count <= 0)
            {
                sortedUrl = string.Format("{0}://{1}{2}/",
                    GetConfiguredSchema(),
                    request.Url.Authority,
                    request.Url.GetAbsolutePath().TrimEnd('/', '\\').ToLower());
                return true;
            }
            StringBuilder orignalQueryString = new StringBuilder();
            orignalQueryString.Append("?");
            int index = 0;
            foreach (string key in request.QueryString.Keys)
            {
                if (index > 0)
                {
                    orignalQueryString.Append("&");
                }
                index++;
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                string v = request.QueryString[key];
                orignalQueryString.AppendFormat("{0}={1}", key.ToLower(), v ?? "");
            }
            string sortedQueryString = request.GetSortedQueryString(true);
            sortedUrl = string.Format("{0}://{1}{2}/{3}",
                GetConfiguredSchema(),
                request.Url.Authority,
                request.Url.GetAbsolutePath().TrimEnd('/', '\\').ToLower(),
                sortedQueryString);
            return string.Equals(orignalQueryString.ToString(), sortedQueryString, StringComparison.InvariantCulture);
        }

        protected string GetConfiguredDomain()
        {
            var config = RouteHelper.GetCurrentConfigRouteItem();
            string domain = config == null ? string.Empty : config.Domain;
            object tmp;
            if (string.IsNullOrWhiteSpace(domain)
                && RouteHelper.TryGetDataTokenOfCurrentRoute("_ibb_domain", out tmp) && tmp != null)
            {
                domain = tmp.ToString();
            }
            if (string.IsNullOrWhiteSpace(domain))
            {
                return string.Empty;
            }
            if (domain.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) == false
                && domain.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) == false)
            {
                return "http://" + domain.Trim('/', '\\');
            }
            return domain.TrimEnd('/', '\\');
        }

        protected string GetConfiguredSchema()
        {
            var config = RouteHelper.GetCurrentConfigRouteItem();
            string domain = config == null ? string.Empty : config.Domain;
            object tmp;
            if (string.IsNullOrWhiteSpace(domain)
                && RouteHelper.TryGetDataTokenOfCurrentRoute("_ibb_domain", out tmp) && tmp != null)
            {
                domain = tmp.ToString();
            }
            if (string.IsNullOrWhiteSpace(domain))
            {
                return "http";
            }
            if (domain.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                return "https";
            }
            return "http";
        }

        protected bool CheckUrlEndsWithFileName(Uri url)
        {
            if (url == null || string.IsNullOrWhiteSpace(url.AbsolutePath) || url.Segments.Length <= 0)
            {
                return false;
            }
            return url.Segments[url.Segments.Length - 1].Contains('.');
        }

        protected bool HasUppercaseChar(string url)
        {
            foreach (var c in url)
            {
                if (c >= 65 && c <= 90)
                {
                    return true;
                }
            }
            return false;
        }

        protected static string[] BubbleSort(string[] r)
        {
            int i, j;
            string temp;
            bool exchange;
            for (i = 0; i < r.Length; i++)
            {
                exchange = false;
                for (j = r.Length - 2; j >= i; j--)
                {
                    if (String.CompareOrdinal(r[j + 1], r[j]) < 0)
                    {
                        temp = r[j + 1];
                        r[j + 1] = r[j];
                        r[j] = temp;
                        exchange = true;
                    }
                }
                if (!exchange)
                {
                    break;
                }
            }
            return r;
        }
    }
}
