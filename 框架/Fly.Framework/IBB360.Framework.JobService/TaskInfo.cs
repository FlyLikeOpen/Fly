using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using IBB360.Framework.JobService.ScheduleManager;

namespace IBB360.Framework.JobService
{
    [Serializable]
    [XmlRoot("taskList")]
    public class TaskList
    {
        [XmlElement("task")]
        public List<TaskInfo> List
        {
            get;
            set;
        }
    }

    [Serializable]
    [XmlRoot("watchFileList")]
    public class WatchFileList
    {
        [XmlElement("relativePath")]
        public List<string> RelativePath
        {
            get;
            set;
        }
    }

    [Serializable]
    [XmlRoot("spParamList")]
    public class SpParamList
    {
        [XmlElement("param")]
        public List<SpParam> SpParams
        {
            get;
            set;
        }
    }

    [Serializable]
    [XmlRoot("param")]
    public class SpParam
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }

    [Serializable]
    [XmlRoot("task")]
    public class TaskInfo
    {
        [XmlAttribute("id")]
        public string ID
        {
            get;
            set;
        }

        [XmlAttribute("pollingIntervalMilliseconds")]
        public int PollingIntervalMilliseconds
        {
            get;
            set;
        }

        [XmlAttribute("scheduler")]
        public string SchedulerID
        {
            get;
            set;
        }

        [XmlAttribute("type")]
        public string Type
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

        [XmlElement("args")]
        public string Args
        {
            get;
            set;
        }

        [XmlElement("open")]
        public string OpenMethodName
        {
            get;
            set;
        }

        [XmlElement("loop")]
        public string LoopMethodName
        {
            get;
            set;
        }

        [XmlElement("close")]
        public string CloseMethodName
        {
            get;
            set;
        }

        [XmlElement("watchFileList")]
        public WatchFileList WatchFileList
        {
            get;
            set;
        }

        [XmlElement("scheduleList")]
        public ScheduleList ScheduleList
        {
            get;
            set;
        }

        [XmlElement("connectionString")]
        public string ConnectionString
        {
            get;
            set;
        }

        [XmlElement("spName")]
        public string SpName
        {
            get;
            set;
        }

        [XmlElement("spParamList")]
        public SpParamList SpParamList
        {
            get;
            set;
        }

        [XmlIgnore]
        public SchedulerInfo SchedulerInfo
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool IsValid
        {
            get
            {
                if (this.IsExe)
                {
                    return this.AssemblyPath != null && this.AssemblyPath.Trim().Length > 0;
                }
                if (this.IsSp)
                {
                    bool t = string.IsNullOrWhiteSpace(ConnectionString) == false && string.IsNullOrWhiteSpace(SpName) == false;
                    if (t == false)
                    {
                        return false;
                    }
                    if (SpParamList != null && SpParamList.SpParams != null && SpParamList.SpParams.Count > 0)
                    {
                        foreach (var param in SpParamList.SpParams)
                        {
                            if (string.IsNullOrWhiteSpace(param.Name) || string.IsNullOrWhiteSpace(param.Type))
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }
                return this.AssemblyPath != null && this.AssemblyPath.Trim().Length > 0
                        && this.TypeFullName != null && this.TypeFullName.Trim().Length > 0
                        && (
                                (this.OpenMethodName != null && this.OpenMethodName.Trim().Length > 0)
                                || (this.LoopMethodName != null && this.LoopMethodName.Trim().Length > 0)
                                || (this.CloseMethodName != null && this.CloseMethodName.Trim().Length > 0)
                            );
            }
        }

        [XmlIgnore]
        public bool IsExe
        {
            get
            {
                return this.Type != null && this.Type.Trim().ToUpper() == "EXE";
            }
        }

        [XmlIgnore]
        public bool IsSp
        {
            get
            {
                return this.Type != null && this.Type.Trim().ToUpper() == "SP";
            }
        }
    }
}
