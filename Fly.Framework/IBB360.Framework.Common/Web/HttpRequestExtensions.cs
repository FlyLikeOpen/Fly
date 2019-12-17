using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Fly.Framework.Common
{
    public static class HttpRequestExtensions
    {
        private static readonly List<string> s_SearchEngineSpiderIdentifiers = new List<string>
		{
			"bot",
			"spider",
			"crawler",
			"ask",
			"architext",
			"slurp",
			"yahoo",
			"ia_archiver",
			"infoseek",
			"scooter",
			"nutch",
			"wordpress",
			"urllib",
			"pycurl",
			"larbin",
			"acoon",
			"soft framework",
			"EC2LinkFinder",
            "Baiduspider",
			"DNSPod"
		};

        /// <summary>
        /// Is this current request coming from a search engine bot.
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool BySearchEngine(this HttpRequest httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return true;
            }

            string userAgent = httpRequest.UserAgent.ToLowerInvariant();
            foreach (string identifier in s_SearchEngineSpiderIdentifiers)
            {
                if (httpRequest.UserAgent.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) > -1
                    || string.Equals(userAgent, identifier, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Is this current request coming from a search engine bot.
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool BySearchEngine(this HttpRequestBase httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return true;
            }

            string userAgent = httpRequest.UserAgent;
            foreach (string identifier in s_SearchEngineSpiderIdentifiers)
            {
                if (userAgent.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) > -1
                    || string.Equals(userAgent, identifier, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Is this current request using weixin browser
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByWeiXinBrowser(this HttpRequest httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf("micromessenger", StringComparison.OrdinalIgnoreCase) > -1
                && httpRequest.UserAgent.IndexOf("wxwork", StringComparison.OrdinalIgnoreCase) <= -1;
        }

        /// <summary>
        /// Is this current request using weixin browser
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByWeiXinBrowser(this HttpRequestBase httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf("micromessenger", StringComparison.OrdinalIgnoreCase) > -1
                && httpRequest.UserAgent.IndexOf("wxwork", StringComparison.OrdinalIgnoreCase) <= -1;
        }

        public static bool ByWorkWeChatBrowser(this HttpRequest httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf("wxwork", StringComparison.OrdinalIgnoreCase) > -1;
        }

        public static bool ByWorkWeChatBrowser(this HttpRequestBase httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf("wxwork", StringComparison.OrdinalIgnoreCase) > -1;
        }

        public static bool ByWorkWeChatMobileBrowser(this HttpRequestBase httpRequest)
        {
            return httpRequest.ByWorkWeChatBrowser()
                   && httpRequest.UserAgent.IndexOf("Mobile", StringComparison.OrdinalIgnoreCase) > -1;
        }

        /// <summary>
        /// Is this current request using weibo browser
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByWeiBoBrowser(this HttpRequest httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf("weibo", StringComparison.OrdinalIgnoreCase) > -1;
        }

        /// <summary>
        /// Is this current request using weibo browser
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByWeiBoBrowser(this HttpRequestBase httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf("weibo", StringComparison.OrdinalIgnoreCase) > -1;
        }

        /// <summary>
        /// Is this current request using qq browser
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByQQBrowser(this HttpRequest httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf(" QQ/", StringComparison.OrdinalIgnoreCase) > -1;
        }

        /// <summary>
        /// Is this current request using qq browser
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByQQBrowser(this HttpRequestBase httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf(" QQ/", StringComparison.OrdinalIgnoreCase) > -1;
        }


        /// <summary>
        /// Is this current request using qq browser
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByAlipayBrowser(this HttpRequest httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf("AlipayClient/", StringComparison.OrdinalIgnoreCase) > -1;
        }

        /// <summary>
        /// Is this current request using qq browser
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByAlipayBrowser(this HttpRequestBase httpRequest)
        {
            if (httpRequest == null || string.IsNullOrWhiteSpace(httpRequest.UserAgent))
            {
                return false;
            }
            return httpRequest.UserAgent.IndexOf("AlipayClient/", StringComparison.OrdinalIgnoreCase) > -1;
        }

        /// <summary>
        /// Is this current request from Fly's app
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByApp(this HttpRequest httpRequest)
        {
            if (httpRequest == null || httpRequest.Headers == null || httpRequest.Headers.AllKeys == null)
            {
                return false;
            }
            return httpRequest.Headers.AllKeys.Contains(MobileCookie.COOKIE_NAME);
        }

        /// <summary>
        /// Is this current request from Fly's app
        /// </summary>
        /// <param name="httpRequest"></param>
        /// <returns></returns>
        public static bool ByApp(this HttpRequestBase httpRequest)
        {
            if (httpRequest == null || httpRequest.Headers == null || httpRequest.Headers.AllKeys == null)
            {
                return false;
            }
            return httpRequest.Headers.AllKeys.Contains(MobileCookie.COOKIE_NAME);
        }

        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            return ((request["X-Requested-With"] == "XMLHttpRequest") || ((request.Headers != null) && (request.Headers["X-Requested-With"] == "XMLHttpRequest")));
        }

        private static string[] BubbleSort(string[] r)
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

        public static string GetSortedQueryString(this HttpRequestBase request, params string[] removedKeys)
        {
            return GetSortedQueryString(request, true, removedKeys);
        }

        public static string GetSortedQueryString(this HttpRequestBase request, bool beginWithQuestionMark, params string[] removedKeys)
        {
            if (request.QueryString.Count <= 0)
            {
                return string.Empty;
            }
            StringBuilder query = new StringBuilder();
            if (beginWithQuestionMark)
            {
                query.Append("?");
            }
            List<string> keyList = new List<string>(request.QueryString.Keys.Count);
            foreach (string key in request.QueryString.Keys)
            {
                if (string.IsNullOrWhiteSpace(key)
                    || (removedKeys != null && removedKeys.Length > 0 && removedKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase)))
                {
                    continue;
                }
                keyList.Add(key.ToLower());
            }
            string[] keys = BubbleSort(keyList.ToArray());
            int index = 0;
            foreach (var key in keys)
            {
                string v = request.QueryString[key];
                if (index > 0)
                {
                    query.Append("&");
                }
                query.AppendFormat("{0}={1}", key, v ?? "");
                index++;
            }
            if (beginWithQuestionMark && query.Length == 1)
            {
                return string.Empty;
            }
            return query.ToString();
        }

        public static string GetSortedQueryString(this HttpRequest request, params string[] removedKeys)
        {
            return GetSortedQueryString(request, true, removedKeys);
        }

        public static string GetSortedQueryString(this HttpRequest request, bool beginWithQuestionMark, params string[] removedKeys)
        {
            if (request.QueryString.Count <= 0)
            {
                return string.Empty;
            }
            StringBuilder query = new StringBuilder();
            if (beginWithQuestionMark)
            {
                query.Append("?");
            }
            List<string> keyList = new List<string>(request.QueryString.Keys.Count);
            foreach (string key in request.QueryString.Keys)
            {
                if (string.IsNullOrWhiteSpace(key)
                    || (removedKeys != null && removedKeys.Length > 0 && removedKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase)))
                {
                    continue;
                }
                keyList.Add(key.ToLower());
            }
            string[] keys = BubbleSort(keyList.ToArray());
            int index = 0;
            foreach (var key in keys)
            {
                string v = request.QueryString[key];
                if (index > 0)
                {
                    query.Append("&");
                }
                query.AppendFormat("{0}={1}", key, v ?? "");
                index++;
            }
            return query.ToString();
        }

        public static string GetPath(this HttpRequest request)
        {
            string path = request.Path;
            string first = HttpContext.Current.Items["url_first_section"] as string;
            if (string.IsNullOrWhiteSpace(first) == false)
            {
                path = "/" + first + path;
            }
            return path;
        }

        public static string GetRawUrl(this HttpRequest request)
        {
            string path = request.RawUrl;
            string first = HttpContext.Current.Items["url_first_section"] as string;
            if (string.IsNullOrWhiteSpace(first) == false)
            {
                path = "/" + first + path;
            }
            return path;
        }
    }
}
