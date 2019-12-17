using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace Fly.Framework.Common
{
    public abstract class ContextBase<T> : IContext
    {
        private T m_RealContext;
        private CultureInfo m_Culture;

        public ContextBase()
        {
            m_RealContext = RealContext;
            m_Culture = Thread.CurrentThread.CurrentCulture;
        }

        public virtual bool HasLogined
        {
            get { return UserId != Guid.Empty; }
        }

        protected abstract T RealContext { get; }
        protected abstract object GetValueFromRealContext(T context, string key);
        protected abstract void SetValueFromRealContext(T context, string key, object v);
        protected abstract Guid GetUserId(T context);
        protected abstract string GetSessionId(T context);
        protected abstract string GetClientIP(T context);
        protected abstract string GetRequestUserAgent(T context);
        protected abstract DeviceType GetDeviceType(T context);
        protected abstract Geography GetLocation(T context);

        public void Attach(IContext owner)
        {
            ContextBase<T> c = owner as ContextBase<T>;
            if (c == null)
            {
                return;
            }
            Thread.CurrentThread.CurrentCulture = c.m_Culture;
            m_RealContext = c.m_RealContext;
        }

        public Guid UserId
        {
            get { return GetUserId(m_RealContext); }
        }

        public string SessionId
        {
            get { return GetSessionId(m_RealContext); }
        }

        public int SalesChannelNumber
        {
            get
            {
                string t = ConfigurationManager.AppSettings["SalesChannelNumber"];
                if (string.IsNullOrWhiteSpace(t))
                {
                    throw new ApplicationException("没有配置SalesChannelNumber");
                }
                int x;
                if (int.TryParse(t, out x) == false || x <= 0)
                {
                    throw new ApplicationException("配置的SalesChannelNumber有误");
                }
                return x;
            }
        }

        public virtual Guid? SalesAgentId
        {
            get { return null; }
        }

		public virtual Guid? VendorId
		{
			get { return null; }
		}

        public virtual Guid? MerchantId { get { return null; } }

        public string ClientIP
        {
            get { return GetClientIP(m_RealContext); }
        }

        public object this[string key]
        {
            get { return GetValueFromRealContext(m_RealContext, key); }
            set { SetValueFromRealContext(m_RealContext, key, value); }
        }

        public string RequestUserAgent
        {
            get { return GetRequestUserAgent(m_RealContext); }
        }

        public DeviceType DeviceType
        {
            get { return GetDeviceType(m_RealContext); }
        }

        public Geography ClientLocation
        {
            get { return GetLocation(m_RealContext); }
        }
    }
}
