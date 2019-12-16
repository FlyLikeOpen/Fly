using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;

namespace IBB360.Framework.JobService
{
    [Serializable]
    [XmlRoot("schedulerList")]
    public class SchedulerList
    {
        [XmlElement("scheduler")]
        public List<SchedulerInfo> List
        {
            get;
            set;
        }
    }

    [Serializable]
    [XmlRoot("scheduler")]
    public class SchedulerInfo
    {
        [XmlAttribute("id")]
        public string ID
        {
            get;
            set;
        }

        [XmlElement("assembly")]
        public string AssemblyPath
        {
            get;
            set;
        }

        [XmlElement("type")]
        public string TypeFullName
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool IsValid
        {
            get
            {
                return (this.AssemblyPath != null && this.AssemblyPath.Trim().Length > 0
                        && this.TypeFullName != null && this.TypeFullName.Trim().Length > 0);
            }
        }
    }
}
