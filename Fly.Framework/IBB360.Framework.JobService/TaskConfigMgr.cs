using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Configuration;

namespace IBB360.Framework.JobService
{
    public class TaskConfigMgr
    {
        private string m_ConfigFilePath;

        public string ConfigFilePath
        {
            get { return m_ConfigFilePath; }
        }

        public TaskConfigMgr()
            : this(GetDefaultConfigFilePath())
        {

        }

        public TaskConfigMgr(string configPath)
        {
            if (configPath.IndexOf(':') < 0)
            {
                m_ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, configPath).Trim().ToUpper();
            }
            else
            {
                m_ConfigFilePath = configPath.Trim().ToUpper();
            }
        }

        public List<TaskInfo> GetSetting()
        {
            TaskSetting taskSetting = LoadFromFile<TaskSetting>(m_ConfigFilePath);
            if (taskSetting == null || taskSetting.TaskList == null || taskSetting.TaskList.List == null)
            {
                return new List<TaskInfo>(0);
            }
            if (taskSetting.SchedulerList == null || taskSetting.SchedulerList.List == null)
            {
                taskSetting.SchedulerList = new SchedulerList() { List = new List<SchedulerInfo>(0) };
            }
            foreach (TaskInfo task in taskSetting.TaskList.List)
            {
                string id = task.SchedulerID;
                if (id != null && id.Trim().Length > 0)
                {
                    task.SchedulerInfo = taskSetting.SchedulerList.List.Find(s => s.ID == id);
                }
            }
            return taskSetting.TaskList.List;
        }

        private T LoadFromFile<T>(string filePath) where T : class
        {
            FileStream fs = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return (T)serializer.Deserialize(fs);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
        }

        private static string GetDefaultConfigFilePath()
        {
            string filePath = ConfigurationManager.AppSettings["TaskConfigFile"];
            if (filePath == null || filePath.Trim().Length <= 0)
            {
                return "Job.xml";
            }
            return filePath;
        }
    }

    [XmlRoot("taskSetting")]
    public class TaskSetting
    {
        [XmlElement("taskList")]
        public TaskList TaskList
        {
            get;
            set;
        }

        [XmlElement("schedulerList")]
        public SchedulerList SchedulerList
        {
            get;
            set;
        }
    }
}
