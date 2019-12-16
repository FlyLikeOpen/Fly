using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBB360.Framework.JobService
{
    public delegate void ExceptionEventHandler(object sender, ExceptionEventArgs e);

    [Serializable]
    public class ExceptionEventArgs : ExecutedNotifyEventArgs
    {
        public string Exception
        {
            get;
            private set;
        }

        public ExceptionEventArgs(Exception ex, string folder, string taskID, string desc)
        {
            Exception = ex.ToString();
            Folder = folder;
            TaskID = taskID;
            Description = desc;
        }

        public ExceptionEventArgs(string exMessage, string folder, string taskID, string desc)
        {
            Exception = exMessage;
            Folder = folder;
            TaskID = taskID;
            Description = desc;
        }
    }

    [Serializable]
    public class TaskEventArgs : EventArgs
    {
        public string TaskID
        {
            get;
            private set;
        }

        public string Folder
        {
            get;
            private set;
        }

        public TaskEventArgs(string folder, string taskID)
        {
            Folder = folder;
            TaskID = taskID;
        }
    }

    [Serializable]
    public class ExecutedNotifyEventArgs : EventArgs
    {
        public string TaskID
        {
            get;
            set;
        }

        public string Folder
        {
            get;
            set;
        }

        public DateTime? LastExecuteTime
        {
            get;
            set;
        }

        public DateTime? NextExecuteTime
        {
            get;
            set;
        }

        public DateTime? ExecuteStartTime
        {
            get;
            set;
        }

        public double ExecuteDuration
        {
            get;
            set;
        }

        public ExecuteResultEnum? ExecuteResult
        {
            get;
            set;
        }

        public string ExecutePath
        {
            get;
            set;
        }

        public string ServerName
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }
    }

    [Serializable]
    public enum ExecuteResultEnum
    {
        Successed = 1,
        Failed = 2,
        Cancelled = 3,
    }
}
