using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class RequestTraceAttribute : ActionFilterAttribute
    {
        private class TraceInfo
        {
            private DateTime m_StartTime;
            private Stopwatch m_Watch;
            private Stopwatch m_WatchView;
            private long m_ElapsedMilliseconds;
            private long m_ActionElapsedMilliseconds;
            private long m_ViewElapsedMilliseconds;
            private bool m_HasException = false;

            private static string s_ServerIP;
            private static string s_AppId;
            private static string GetServerIP() // 不需要加锁，因为即使多线程时发生了并发冲突也不会有问题，加锁反而带来更大的性能开销
            {
                if (s_ServerIP == null)
                {
                    IPAddress ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(i => i.AddressFamily == AddressFamily.InterNetwork);
                    s_ServerIP = ip.ToString();
                }
                return s_ServerIP;
            }

            private static string GetAppId()
            {
                if (s_AppId == null)
                {
                    string id = ConfigurationManager.AppSettings["AppId"];
                    s_AppId = string.IsNullOrWhiteSpace(id) ? "Unknown" : id.Trim();
                    if (s_AppId.Length > 16)
                    {
                        s_AppId = s_AppId.Substring(0, 16);
                    }
                }
                return s_AppId;
            }

            public void Start()
            {
                m_StartTime = DateTime.Now;
                m_Watch = Stopwatch.StartNew();
            }

            public void ActionEnd(bool hasException)
            {
                m_ActionElapsedMilliseconds = m_Watch.ElapsedMilliseconds;
                m_HasException = m_HasException || hasException;
            }

            public void ViewBegin()
            {
                m_WatchView = Stopwatch.StartNew();
            }

            public void Stop(bool hasException)
            {
                m_WatchView.Stop();
                m_Watch.Stop();
                m_ViewElapsedMilliseconds = m_WatchView.ElapsedMilliseconds;
                m_ElapsedMilliseconds = m_Watch.ElapsedMilliseconds;
                m_HasException = m_HasException || hasException;
            }

            public RequestTraceMessage ToMessage(HttpContextBase httpContext, string url, string urlReferrer, string domainReferrer, string clientType)
            {
                IContext cxt = ContextManager.Current;
                bool ajax = httpContext.Request.IsAjaxRequest();
                return new RequestTraceMessage()
                {
                    AppServerIP = GetServerIP(),
                    AppId = GetAppId(),
                    RequestRawUrl = url,
                    RequestUrl = string.IsNullOrWhiteSpace(url) ? url : url.Split('?')[0],
                    UserId = cxt.UserId,
                    SessionId = cxt.SessionId,
                    UserAgent = cxt.RequestUserAgent,
                    DeviceType = cxt.DeviceType,
                    ClientIP = cxt.ClientIP,
                    Latitude = null, //cxt.ClientLocation.Latitude == 0 ? null : new float?(cxt.ClientLocation.Latitude),
                    Longitude = null, //cxt.ClientLocation.Longitude == 0 ? null : new float?(cxt.ClientLocation.Longitude),
                    RequestStartTime = m_StartTime,
                    IsAjax = ajax,
                    ElapsedSecond = (double)m_ElapsedMilliseconds / 1000d,
                    ClientType = clientType,
                    UrlReferrer = urlReferrer,
                    DomainReferrer = domainReferrer,
                    ActionElapsedSecond = (double)m_ActionElapsedMilliseconds / 1000d,
                    ViewElapsedSecond = (double)m_ViewElapsedMilliseconds / 1000d,
                    HasException = m_HasException
                };
            }
        }

        private static bool? s_Disabled;
        private static bool Disabled
        {
            get
            {
                if (s_Disabled == null)
                {
                    string str = ConfigurationManager.AppSettings["RequestTrace_Disabled"];
                    bool dis;
                    if (string.IsNullOrWhiteSpace(str) == false && bool.TryParse(str, out dis))
                    {
                        s_Disabled = dis;
                    }
                    else
                    {
                        s_Disabled = false;
                    }
                }
                return s_Disabled.Value;
            }
        }

        protected virtual bool CheckNeedTrace(HttpContextBase httpContext)
        {
            return Disabled == false && string.IsNullOrWhiteSpace(ContextManager.Current.SessionId) == false && httpContext.Request.BySearchEngine() == false;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (CheckNeedTrace(filterContext.HttpContext) == false)
            {
                return;
            }
            TraceInfo info = new TraceInfo();
            filterContext.RequestContext.HttpContext.Items["ibb360_RequestTrace_6742"] = info;
            info.Start();
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (CheckNeedTrace(filterContext.HttpContext) == false)
            {
                return;
            }
            TraceInfo info = filterContext.RequestContext.HttpContext.Items["ibb360_RequestTrace_6742"] as TraceInfo;
            if (info != null)
            {
                info.ActionEnd(filterContext.Exception != null && (filterContext.Exception is BizException) == false);
            }
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (CheckNeedTrace(filterContext.HttpContext) == false)
            {
                return;
            }
            TraceInfo info = filterContext.RequestContext.HttpContext.Items["ibb360_RequestTrace_6742"] as TraceInfo;
            if (info != null)
            {
                info.ViewBegin();
            }
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext.HttpContext.Response.IsRequestBeingRedirected)
            {
                return;
            }
            if (CheckNeedTrace(filterContext.HttpContext) == false)
            {
                return;
            }
            TraceInfo info = filterContext.RequestContext.HttpContext.Items["ibb360_RequestTrace_6742"] as TraceInfo;
            if (info != null)
            {
                info.Stop(filterContext.Exception != null && (filterContext.Exception is BizException) == false);
                string urlReferrer = filterContext.RequestContext.HttpContext.Request.UrlReferrer != null ? filterContext.RequestContext.HttpContext.Request.UrlReferrer.ToString() : null;
                string domainReferrer = filterContext.RequestContext.HttpContext.Request.UrlReferrer != null ? filterContext.RequestContext.HttpContext.Request.UrlReferrer.Host : null;
                string url = filterContext.RequestContext.HttpContext.Request.RawUrl;
                string clientType = DetectRequestClientType(filterContext.HttpContext);
                if (string.IsNullOrWhiteSpace(clientType))
                {
                    clientType = "OT";
                }
                RequestTraceMessage msg = info.ToMessage(filterContext.HttpContext, url, urlReferrer, domainReferrer, clientType);
                SetExtentionData(filterContext.HttpContext, msg.ExtentionData);
                TaskPool.Instance.Enqueue(msg);
            }
        }

        protected virtual void SetExtentionData(HttpContextBase httpContext, NameValueCollection extentionData)
        {

        }

        protected virtual string DetectRequestClientType(HttpContextBase httpContext)
        {
            string clientType = "OT";
            if (httpContext.Request.ByApp())
            {
                clientType = "AP";
            }
            else if (httpContext.Request.ByQQBrowser())
            {
                clientType = "QQ";
            }
            else if (httpContext.Request.BySearchEngine())
            {
                clientType = "SE";
            }
            else if (httpContext.Request.ByWeiBoBrowser())
            {
                clientType = "WB";
            }
            else if (httpContext.Request.ByWeiXinBrowser())
            {
                clientType = "WX";
            }
            else if (httpContext.Request.ByAlipayBrowser())
            {
                clientType = "AL";
            }
            return clientType;
        }
    }

    public class RequestTraceMessage
    {
        public string AppServerIP { get; set; }

        public string AppId { get; set; }

        public string RequestUrl { get; set; }

        public string RequestRawUrl { get; set; }

        public Guid? UserId { get; set; }

        public string SessionId { get; set; }

        public string UserAgent { get; set; }

        public DeviceType DeviceType { get; set; }

        public string ClientIP { get; set; }

        public float? Latitude { get; set; }

        public float? Longitude { get; set; }

        public DateTime RequestStartTime { get; set; }

        public double ElapsedSecond { get; set; }

        public bool IsAjax { get; set; }

        public string ClientType { get; set; }

        // ---- New add

        public string UrlReferrer { get; set; }

        public string DomainReferrer { get; set; }

        public double ActionElapsedSecond { get; set; }

        public double ViewElapsedSecond { get; set; }

        public bool HasException { get; set; }

		private NameValueCollection _extentionData;

		public NameValueCollection ExtentionData
		{
			get
			{
				if (_extentionData == null)
				{
					_extentionData = new NameValueCollection();
				}
				return _extentionData;
			}
		}

        //// 下面的属性都是项目级的业务属性，重构代码时需要都去除掉，改用ExtentionData来装载即可
        //// ---- New add 2

        //public string TrackCode { get; set; }

        //public int? CampaignIdNumber { get; set; }

        //public int? WeiXinAcctId { get; set; }

        //public string WeiXinOpenId { get; set; }

        //public string WeiXinUnionId { get; set; }
    }
}
