using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.IO;
using System.Web;
using System.Security.Cryptography;
using System.Web.Configuration;

namespace Fly.Framework.Common
{
    public class ContentNotModifiedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Filter = new ContentNotModifiedFilter(filterContext.HttpContext.Response, filterContext.RequestContext.HttpContext.Request);
        }

        private class ContentNotModifiedFilter : MemoryStream
        {
            private HttpResponseBase m_Response = null;
            private HttpRequestBase m_Request;
            private Stream m_Filter = null;
            private bool m_Debug = false;

            public ContentNotModifiedFilter(HttpResponseBase response, HttpRequestBase request)
            {
                SystemWebSectionGroup config = (SystemWebSectionGroup)WebConfigurationManager.OpenWebConfiguration("~/Web.Config").SectionGroups["system.web"];
                CompilationSection cp = config.Compilation;
                m_Debug = cp.Debug;
                m_Response = response;
                m_Request = request;
                m_Filter = response.Filter;
            }

            private string GetToken(Stream stream)
            {
                byte[] checksum = new byte[0];
                checksum = MD5.Create().ComputeHash(stream);
                return Convert.ToBase64String(checksum, 0, checksum.Length);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if(m_Debug)
                {
                    m_Filter.Write(buffer, offset, count);
                    return;
                }
                byte[] data = new byte[count];
                Buffer.BlockCopy(buffer, offset, data, 0, count);
                var token = GetToken(new MemoryStream(data));

                string clientToken = m_Request.Headers["If-None-Match"];

                if (token != clientToken)
                {
                    m_Response.AddHeader("ETag", token);
                    m_Filter.Write(data, 0, count);
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
