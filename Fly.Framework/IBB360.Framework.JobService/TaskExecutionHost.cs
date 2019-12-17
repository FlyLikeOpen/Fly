using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace IBB360.Framework.JobService
{
    public class TaskExecutionHost : IDisposable
    {
        private List<TaskInfo> m_TaskInfoList;
        private List<IExecuteTask> m_TaskExecutorList;
        private FileWatcher m_FileWatcher;
        private TaskConfigMgr m_TaskConfigMgr;
        private bool m_IsOpen = false;

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
            Logger.Info(args, "准备启动...");
            EventHandler<TaskEventArgs> handler = m_EachTaskStarting;
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex1)
                {
                    Logger.Error(args.Folder, args.TaskID, ex1, "执行TaskExecutionHost.OnEachTaskStarting时出错");
                }
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
            Logger.Info(args, "启动成功...");
            EventHandler<TaskEventArgs> handler = m_EachTaskStarted;
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex1)
                {
                    Logger.Error(args.Folder, args.TaskID, ex1, "执行TaskExecutionHost.OnEachTaskStarted时出错");
                }
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
            Logger.Info(args, "准备停止...");
            EventHandler<TaskEventArgs> handler = m_EachTaskStopping;
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex1)
                {
                    Logger.Error(args.Folder, args.TaskID, ex1, "执行TaskExecutionHost.OnEachTaskStopping时出错");
                }
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
            Logger.Info(args, "已经停止...");
            EventHandler<TaskEventArgs> handler = m_EachTaskStopped;
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex1)
                {
                    Logger.Error(args.Folder, args.TaskID, ex1, "执行TaskExecutionHost.OnEachTaskStopped时出错");
                }
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
            Logger.Info(args, "准备重启...");
            EventHandler<TaskEventArgs> handler = m_EachTaskRestarting;
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex1)
                {
                    Logger.Error(args.Folder, args.TaskID, ex1, "执行TaskExecutionHost.OnEachTaskRestarting时出错");
                }
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
            Logger.Info(args, "重启成功...");
            EventHandler<TaskEventArgs> handler = m_EachTaskRestarted;
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex1)
                {
                    Logger.Error(args.Folder, args.TaskID, ex1, "执行TaskExecutionHost.OnEachTaskRestarted时出错");
                }
            }
        }

        #endregion

        public string FolderPath
        {
            get;
            private set;
        }

        public TaskExecutionHost()
        {
            m_TaskConfigMgr = new TaskConfigMgr();
            FolderPath = Path.GetDirectoryName(m_TaskConfigMgr.ConfigFilePath);
            InitFromTasks(m_TaskConfigMgr.GetSetting());
        }

        public TaskExecutionHost(string configPath)
        {
            m_TaskConfigMgr = new TaskConfigMgr(configPath);
            FolderPath = Path.GetDirectoryName(m_TaskConfigMgr.ConfigFilePath);
            InitFromTasks(m_TaskConfigMgr.GetSetting());
        }

        private void InitFromTasks(List<TaskInfo> infoList)
        {
            m_TaskInfoList = infoList;
            m_TaskExecutorList = new List<IExecuteTask>(infoList.Count);
            bool needAllFileWatch = false;
            foreach (TaskInfo info in infoList)
            {
                IExecuteTask tmp;
                if (info.IsExe)
                {
                    tmp = new ExeTaskExecutor(FolderPath, info);
                }
                else if (info.IsSp)
                {
                    tmp = new SpTaskExecutor(FolderPath, info);
                }
                else if (info.WatchFileList == null || info.WatchFileList.RelativePath == null || info.WatchFileList.RelativePath.Count <= 0)
                {
                    tmp = new TaskExecutor(FolderPath, info);
                }
                else
                {
                    tmp = new AppDomainTaskExecutor(FolderPath, info);
                    needAllFileWatch = true;
                }
                tmp.UnhandledException += (s, e) => { OnUnhandledException(s, e); };
                tmp.ExecutedNotify += (s, e) => { OnExecutedNotify(s, e); };
                m_TaskExecutorList.Add(tmp);
            }
            if (needAllFileWatch)
            {
                m_FileWatcher = new FileWatcher(FolderPath, FilesChanged);
            }
            else
            {
                m_FileWatcher = new FileWatcher(FolderPath, Path.GetFileName(m_TaskConfigMgr.ConfigFilePath), FilesChanged);
            }
        }

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

        private void OnUnhandledException(Exception ex, string taskID, string desc)
        {
            ExceptionEventHandler handler = m_UnhandledException;
            if (handler != null)
            {
                handler(this, new ExceptionEventArgs(ex, FolderPath, taskID, desc));
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

        public void Open()
        {
            if (m_TaskExecutorList == null || m_TaskExecutorList.Count <= 0)
            {
                return;
            }
            foreach (IExecuteTask task in m_TaskExecutorList)
            {
                try
                {
                    OnEachTaskStarting(this, new TaskEventArgs(FolderPath, task.TaskID));
                    task.Start();
                    OnEachTaskStarted(this, new TaskEventArgs(FolderPath, task.TaskID));
                }
                catch (Exception ex)
                {
                    OnUnhandledException(ex, task.TaskID, "执行Task”" + task.TaskID + "“的Start方法出错");
                }
            }
            if (m_FileWatcher != null)
            {
                m_FileWatcher.StartToWatch();
            }
            m_IsOpen = true;
        }

        public void Close()
        {
            if (!m_IsOpen)
            {
                return;
            }
            if (m_FileWatcher != null)
            {
                m_FileWatcher.StopToWatch();
                m_FileWatcher.Dispose();
                m_FileWatcher = null;
            }
            if (m_TaskExecutorList == null || m_TaskExecutorList.Count <= 0)
            {
                return;
            }
            foreach (IExecuteTask task in m_TaskExecutorList)
            {
                try
                {
                    OnEachTaskStopping(this, new TaskEventArgs(FolderPath, task.TaskID));
                    task.Stop();
                    OnEachTaskStopped(this, new TaskEventArgs(FolderPath, task.TaskID));
                }
                catch (Exception ex)
                {
                    OnUnhandledException(ex, task.TaskID, "执行Task”" + task.TaskID + "“的Stop方法出错");
                }
            }
            m_TaskExecutorList = null;
            m_IsOpen = false;
        }

        public void Restart()
        {
            Restart(m_TaskInfoList);
        }

        private void Restart(List<TaskInfo> infoList)
        {
            try
            {
                Close();
                InitFromTasks(infoList);
                Open();
            }
            catch (Exception ex)
            {
                OnUnhandledException(this, new ExceptionEventArgs(ex, FolderPath, "", "重启整个目录里的服务时出现异常！"));
            }
        }

        public void Dispose()
        {
            Close();
        }

        private bool WildcardContains(List<string> files, string wildcardPattern)
        {
            wildcardPattern = wildcardPattern.Trim();
            foreach (var file in files)
            {
                if (string.Equals(file, wildcardPattern, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            string regPattern = "^" + Regex.Escape(wildcardPattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            foreach (var file in files)
            {
                if (Regex.IsMatch(file, regPattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void FilesChanged(List<string> files)
        {
            if (files.Contains(m_TaskConfigMgr.ConfigFilePath, StringComparer.InvariantCultureIgnoreCase))
            {
                Restart(m_TaskConfigMgr.GetSetting());
                return;
            }

            foreach (IExecuteTask executor in m_TaskExecutorList)
            {
                if (executor is AppDomainTaskExecutor)
                {
                    try
                    {
                        AppDomainTaskExecutor ext = (AppDomainTaskExecutor)executor;
                        foreach (string filePath in ext.WatchFileList)
                        {
                            if (WildcardContains(files, filePath))
                            {
                                OnEachTaskRestarting(this, new TaskEventArgs(FolderPath, executor.TaskID));
                                try
                                {
                                    ext.Restart();
                                }
                                catch (Exception ex)
                                {
                                    OnUnhandledException(this, new ExceptionEventArgs(ex, FolderPath, executor.TaskID, "发现硬盘相关文件变化，重启服务时出现异常！"));
                                }
                                OnEachTaskRestarted(this, new TaskEventArgs(FolderPath, executor.TaskID));
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OnUnhandledException(ex, executor.TaskID, "发现硬盘相关文件变化，执行Task”" + executor.TaskID + "“的Restart方法出错");
                    }
                }
            }
        }
    }
}
