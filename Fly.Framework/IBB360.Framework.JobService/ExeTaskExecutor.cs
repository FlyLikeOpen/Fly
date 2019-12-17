using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Reflection;
using IBB360.Framework.JobService.FastInvoke;
using System.Diagnostics;
using System.Net;

namespace IBB360.Framework.JobService
{
    public class ExeTaskExecutor : IExecuteTask
    {
        private readonly string m_ServerName = Dns.GetHostName();
        private readonly string m_ExecutePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

        private string m_ExePath;
        private TaskInfo m_TaskInfo;
        private int m_PollingIntervalMilliseconds;
        private Timer m_Timer;
        private string m_FolderPath;
        private IScheduler m_Scheduler;

        private DateTime m_LastLoopExecuteTime;
        private bool m_IsStop;

        private int m_RunningTimeout = 24 * 60 * 60 * 1000; // 24 hours

        public ExeTaskExecutor(string folderPath, TaskInfo task)
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
            m_ExePath = task.AssemblyPath.Trim().IndexOf(':') > 0 ? task.AssemblyPath.Trim() : Path.Combine(m_FolderPath, task.AssemblyPath.Trim());
            if (!File.Exists(m_ExePath))
            {
                throw new FileNotFoundException(m_ExePath);
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
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行ExeTaskExecutor.OnUnhandledException时出错");
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
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行ExeTaskExecutor.OnUnhandledException时出错");
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
                            Logger.Error(m_FolderPath, TaskID, ex1, "执行ExeTaskExecutor.OnExecutedNotify时出错");
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

                RunExeFile(agrs);
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

        private void RunExeFile(ExecutedNotifyEventArgs agrs)
        {
            Process proc = null;
            Stopwatch timer = new Stopwatch();
            Exception exc = null;
            try
            {
                StringBuilder outputData = new StringBuilder();
                proc = new Process();
                proc.StartInfo.FileName = m_ExePath;
                if (m_TaskInfo.Args != null && m_TaskInfo.Args.Trim().Length > 0)
                {
                    proc.StartInfo.Arguments = m_TaskInfo.Args.Trim();
                }
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(proc.StartInfo.FileName);
                proc.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    outputData.AppendLine(e.Data);
                };
                timer.Start();
                proc.Start();

                proc.BeginOutputReadLine();

                if (!proc.WaitForExit(m_RunningTimeout))
                {
                    proc.Kill();
                    exc = new TimeoutException("任务执行超时（超过了24小时），已被自动终止");
                }

                if (proc.ExitCode != 0)
                {
                    string error = proc.StandardError.ReadToEnd();
                    exc = new ApplicationException(error);
                }
                else
                {
                    agrs.Description = outputData.ToString();
                }
            }
            catch (Exception ex)
            {
                exc = ex;
            }
            finally
            {
                timer.Stop();
                if (proc != null)
                {
                    try
                    {
                        proc.Close();
                    }
                    catch { }
                }
                agrs.ExecuteDuration = timer.Elapsed.TotalMilliseconds;
                if (exc != null)
                {
                    agrs.Description = "在Task开启新进程执行文件" + m_ExePath + "时发生错误";
                    agrs.ExecuteResult = ExecuteResultEnum.Failed;
                    OnUnhandledException(exc, agrs);
                }
                else
                {
                    agrs.Description = "Task开启新进程执行文件" + m_ExePath + "成功！Console上输出：" + agrs.Description;
                    agrs.ExecuteResult = ExecuteResultEnum.Successed;
                    OnExecutedNotify(agrs);
                }
            }
        }
    }
}
