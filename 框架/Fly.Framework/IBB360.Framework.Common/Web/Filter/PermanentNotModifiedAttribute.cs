using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.IO;
using System.Web;
using System.Security.Cryptography;

namespace Fly.Framework.Common
{
    public class PermanentNotModifiedAttribute : ActionFilterAttribute
    {
        private static string s_ETag = Guid.NewGuid().ToString();
        
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Filter = new PermanentNotModifiedFilter(filterContext.HttpContext.Response,
                filterContext.RequestContext.HttpContext.Request);
        }

        private class PermanentNotModifiedFilter : MemoryStream
        {
            private HttpResponseBase m_Response = null;
            private HttpRequestBase m_Request;
            private Stream m_Filter = null;

            public PermanentNotModifiedFilter(HttpResponseBase response, HttpRequestBase request)
            {
                m_Response = response;
                m_Request = request;
                m_Filter = response.Filter;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                string clientToken = m_Request.Headers["If-None-Match"];
                if (s_ETag != clientToken)
                {
                    m_Response.AddHeader("ETag", s_ETag);
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
    }
}
