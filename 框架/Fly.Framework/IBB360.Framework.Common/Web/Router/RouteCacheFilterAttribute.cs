using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class RouteCacheFilterAttribute : ActionFilterAttribute
    {
        private static string s_ETag = Guid.NewGuid().ToString();

        private class CacheItem
        {
            public List<byte> Html { get; set; }

            public string ETag { get; set; }
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var item = RouteHelper.GetCurrentConfigRouteItem();
            if (item.ServerCacheExpiredMinutes > 0)
            {
                string key = GenerateServerCacheKey(filterContext.HttpContext);
                CacheItem citem = Cache.GetLocalCache(key) as CacheItem;
                if (citem != null)
                {
                    string c = "1";
                    using(var stream = new MemoryStream(citem.Html.ToArray()))
                    {
                        using(var sr = new StreamReader(stream))
                        {
                            c = sr.ReadToEnd();
                        }
                    }
                    filterContext.HttpContext.Items["RouteCacheFilterAttribute.citem"] = citem;
                    filterContext.HttpContext.Items["RouteCacheFilterAttribute.FindItem"] = "1";
                    filterContext.Result = new ContentResult() { Content = c }; // 避免再执行Action和View的Render
                    return;
                }
            }
            if (item.ClientCache == ClientCacheMode.Permanent304)
            {
                string etag = item.ETag;
                if (string.IsNullOrWhiteSpace(etag))
                {
                    etag = s_ETag;
                }
                string clientToken = filterContext.HttpContext.Request.Headers["If-None-Match"];
                if (etag == clientToken)
                {
                    filterContext.Result = new ContentResult() { Content="1" }; // 避免再执行Action和View的Render
                    return;
                }
            }
            base.OnActionExecuting(filterContext);
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var item = RouteHelper.GetCurrentConfigRouteItem();
            CacheItem citem = null;
            if (item.ServerCacheExpiredMinutes > 0) // 需要服务端缓存
            {
                citem = filterContext.HttpContext.Items["RouteCacheFilterAttribute.citem"] as CacheItem;
            }
            if(item == null || item.ClientCache == ClientCacheMode.Default)
            {
                filterContext.HttpContext.Response.Filter = new ServerCacheFilter(filterContext.HttpContext, item.ServerCacheExpiredMinutes, citem != null);
                return;
            }
            if (item.ClientCache == ClientCacheMode.None)
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
                filterContext.HttpContext.Response.Filter = new ServerCacheFilter(filterContext.HttpContext, item.ServerCacheExpiredMinutes, citem != null);
            }
            else if (item.ClientCache == ClientCacheMode.Permanent304)
            {
                string etag = item.ETag;
                if (string.IsNullOrWhiteSpace(etag))
                {
                    etag = s_ETag;
                }
                filterContext.HttpContext.Response.Filter = new PermanentNotModifiedFilter(filterContext.HttpContext,
                    etag, item.ServerCacheExpiredMinutes, citem != null);
            }
            else if (item.ClientCache == ClientCacheMode.Unchanged304)
            {
                filterContext.HttpContext.Response.Filter = new ContentNotModifiedFilter(filterContext.HttpContext, item.ServerCacheExpiredMinutes, citem != null);
            }
            else
            {
                filterContext.HttpContext.Response.Filter = new ServerCacheFilter(filterContext.HttpContext, item.ServerCacheExpiredMinutes, citem != null);
            }
        }

        private static string GenerateServerCacheKey(HttpContextBase httpContext)
        {
            StringBuilder url = new StringBuilder();
            url.Append(httpContext.Request.Url.GetAbsolutePath().ToLower().Trim());
            if (httpContext.Request.QueryString.Keys.Count > 0)
            {
                url.Append(httpContext.Request.GetSortedQueryString(true, "sessionid"));
            }
            return url.ToString();
        }

        private class ServerCacheFilter : MemoryStream
        {
            protected HttpResponseBase m_Response = null;
            protected HttpRequestBase m_Request;
            protected HttpContextBase m_HttpContext;
            private Stream m_Filter = null;
            private bool m_HasCacheItem;
            private int m_ServerCacheExpiredMinutes;

            public ServerCacheFilter(HttpContextBase httpContext, int serverCacheExpiredMinutes, bool hasCacheItem)
            {
                m_HttpContext = httpContext;
                m_Response = httpContext.Response;
                m_Request = httpContext.Request;
                m_Filter = m_Response.Filter;
                m_ServerCacheExpiredMinutes = serverCacheExpiredMinutes;
                m_HasCacheItem = hasCacheItem;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (m_Response.StatusCode != 200) // 比如301或302跳转的
                {
                    m_Filter.Write(buffer, offset, count);
                    return;
                }
                var item = RouteHelper.GetCurrentConfigRouteItem();
                if (item.ServerCacheExpiredMinutes > 0 && m_HasCacheItem == false) // 需要服务器端缓存，但还没有缓存
                {
                    var citem = m_HttpContext.Items["RouteCacheFilterAttribute.citem"] as CacheItem;
                    if (citem == null) // 这样做目的是为了确保Write方法可能被多次调用时不会出错
                    {
                        citem = new CacheItem { Html = new List<byte>(count) };
                        for (int i = offset; i < count; i++)
                        {
                            citem.Html.Add(buffer[i]);
                        }
                        string key = GenerateServerCacheKey(m_HttpContext);
                        m_HttpContext.Items["RouteCacheFilterAttribute.citem"] = citem;
                        Cache.SetLocalCache(key, citem, true, item.ServerCacheExpiredMinutes); // 此时将缓存数据放入全局Cache中，可以被其他访问请求看到和读取了
                    }
                    else
                    {
                        for (int i = offset; i < count; i++)
                        {
                            citem.Html.Add(buffer[i]);
                        }
                    }
                }
                m_Filter.Write(buffer, offset, count);
            }
        }

        private class PermanentNotModifiedFilter : MemoryStream
        {
            private HttpResponseBase m_Response = null;
            private HttpRequestBase m_Request;
            private HttpContextBase m_HttpContext;
            private Stream m_Filter = null;
            private string m_ETag;
            private int m_ServerCacheExpiredMinutes;
            private bool m_HasCacheItem;

            public PermanentNotModifiedFilter(HttpContextBase httpContext, string etag, int serverCacheExpiredMinutes, bool hasCacheItem)
            {
                m_HttpContext = httpContext;
                m_Response = httpContext.Response;
                m_Request = httpContext.Request;
                m_Filter = m_Response.Filter;
                m_ETag = etag;
                m_ServerCacheExpiredMinutes = serverCacheExpiredMinutes;
                m_HasCacheItem = hasCacheItem;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (m_Response.StatusCode != 200) // 比如301或302跳转的
                {
                    m_Filter.Write(buffer, offset, count);
                    return;
                }
                string clientToken = m_Request.Headers["If-None-Match"];
                if (m_ETag != clientToken)
                {
                    var item = RouteHelper.GetCurrentConfigRouteItem();
                    if (item.ServerCacheExpiredMinutes > 0 && m_HasCacheItem == false) // 需要服务器端缓存，但还没有缓存
                    {
                        var citem = m_HttpContext.Items["RouteCacheFilterAttribute.citem"] as CacheItem;
                        if (citem == null) // 这样做目的是为了确保Write方法可能被多次调用时不会出错
                        {
                            citem = new CacheItem { Html = new List<byte>(count) };
                            for (int i = offset; i < count; i++)
                            {
                                citem.Html.Add(buffer[i]);
                            }
                            string key = GenerateServerCacheKey(m_HttpContext);
                            m_HttpContext.Items["RouteCacheFilterAttribute.citem"] = citem;
                            Cache.SetLocalCache(key, citem, true, item.ServerCacheExpiredMinutes); // 此时将缓存数据放入全局Cache中，可以被其他访问请求看到和读取了
                        }
                        else
                        {
                            for (int i = offset; i < count; i++)
                            {
                                citem.Html.Add(buffer[i]);
                            }
                        }
                    }
                    m_Response.AddHeader("ETag", m_ETag);
                    m_Filter.Write(buffer, offset, count);
                }
                else
                {
                    m_Response.SuppressContent = true;
                    m_Response.StatusCode = 304;
                    m_Response.StatusDescription = "Not Modified";
                    //m_Response.AddHeader("Content-Length", "0");
                }
            }
        }

        private class ContentNotModifiedFilter : MemoryStream
        {
            private HttpResponseBase m_Response;
            private HttpRequestBase m_Request;
            private HttpContextBase m_HttpContext;
            private Stream m_Filter = null;
            private bool m_Debug = false;
            private int m_ServerCacheExpiredMinutes;
            private bool m_HasCacheItem;

            public ContentNotModifiedFilter(HttpContextBase httpContext, int serverCacheExpiredMinutes, bool hasCacheItem)
            {
                SystemWebSectionGroup config = (SystemWebSectionGroup)WebConfigurationManager.OpenWebConfiguration("~/Web.Config").SectionGroups["system.web"];
                CompilationSection cp = config.Compilation;
                m_Debug = cp.Debug;
                m_HttpContext = httpContext;
                m_Response = httpContext.Response;
                m_Request = httpContext.Request;
                m_Filter = m_Response.Filter;
                m_ServerCacheExpiredMinutes = serverCacheExpiredMinutes;
                m_HasCacheItem = hasCacheItem;
            }

            private string GetToken(Stream stream)
            {
                byte[] checksum = new byte[0];
                checksum = MD5.Create().ComputeHash(stream);
                return Convert.ToBase64String(checksum, 0, checksum.Length);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (m_Response.StatusCode != 200) // 比如301或302跳转的
                {
                    m_Filter.Write(buffer, offset, count);
                    return;
                }
                var item = RouteHelper.GetCurrentConfigRouteItem();
                CacheItem citem = null;
                if (item.ServerCacheExpiredMinutes > 0 && m_HasCacheItem == false) // 需要服务器端缓存，但还没有缓存
                {
                    citem = m_HttpContext.Items["RouteCacheFilterAttribute.citem"] as CacheItem;
                    if (citem == null) // 这样做目的是为了确保Write方法可能被多次调用时不会出错
                    {
                        citem = new CacheItem { Html = new List<byte>(count) };
                        for (int i = offset; i < count; i++)
                        {
                            citem.Html.Add(buffer[i]);
                        }
                        citem.ETag = GetToken(new MemoryStream(buffer));
                        string key = GenerateServerCacheKey(m_HttpContext);
                        m_HttpContext.Items["RouteCacheFilterAttribute.citem"] = citem;
                        Cache.SetLocalCache(key, citem, true, item.ServerCacheExpiredMinutes); // 此时将缓存数据放入全局Cache中，可以被其他访问请求看到和读取了
                    }
                    else
                    {
                        for (int i = offset; i < count; i++)
                        {
                            citem.Html.Add(buffer[i]);
                        }
                        citem.ETag = GetToken(new MemoryStream(citem.Html.ToArray()));
                    }
                }
                if (m_Debug)
                {
                    m_Filter.Write(buffer, offset, count);
                    return;
                }
                //byte[] data = new byte[count];
                //Buffer.BlockCopy(buffer, offset, data, 0, count);

                string token;
                citem = m_HttpContext.Items["RouteCacheFilterAttribute.citem"] as CacheItem;
                if (citem == null)
                {
                    token = GetToken(new MemoryStream(buffer));
                }
                else
                {
                    token = citem.ETag;
                }
                string clientToken = m_Request.Headers["If-None-Match"];
                if (token != clientToken)
                {
                    m_Response.AddHeader("ETag", token);
                    m_Filter.Write(buffer, 0, count);
                }
                else
                {
                    m_Response.SuppressContent = true;
                    m_Response.StatusCode = 304;
                    m_Response.StatusDescription = "Not Modified";
                    //m_Response.AddHeader("Content-Length", "0");
                }
            }
        }
    }
}
