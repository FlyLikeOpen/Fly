using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Fly.Framework.Common
{
    public class RoutingItem : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired=true, IsKey=true)]
        public string Name
        {
            get
            {
                return this["name"].ToString();
            }
        }

        [ConfigurationProperty("url", IsRequired=true, IsKey=true)]
        public string Url
        {
            get
            {
                return this["url"].ToString();
            }
        }

        [ConfigurationProperty("controller", IsRequired=false)]
        public string Controller
        {
            get
            {
                return this["controller"].ToString();
            }
        }

        [ConfigurationProperty("action", IsRequired = false)]
        public string Action
        {
            get
            {
                return this["action"].ToString();
            }
        }

        [ConfigurationProperty("domain", IsRequired = false)]
        public string Domain
        {
            get
            {
                return this["domain"].ToString();
            }
        }

        [ConfigurationProperty("needLogin", IsRequired = false, DefaultValue = false)]
        public bool NeedLogin
        {
            get
            {
                if(this["needLogin"] == null)
                {
                    return false;
                }
                return (bool)this["needLogin"];
            }
        }

        [ConfigurationProperty("tryAutoLogin", IsRequired = false, DefaultValue = true)]
        public bool TryAutoLogin
        {
            get
            {
                if (this["tryAutoLogin"] == null)
                {
                    return true;
                }
                return (bool)this["tryAutoLogin"];
            }
        }

        [ConfigurationProperty("authKey", IsRequired = false)]
        public string AuthKey
        {
            get
            {
                if (this["authKey"] == null)
                {
                    return string.Empty;
                }
                return (string)this["authKey"];
            }
        }

        [ConfigurationProperty("trace", IsRequired = false, DefaultValue = true)]
        public bool Trace
        {
            get
            {
                if (this["trace"] == null)
                {
                    return true;
                }
                return (bool)this["trace"];
            }
        }

        [ConfigurationProperty("needSessionId", IsRequired = false, DefaultValue = true)]
        public bool NeedSessionId
        {
            get
            {
                if (this["needSessionId"] == null)
                {
                    return true;
                }
                return (bool)this["needSessionId"];
            }
        }

        [ConfigurationProperty("clientCache", IsRequired = false)]
        public ClientCacheMode ClientCache
        {
            get
            {
                if (this["clientCache"] == null)
                {
                    return ClientCacheMode.Default;
                }
                return (ClientCacheMode)this["clientCache"];
            }
        }

        [ConfigurationProperty("eTag", IsRequired = false)]
        public string ETag
        {
            get
            {
                var obj = this["eTag"];
                if (obj == null)
                {
                    return string.Empty;
                }
                return this["eTag"].ToString();
            }
        }

        [ConfigurationProperty("serverCacheExpiredMinutes", IsRequired = false)]
        public int ServerCacheExpiredMinutes
        {
            get
            {
                if (this["serverCacheExpiredMinutes"] == null)
                {
                    return 0;
                }
                return (int)this["serverCacheExpiredMinutes"];
            }
        }

        [ConfigurationProperty("redirectUrlForOtherDevices", IsRequired = false)]
        public string RedirectUrlForOtherDevices
        {
            get
            {
                var obj = this["redirectUrlForOtherDevices"];
                if (obj == null)
                {
                    return string.Empty;
                }
                return this["redirectUrlForOtherDevices"].ToString();
            }
        }

        [ConfigurationProperty("parameters", IsRequired = false)]
        public ParameterCollection Paramaters
        {
            get
            {
                return this["parameters"] as ParameterCollection;
            }
        }

        [ConfigurationProperty("extProperties", IsRequired = false)]
        public ExtPropertyCollection ExtProperties
        {
            get
            {
                return this["extProperties"] as ExtPropertyCollection;
            }
        }
    }

    public enum ClientCacheMode
    {
        Default = 0,
        None = 1,
        Unchanged304 = 2,
        Permanent304 = 3
    }
}
