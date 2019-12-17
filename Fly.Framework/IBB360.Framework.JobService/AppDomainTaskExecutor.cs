using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBB360.Framework.JobService.FastInvoke;
using System.IO;
using System.Reflection;

namespace IBB360.Framework.JobService
{
    public class AppDomainTaskExecutor : MarshalByRefObject, IExecuteTask
    {
        private TaskExecutor m_TaskExecutor;
        private AppDomain m_AppDomain;
        private TaskInfo m_TaskInfo;
        private string m_FolderPath;

        public List<string> WatchFileList
        {
            get;
            private set;
        }

        public AppDomainTaskExecutor(string folderPath, TaskInfo task)
        {
            m_FolderPath = folderPath;
            InitFromTask(task);
        }

        internal static void DomainCallBack()
        {
            TaskInfo t = (TaskInfo)AppDomain.CurrentDomain.GetData("TaskInfo");
            string folderPath = AppDomain.CurrentDomain.GetData("DllFolderPath").ToString();
            AppDomain.CurrentDomain.SetData("APPBASE", folderPath);
            AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", Path.Combine(folderPath, "app.config"));
            AppDomainTaskExecutor handler = (AppDomainTaskExecutor)AppDomain.CurrentDomain.GetData("ExceptionHandler");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is System.Exception)
                {
                    Exception ex1 = (Exception)e.ExceptionObject;
                    string desc = "AppDomain的UnhandledException事件被触发";
                    Logger.Error(new ExceptionEventArgs(ex1.ToString(), folderPath, t.ID, desc));
                    try
                    {
                        handler.OnUnhandledException(ex1.ToString(), desc);
                    }
                    catch (Exception ex2)
                    {
                        Logger.Error(folderPath, t.ID, ex2, "执行AppDomain.CurrentDomain.UnhandledException的OnUnhandledException时出错");
                    }
                }
            };
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string path = args.Name.Split(',')[0].Trim() + ".dll";
                path = Path.Combine(AppDomain.CurrentDomain.GetData("DllFolderPath").ToString(), path);
                if (File.Exists(path))
                {
                    try
                    {
                        return Assembly.LoadFrom(path);
                    }
                    catch (Exception ex)
                    {
                        var de = new ExceptionEventArgs(ex, folderPath, t.ID, "Failed to load assembly \"" + path + "\" in AppDomain.CurrentDomain.AssemblyResolve.");
                        Logger.Error(de);
                        try
                        {
                            handler.OnUnhandledException(de);
                        }
                        catch (Exception ex1)
                        {
                            Logger.Error(folderPath, t.ID, ex1, "执行AppDomain.CurrentDomain.AssemblyResolve的OnUnhandledException时出错");
                        }
                    }
                }
                return null;
            };
            TaskExecutor executor = new TaskExecutor(folderPath, t);
            executor.UnhandledException += (s, e) =>
            {
                handler.OnUnhandledException(e);
            };
            executor.ExecutedNotify += (s, e) =>
            {
                handler.OnExecutedNotify(e);
            };
            AppDomain.CurrentDomain.SetData("Executor", executor);
        }

        public void InitFromTask(TaskInfo task)
        {
            m_TaskInfo = task;

            WatchFileList = new List<string>(task.WatchFileList.RelativePath.Count);
            foreach (string t in task.WatchFileList.RelativePath)
            {
                if (t.IndexOf(':') >= 0)
                {
                    throw new ApplicationException("对不起，ID为" + task.ID + "的Task的监控文件使用了绝对路径，因为只能监控应用程序根目录下的文件，所以只能使用相对于应用程序根目录的相对路径！");
                }
                string[] arry = t.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                WatchFileList.Add(Path.Combine(m_FolderPath, string.Join(Path.DirectorySeparatorChar.ToString(), arry)).ToUpper());
            }

            AppDomainSetup setupInfo = new AppDomainSetup();
            setupInfo.ShadowCopyFiles = "true";
            m_AppDomain = AppDomain.CreateDomain("Task_" + Guid.NewGuid(), null, setupInfo);
            m_AppDomain.SetData("DllFolderPath", m_FolderPath);
            m_AppDomain.SetData("ExceptionHandler", this);
            m_AppDomain.SetData("TaskInfo", task);
            m_AppDomain.DoCallBack(DomainCallBack);
            m_TaskExecutor = (TaskExecutor)m_AppDomain.GetData("Executor");
        }

        public void Start()
        {
            m_TaskExecutor.Start();
        }

        public void Stop()
        {
            if (m_TaskExecutor != null)
            {
                try
                {
                    m_TaskExecutor.Stop();
                }
                catch (Exception ex)
                {
                    OnUnhandledException(ex.ToString(), "调用远程代理的Stop方法出错");
                }
                m_TaskExecutor = null;
            }
            if (m_AppDomain != null)
            {
                try
                {
                    AppDomain.Unload(m_AppDomain);
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    OnUnhandledException(ex.ToString(), "卸载AppDomain时出错");
                }
                m_AppDomain = null;
            }
        }

        public void Restart()
        {
            Stop();
            InitFromTask(m_TaskInfo);
            Start();
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

        public void OnUnhandledException(ExceptionEventArgs ea)
        {
            ExceptionEventHandler handler = m_UnhandledException;
            if (handler != null)
            {
                try
                {
                    handler(this, ea);
                }
                catch (Exception ex1)
                {
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行AppDomainTaskExecutor.OnUnhandledException时出错");
                }
            }
        }

        public void OnUnhandledException(string exMessage, string desc)
        {
            ExceptionEventHandler handler = m_UnhandledException;
            if (handler != null)
            {
                try
                {
                    handler(this, new ExceptionEventArgs(exMessage, m_FolderPath, TaskID, desc));
                }
                catch (Exception ex1)
                {
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行AppDomainTaskExecutor.OnUnhandledException时出错");
                }
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

        protected virtual void OnExecutedNotify(ExecutedNotifyEventArgs args)
        {
            EventHandler<ExecutedNotifyEventArgs> handler = m_ExecutedNotify;
            if (handler != null)
            {
                try 
                {
                    handler(this, args);
                }
                catch (Exception ex1)
                {
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行AppDomainTaskExecutor.OnExecutedNotify时出错");
                }
            }
        }

        #endregion        

        public string TaskID
        {
            get { return m_TaskInfo.ID; }
        }

        public void Dispose()
        {
            Stop();
        }

        public override Object InitializeLifetimeService()
        {
            return null;
        }
    }
}
