using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace IBB360.Framework.JobService
{
    public class TaskExecutionHostMain : IDisposable
    {
        private const string CONFIG_FILE_NAME = "JOB.XML";
        private List<TaskExecutionHost> m_TaskExecutionHostList;
        private static object s_SyncObj = new object();
        private TaskFolderWatcher m_Watcher;

        #region UnhandledException事件

        private event ExceptionEventHandler m_UnhandledException;
        public event ExceptionEventHandler UnhandledException
        {
            add
            {
                m_UnhandledException += value;
            }
            remove
            {
                m_UnhandledException -= value;
            }
        }

        private void OnUnhandledException(object sender, ExceptionEventArgs args)
        {
            ExceptionEventHandler handler = m_UnhandledException;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        private void OnUnhandledException(Exception ex, string folder, string taskID, string desc)
        {
            ExceptionEventHandler handler = m_UnhandledException;
            if (handler != null)
            {
                handler(this, new ExceptionEventArgs(ex, folder, taskID, desc));
            }
        }

        #endregion        

        #region ExecutedNotify事件

        private event EventHandler<ExecutedNotifyEventArgs> m_ExecutedNotify;
        public event EventHandler<ExecutedNotifyEventArgs> ExecutedNotify
        {
            add
            {
                m_ExecutedNotify += value;
            }
            remove
            {
                m_ExecutedNotify -= value;
            }
        }

        protected virtual void OnExecutedNotify(object sender, ExecutedNotifyEventArgs args)
        {
            EventHandler<ExecutedNotifyEventArgs> handler = m_ExecutedNotify;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        #endregion        

        #region Task Event

        private event EventHandler<TaskEventArgs> m_EachTaskStarting;
        public event EventHandler<TaskEventArgs> EachTaskStarting
        {
            add
            {
                m_EachTaskStarting += value;
            }
            remove
            {
                m_EachTaskStarting -= value;
            }
        }
        private void OnEachTaskStarting(object sender, TaskEventArgs args)
        {
            EventHandler<TaskEventArgs> handler = m_EachTaskStarting;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        private event EventHandler<TaskEventArgs> m_EachTaskStarted;
        public event EventHandler<TaskEventArgs> EachTaskStarted
        {
            add
            {
                m_EachTaskStarted += value;
            }
            remove
            {
                m_EachTaskStarted -= value;
            }
        }
        private void OnEachTaskStarted(object sender, TaskEventArgs args)
        {
            EventHandler<TaskEventArgs> handler = m_EachTaskStarted;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        private event EventHandler<TaskEventArgs> m_EachTaskStopping;
        public event EventHandler<TaskEventArgs> EachTaskStopping
        {
            add
            {
                m_EachTaskStopping += value;
            }
            remove
            {
                m_EachTaskStopping -= value;
            }
        }
        private void OnEachTaskStopping(object sender, TaskEventArgs args)
        {
            EventHandler<TaskEventArgs> handler = m_EachTaskStopping;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        private event EventHandler<TaskEventArgs> m_EachTaskStopped;
        public event EventHandler<TaskEventArgs> EachTaskStopped
        {
            add
            {
                m_EachTaskStopped += value;
            }
            remove
            {
                m_EachTaskStopped -= value;
            }
        }
        private void OnEachTaskStopped(object sender, TaskEventArgs args)
        {
            EventHandler<TaskEventArgs> handler = m_EachTaskStopped;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        private event EventHandler<TaskEventArgs> m_EachTaskRestarting;
        public event EventHandler<TaskEventArgs> EachTaskRestarting
        {
            add
            {
                m_EachTaskRestarting += value;
            }
            remove
            {
                m_EachTaskRestarting -= value;
            }
        }
        private void OnEachTaskRestarting(object sender, TaskEventArgs args)
        {
            EventHandler<TaskEventArgs> handler = m_EachTaskRestarting;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        private event EventHandler<TaskEventArgs> m_EachTaskRestarted;
        public event EventHandler<TaskEventArgs> EachTaskRestarted
        {
            add
            {
                m_EachTaskRestarted += value;
            }
            remove
            {
                m_EachTaskRestarted -= value;
            }
        }
        private void OnEachTaskRestarted(object sender, TaskEventArgs args)
        {
            EventHandler<TaskEventArgs> handler = m_EachTaskRestarted;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        #endregion

        public TaskExecutionHostMain()
        {
            string[] arry = Directory.GetDirectories(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "*.*", SearchOption.TopDirectoryOnly);
            List<string> taskFolder = new List<string>(arry.Length);
            foreach (string folder in arry)
            {
                if (File.Exists(Path.Combine(folder, CONFIG_FILE_NAME)))
                {
                    taskFolder.Add(folder);
                }
            }
            m_TaskExecutionHostList = new List<TaskExecutionHost>(taskFolder.Count);
            foreach (string folder in taskFolder)
            {
                try
                {
                    TaskExecutionHost tmp = new TaskExecutionHost(Path.Combine(folder, CONFIG_FILE_NAME));
                    tmp.UnhandledException += (s, e) => { OnUnhandledException(s, e); };
                    tmp.ExecutedNotify += (s, e) => { OnExecutedNotify(s, e); };
                    tmp.EachTaskStarted += (s, e) => { OnEachTaskStarted(s, e); };
                    tmp.EachTaskStarting += (s, e) => { OnEachTaskStarting(s, e); };
                    tmp.EachTaskStopped += (s, e) => { OnEachTaskStopped(s, e); };
                    tmp.EachTaskStopping += (s, e) => { OnEachTaskStopping(s, e); };
                    tmp.EachTaskRestarted += (s, e) => { OnEachTaskRestarted(s, e); };
                    tmp.EachTaskRestarting += (s, e) => { OnEachTaskRestarting(s, e); };
                    m_TaskExecutionHostList.Add(tmp);
                }
                catch (Exception ex)
                {
                    var e = new ExceptionEventArgs(ex, folder, "", "启动整个目录里的服务时出现异常，请确认任务配置文件是否正确！");
                    Logger.Error(e);
                    try
                    {
                        OnUnhandledException(this, e);
                    }
                    catch (Exception ex1)
                    {
                        Logger.Error(folder, "", ex1, "执行TaskExecutionHostMain.OnUnhandledException时出错");
                    }
                }
            }
            m_Watcher = new TaskFolderWatcher(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, CONFIG_FILE_NAME, NewTaskCreated, TaskDeleted, TaskRenamed);
        }

        public void Open()
        {
            foreach (TaskExecutionHost host in m_TaskExecutionHostList)
            {
                try
                {
                    host.Open();
                }
                catch(Exception ex)
                {
                    OnUnhandledException(ex, host.FolderPath, "TaskExecutionHost", "TaskExecutionHost在执行Open方法时出错");
                }
            }
            m_Watcher.StartToWatch();
        }

        public void Close()
        {
            m_Watcher.StopToWatch();
            foreach (TaskExecutionHost host in m_TaskExecutionHostList)
            {
                try
                {
                    host.Close();
                }
                catch (Exception ex)
                {
                    OnUnhandledException(ex, host.FolderPath, "TaskExecutionHost", "TaskExecutionHost在执行Close方法时出错");
                }
            }
        }

        public void Dispose()
        {
            Close();
        }

        private void NewTaskCreated(string configPath)
        {
            TaskExecutionHost tmp = null;
            try
            {
                string folder = Path.GetDirectoryName(configPath).ToUpper();
                if (!m_TaskExecutionHostList.Exists(t => t.FolderPath == folder))
                {
                    lock (s_SyncObj)
                    {
                        if (!m_TaskExecutionHostList.Exists(t => t.FolderPath == folder))
                        {
                            tmp = new TaskExecutionHost(configPath);
                            tmp.UnhandledException += (s, e) => { OnUnhandledException(s, e); };
                            tmp.EachTaskStarted += (s, e) => { OnEachTaskStarted(s, e); };
                            tmp.EachTaskStarting += (s, e) => { OnEachTaskStarting(s, e); };
                            tmp.EachTaskStopped += (s, e) => { OnEachTaskStopped(s, e); };
                            tmp.EachTaskStopping += (s, e) => { OnEachTaskStopping(s, e); };
                            tmp.EachTaskRestarted += (s, e) => { OnEachTaskRestarted(s, e); };
                            tmp.EachTaskRestarting += (s, e) => { OnEachTaskRestarting(s, e); };
                            m_TaskExecutionHostList.Add(tmp);
                        }
                    }
                }
                if (tmp != null)
                {
                    tmp.Open();
                }
            }
            catch (Exception ex)
            {
                if (tmp != null)
                {
                    tmp.Dispose();
                }
                var e = new ExceptionEventArgs(ex, Path.GetDirectoryName(configPath), "", "启动整个目录里的服务时出现异常，请确认任务配置文件是否正确！");
                Logger.Error(e);
                try
                {
                    OnUnhandledException(this, e);
                }
                catch (Exception ex1)
                {
                    Logger.Error(e.Folder, "", ex1, "执行TaskExecutionHostMain.OnUnhandledException时出错");
                }
            }
        }

        private void TaskDeleted(string configPath)
        {
            TaskExecutionHost tmp = null;
            string folder = Path.GetDirectoryName(configPath).ToUpper();
            int index = m_TaskExecutionHostList.FindIndex(t => t.FolderPath == folder);
            if (index >= 0)
            {
                lock (s_SyncObj)
                {
                    index = m_TaskExecutionHostList.FindIndex(t => t.FolderPath == folder);
                    if (index >= 0)
                    {
                        tmp = m_TaskExecutionHostList[index];
                        m_TaskExecutionHostList.RemoveAt(index);
                    }
                }
            }
            if (tmp != null)
            {
                tmp.Close();
            }
        }

        private void TaskRenamed(string oldConfigPath, string newConfigPath)
        {
            oldConfigPath = oldConfigPath.ToUpper();
            newConfigPath = newConfigPath.ToUpper();
            string oldConfigFileName = Path.GetFileName(oldConfigPath).Trim().ToUpper();
            if (oldConfigFileName == CONFIG_FILE_NAME)
            {
                TaskDeleted(oldConfigPath);
                return;
            }
            string newConfigFileName = Path.GetFileName(newConfigPath).Trim().ToUpper();
            if (newConfigFileName == CONFIG_FILE_NAME)
            {
                NewTaskCreated(newConfigPath);
                return;
            }
        }
    }
}
