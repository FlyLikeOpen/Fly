﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Fly.Framework.Common
{
    public class Parameter : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired=true, IsKey=true)]
        public string Name
        {
            get
            {
                return this["name"].ToString();
            }
        }

        [ConfigurationProperty("value", IsRequired = false)]
        public string Value
        {
            get
            {
                return this["value"].ToString();
            }
        }

        [ConfigurationProperty("constraint", IsRequired = false)]
        public string Constraint
        {
            get
            {
                return this["constraint"].ToString();
            }
        }
    }
}
