using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Fly.Framework.Common
{
    public class RoutingCollection : ConfigurationElementCollection
    {
        public RoutingItem this[int index]
        {
            get
            {
                return base.BaseGet(index) as RoutingItem;
            }

            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }

                this.BaseAdd(index, value); 
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

        [ConfigurationProperty("device", IsRequired = false)]
        public string Device
        {
            get
            {
                var obj = this["device"];
                if (obj == null)
                {
                    return string.Empty;
                }
                return this["device"].ToString();
            }
        }

        [ConfigurationProperty("domain", IsRequired = false)]
        public string Domain
        {
            get
            {
                var obj = this["domain"];
                if (obj == null)
                {
                    return string.Empty;
                }
                return this["domain"].ToString();
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new RoutingItem();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RoutingItem)element).Name;
        }

        public RoutingCollection()
        {
            this.AddElementName = "route";
        }
    }
}
