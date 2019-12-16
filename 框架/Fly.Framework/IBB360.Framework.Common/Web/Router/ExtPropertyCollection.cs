using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    public class ExtPropertyCollection : ConfigurationElementCollection
    {
        public ExtProperty this[int index]
        {
            get
            {
                return base.BaseGet(index) as ExtProperty;
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

        public new ExtProperty this[string name]
        {
            get
            {
                return base.BaseGet(name) as ExtProperty;
            }
            set
            {
                if (base.BaseGet(name) != null)
                {
                    base.BaseRemove(name);
                }
                var t = new ExtProperty();
                this.BaseAdd(t);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ExtProperty();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ExtProperty)element).Name;
        }
    }
}
