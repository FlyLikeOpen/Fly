using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using IBB360.Framework.JobService.FastInvoke;
using System.Threading;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace IBB360.Framework.JobService
{
    public class TaskExecutor : MarshalByRefObject, IExecuteTask
    {
        private readonly string m_ServerName = Dns.GetHostName();
        private readonly string m_ExecutePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

        private object m_Executor;
        private TaskInfo m_TaskInfo;
        private int m_PollingIntervalMilliseconds;
        private bool m_HasLoopMethod = false;
        private IScheduler m_Scheduler;
        private Timer m_Timer;
        private string m_FolderPath;

        private bool m_IsOpenMethodExecuted = false;
        private DateTime m_LastLoopExecuteTime = default(DateTime);
        private bool m_IsStop;

        public TaskExecutor(string folderPath, TaskInfo task)
        {
            m_FolderPath = folderPath;
            InitFromTask(task);
        }

        public void InitFromTask(TaskInfo task)
        {
            if (!task.IsValid)
            {
                throw new ApplicationException("ID为" + task.ID + "的Task配置有误！");
            }
            if (task.SchedulerInfo != null && !task.SchedulerInfo.IsValid)
            {
                throw new ApplicationException("ID为" + task.ID + "的Task对应的ID为" + task.SchedulerInfo.ID + "的Scheduler配置有误！");
            }
            m_TaskInfo = task;
            string aPath = task.AssemblyPath.Trim().IndexOf(':') > 0 ? task.AssemblyPath.Trim() : Path.Combine(m_FolderPath, task.AssemblyPath.Trim());
            Assembly a = Assembly.LoadFrom(aPath);
            m_Executor = Invoker.CreateInstance(a.GetType(task.TypeFullName.Trim()));
            if (task.OpenMethodName != null && task.OpenMethodName.Trim().Length > 0
                && m_Executor.GetType().GetMethod(task.OpenMethodName.Trim()) == null)
            {
                throw new ApplicationException("ID为" + task.ID + "的Task配置的open方法‘" + task.OpenMethodName.Trim() + "’不存在");
            }
            if (task.CloseMethodName != null && task.CloseMethodName.Trim().Length > 0
                && m_Executor.GetType().GetMethod(task.CloseMethodName.Trim()) == null)
            {
                throw new ApplicationException("ID为" + task.ID + "的Task配置的close方法‘" + task.CloseMethodName.Trim() + "’不存在");
            }
            if (task.LoopMethodName != null && task.LoopMethodName.Trim().Length > 0)
            {
                if (m_Executor.GetType().GetMethod(task.LoopMethodName.Trim()) == null)
                {
                    throw new ApplicationException("ID为" + task.ID + "的Task配置的loop方法‘" + task.LoopMethodName.Trim() + "’不存在");
                }
                m_HasLoopMethod = true;
            }
            m_PollingIntervalMilliseconds = task.PollingIntervalMilliseconds > 0 ? task.PollingIntervalMilliseconds : 100;
            if (task.SchedulerInfo == null)
            {
                m_Scheduler = new CompositeScheduler();
            }
            else
            {
                string sPath = task.SchedulerInfo.AssemblyPath.Trim().IndexOf(':') > 0 ? task.SchedulerInfo.AssemblyPath.Trim() 
                    : Path.Combine(m_FolderPath, task.SchedulerInfo.AssemblyPath.Trim());
                Assembly b = Assembly.LoadFrom(sPath);
                m_Scheduler = (IScheduler)Invoker.CreateInstance(b.GetType(task.SchedulerInfo.TypeFullName.Trim()));
            }
            m_Scheduler.InitScheduler(task);
        }

        public void Start()
        {
            m_IsStop = false;
            m_Timer = new Timer(new TimerCallback(InnerExecute), null, 0, Timeout.Infinite);
        }

        public void Stop()
        {
            if (!m_IsStop)
            {
                m_IsStop = true;
                if (m_Timer != null)
                {
                    Timer t = m_Timer;
                    m_Timer = null;
                    t.Dispose();
                }
                Thread.Sleep(m_PollingIntervalMilliseconds);
                m_IsOpenMethodExecuted = false;
                CallCloseMethod();
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

        protected virtual void OnUnhandledException(Exception ex, string desc)
        {
            var e = new ExceptionEventArgs(ex, m_FolderPath, TaskID, desc);
            Logger.Error(e);
            ExceptionEventHandler handler = m_UnhandledException;
            if (handler != null)
            {
                try
                {
                    handler(this, new ExceptionEventArgs(ex, m_FolderPath, TaskID, desc));
                }
                catch (Exception ex1)
                {
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行TaskExecutor.OnUnhandledException时出错");
                }
            }
        }

        protected virtual void OnUnhandledException(Exception ex, ExecutedNotifyEventArgs agrs)
        {
            ExceptionEventArgs t = new ExceptionEventArgs(ex, agrs.Folder, agrs.TaskID, agrs.Description)
            {
                ExecuteDuration = agrs.ExecuteDuration,
                ExecutePath = agrs.ExecutePath,
                ExecuteResult = agrs.ExecuteResult,
                ExecuteStartTime = agrs.ExecuteStartTime,
                LastExecuteTime = agrs.LastExecuteTime,
                NextExecuteTime = agrs.NextExecuteTime,
                ServerName = agrs.ServerName
            };
            Logger.Error(t);
            ExceptionEventHandler handler = m_UnhandledException;
            if (handler != null)
            {
                try
                {
                    handler(this, t);
                }
                catch (Exception ex1)
                {
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行TaskExecutor.OnUnhandledException时出错");
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
            Logger.Info(args);
            EventHandler<ExecutedNotifyEventArgs> handler = m_ExecutedNotify;
            if (handler != null)
            {
                handler.BeginInvoke(this, args, new AsyncCallback(o =>
                {
                    EventHandler<ExecutedNotifyEventArgs> h = o.AsyncState as EventHandler<ExecutedNotifyEventArgs>;
                    if (h != null)
                    {
                        try
                        {
                            h.EndInvoke(o);
                        }
                        catch (Exception ex1)
                        {
                            Logger.Error(m_FolderPath, TaskID, ex1, "执行TaskExecutor.OnExecutedNotify时出错");
                        }
                    }
                }), handler);
                //handler(this, args);
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

        private void InnerExecute(object state)
        {
            if (m_IsStop)
            {
                return;
            }
            if (!m_IsOpenMethodExecuted)
            {
                if (m_IsStop)
                {
                    return;
                }
                CallOpenMethod();
                m_IsOpenMethodExecuted = true;
                m_LastLoopExecuteTime = DateTime.Now;
            }
            if (!m_HasLoopMethod || m_IsStop)
            {
                return;
            }
            ScheduleActionEnum action;
            DateTime? nextRunTime = null;
            try
            {
                action = m_Scheduler.GetAction(m_LastLoopExecuteTime, DateTime.Now, out nextRunTime);
            }
            catch (Exception ex)
            {
                OnUnhandledException(ex, "在Task循环执行处执行" + m_Scheduler.GetType().FullName + ".GetAction时发生错误");
                return;
            }
            if (action == ScheduleActionEnum.End || m_IsStop)
            {
                return;
            }
            if (action == ScheduleActionEnum.Run)
            {
                ExecutedNotifyEventArgs agrs = new ExecutedNotifyEventArgs();
                agrs.ExecutePath = this.m_ExecutePath;
                agrs.ServerName = m_ServerName;
                agrs.TaskID = m_TaskInfo.ID;
                agrs.Folder = m_FolderPath;
                agrs.LastExecuteTime = m_LastLoopExecuteTime == default(DateTime) ? null : new DateTime?(m_LastLoopExecuteTime);
                agrs.NextExecuteTime = nextRunTime;

                m_LastLoopExecuteTime = DateTime.Now;
                agrs.ExecuteStartTime = m_LastLoopExecuteTime;

                CallLoopMethod(agrs);
            }
            if (m_IsStop)
            {
                return;
            }
            if (m_Timer != null)
            {
                m_Timer.Change(m_PollingIntervalMilliseconds, Timeout.Infinite);
            }
        }

        private void CallOpenMethod()
        {
            if (m_TaskInfo != null && m_TaskInfo.OpenMethodName != null && m_TaskInfo.OpenMethodName.Trim().Length > 0)
            {
                try
                {
                    Invoker.MethodInvoke(m_Executor, m_TaskInfo.OpenMethodName.Trim());
                }
                catch (Exception ex)
                {
                    OnUnhandledException(ex, "在Task的Open处执行" + m_TaskInfo.TypeFullName + "." + m_TaskInfo.OpenMethodName + "方法时出错");
                }
            }
        }

        private void CallCloseMethod()
        {
            if (m_TaskInfo != null && m_TaskInfo.CloseMethodName != null && m_TaskInfo.CloseMethodName.Trim().Length > 0)
            {
                try
                {
                    Invoker.MethodInvoke(m_Executor, m_TaskInfo.CloseMethodName.Trim());
                }
                catch (Exception ex)
                {
                    OnUnhandledException(ex, "在Task的Close处执行" + m_TaskInfo.TypeFullName + "." + m_TaskInfo.CloseMethodName + "方法时出错");
                }
            }
        }

        private void CallLoopMethod(ExecutedNotifyEventArgs agrs)
        {
            if (m_HasLoopMethod)
            {
                Stopwatch timer = new Stopwatch();
                Exception exc = null;
                try
                {
                    timer.Start();
                    Invoker.MethodInvoke(m_Executor, m_TaskInfo.LoopMethodName.Trim());
                }
                catch (Exception ex)
                {
                    exc = ex;                    
                }
                finally
                {
                    timer.Stop();
                    agrs.ExecuteDuration = timer.Elapsed.TotalMilliseconds;
                    if (exc != null)
                    {
                        agrs.Description = "在Task循环执行处执行" + m_TaskInfo.TypeFullName + "." + m_TaskInfo.LoopMethodName + "方法时出错";
                        agrs.ExecuteResult = ExecuteResultEnum.Failed;
                        OnUnhandledException(exc, agrs);
                    }
                    else
                    {
                        agrs.Description = "在Task循环执行处执行" + m_TaskInfo.TypeFullName + "." + m_TaskInfo.LoopMethodName + "方法成功";
                        agrs.ExecuteResult = ExecuteResultEnum.Successed;
                        OnExecutedNotify(agrs);
                    }
                }
            }
        }

        private void WriteLog(string folderPath, string logType, string text)
        {
            try
            {
                string logFolder = Path.Combine(folderPath, logType);
                if (Directory.Exists(logFolder) == false)
                {
                    Directory.CreateDirectory(logFolder);
                }
                string logFilePath = Path.Combine(logFolder, DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
                DateTime now = DateTime.Now;
                StringBuilder sb = new StringBuilder();
                sb.Append("\r\n**---- [" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] - Begin ----******************************************************\r\n");
                sb.Append(text);
                sb.Append("\r\n**---- [" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] - End ----********************************************************\r\n");
                byte[] textByte = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                lock (logFilePath)
                {
                    using (FileStream logStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Write))
                    {
                        logStream.Write(textByte, 0, textByte.Length);
                        logStream.Close();
                    }
                }
            }
            catch { }
        }

        public override Object InitializeLifetimeService() 
        { 
            return null; 
        }
    }
}
