using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Fly.Framework.Common
{
    internal class WebCookiePersister : ICookiePersist
    {
        #region ICookiePersist Members

        public void Save(string cookieName, string cookieValue, Dictionary<string, string> parameters)
        {
            HttpCookie cookie = new HttpCookie(cookieName, cookieValue);
            string domain;
            if (parameters.TryGetValue("domain", out domain) && string.IsNullOrWhiteSpace(domain) == false
                && HttpContext.Current.Request.Url.ToString().Contains(domain) && domain.ToLower() != "localhost")
            {
                cookie.Domain = domain.ToLower();
            }
            string path;
            if (parameters.TryGetValue("path", out path) && string.IsNullOrWhiteSpace(path) == false)
            {
                cookie.Path = path;
            }
            int expires;
            string tmp;
            if (parameters.TryGetValue("expires", out tmp) && tmp != null && int.TryParse(tmp, out expires) && expires > 0)
            {
                cookie.Expires = DateTime.Now.AddMinutes(expires);
            }
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public string Get(string cookieName, Dictionary<string, string> parameters)
        {
            var cookie = HttpContext.Current.Request.Cookies.Get(cookieName);

            return (cookie != null && cookie.Value != null) ? cookie.Value : string.Empty;
        }

        public void Remove(string cookieName, Dictionary<string, string> parameters)
        {
            //if (HttpContext.Current.Request.UserAgent.IndexOf("micromessenger", StringComparison.OrdinalIgnoreCase) > -1) // 在微信环境里
            //{
            //    Save(cookieName, string.Empty, parameters); // 微信浏览器中无法删除或者让cookie过期，只能做一个空字符串的cookie去覆盖原有的，从而达到清除数据的效果
            //    return;
            //}
            //HttpContext.Current.Request.Cookies.Remove(cookieName);
            var cookie = HttpContext.Current.Request.Cookies.Get(cookieName);
            if (cookie != null)
            {
                cookie.Value = string.Empty;
                string domain;
                if (parameters.TryGetValue("domain", out domain) && string.IsNullOrWhiteSpace(domain) == false
                    && HttpContext.Current.Request.Url.ToString().Contains(domain) && domain.ToLower() != "localhost")
                {
                    cookie.Domain = domain.ToLower();
                }
                //cookie.Path = parameters["path"];
                cookie.Expires = DateTime.Now.AddMinutes(-1);
                HttpContext.Current.Response.Cookies.Set(cookie);
            }
        }

        #endregion
    }
}
