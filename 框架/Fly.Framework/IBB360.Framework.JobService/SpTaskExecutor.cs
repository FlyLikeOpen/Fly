using IBB360.Framework.JobService.FastInvoke;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IBB360.Framework.JobService
{
    public class SpTaskExecutor : IExecuteTask
    {
        private readonly string m_ServerName = Dns.GetHostName();
        private readonly string m_ExecutePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

        private TaskInfo m_TaskInfo;
        private int m_PollingIntervalMilliseconds;
        private Timer m_Timer;
        private string m_FolderPath;
        private IScheduler m_Scheduler;

        private DateTime m_LastLoopExecuteTime;
        private bool m_IsStop;

        public SpTaskExecutor(string folderPath, TaskInfo task)
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
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行SpTaskExecutor.OnUnhandledException时出错");
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
                    Logger.Error(m_FolderPath, TaskID, ex1, "执行SpTaskExecutor.OnUnhandledException时出错");
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
                            Logger.Error(m_FolderPath, TaskID, ex1, "执行SpTaskExecutor.OnExecutedNotify时出错");
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

                ExecSP(agrs);
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

        private void ExecSP(ExecutedNotifyEventArgs agrs)
        {
            Stopwatch timer = new Stopwatch();
            Exception exc = null;
            string timestampFileName = null;
            DateTime now = DateTime.Now;
            try
            {
                List<SqlParameter> list = new List<SqlParameter>();
                if (m_TaskInfo.SpParamList != null && m_TaskInfo.SpParamList.SpParams != null && m_TaskInfo.SpParamList.SpParams.Count > 0)
                {
                    foreach (var param in m_TaskInfo.SpParamList.SpParams)
                    {
                        object val;
                        if (param.Type == "now")
                        {
                            val = now;
                        }
                        else if (param.Type == "last_run_time")
                        {
                            timestampFileName = param.Value;
                            if (string.IsNullOrWhiteSpace(timestampFileName))
                            {
                                timestampFileName = m_TaskInfo.ID + ".ts";
                            }
                            val = ReadTimestamp(timestampFileName);
                        }
                        else
                        {
                            Type type = Type.GetType(param.Type);
                            val = GetValueByType(type, param.Value);
                        }
                        string pn = param.Name;
                        if(pn.StartsWith("@") == false)
                        {
                            pn = "@" + pn;
                        }
                        list.Add(new SqlParameter(pn, val));
                    }
                }
                using (SqlConnection conn = new SqlConnection(m_TaskInfo.ConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(m_TaskInfo.SpName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        foreach (var p in list)
                        {
                            cmd.Parameters.Add(p);
                        }
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }
                if (string.IsNullOrWhiteSpace(timestampFileName) == false)
                {
                    SaveTimestamp(now, timestampFileName);
                }
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
                    agrs.Description = "在Task循环执行处执行存储过程" + m_TaskInfo.SpName + "时发生错误";
                    agrs.ExecuteResult = ExecuteResultEnum.Failed;
                    OnUnhandledException(exc, agrs);
                }
                else
                {
                    agrs.Description = "在Task循环执行处执行存储过程" + m_TaskInfo.SpName + "方法成功";
                    agrs.ExecuteResult = ExecuteResultEnum.Successed;
                    OnExecutedNotify(agrs);
                }
            }
        }

        private string EnsureValidFileName(string fileName)
        {
            var cs = Path.GetInvalidFileNameChars();
            foreach (var c in cs)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        private void SaveTimestamp(DateTime timestamp, string fileName)
        {
            fileName = EnsureValidFileName(fileName);
            if (string.IsNullOrWhiteSpace(Path.GetPathRoot(fileName)))
            {
                fileName = Path.Combine(m_FolderPath, fileName);
            }
            File.WriteAllText(fileName, timestamp.ToString());
        }

        private DateTime ReadTimestamp(string fileName)
        {
            fileName = EnsureValidFileName(fileName);
            if (string.IsNullOrWhiteSpace(Path.GetPathRoot(fileName)))
            {
                fileName = Path.Combine(m_FolderPath, fileName);
            }
            if (File.Exists(fileName) == false)
            {
                return new DateTime(2000, 1, 1);
            }
            string c = File.ReadAllText(fileName);
            DateTime t;
            if (string.IsNullOrWhiteSpace(c) == false && DateTime.TryParse(c, out t))
            {
                return t;
            }
            return new DateTime(2000, 1, 1);
        }

        private object GetValueByType(Type destinationType, string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return DBNull.Value;
            }
            try
            {
                if (destinationType == typeof(string))
                {
                    return data.Trim();
                }
                else if (destinationType == typeof(char) || destinationType == typeof(char?))
                {
                    return ((object)Convert.ToChar(data));
                }
                else if (destinationType == typeof(sbyte) || destinationType == typeof(sbyte?))
                {
                    return ((object)Convert.ToSByte(data));
                }
                else if (destinationType == typeof(byte) || destinationType == typeof(byte?))
                {
                    return ((object)Convert.ToByte(data));
                }
                else if (destinationType == typeof(short) || destinationType == typeof(short?))
                {
                    return ((object)Convert.ToInt16(data));
                }
                else if (destinationType == typeof(ushort) || destinationType == typeof(ushort?))
                {
                    return ((object)Convert.ToUInt16(data));
                }
                else if (destinationType == typeof(int) || destinationType == typeof(int?))
                {
                    return ((object)Convert.ToInt32(data));
                }
                else if (destinationType == typeof(uint) || destinationType == typeof(uint?))
                {
                    return ((object)Convert.ToUInt32(data));
                }
                else if (destinationType == typeof(long) || destinationType == typeof(long?))
                {
                    return ((object)Convert.ToInt64(data));
                }
                else if (destinationType == typeof(ulong) || destinationType == typeof(ulong?))
                {
                    return ((object)Convert.ToUInt64(data));
                }
                else if (destinationType == typeof(DateTime) || destinationType == typeof(DateTime?))
                {
                    return ((object)Convert.ToDateTime(data));
                }
                else if (destinationType == typeof(decimal) || destinationType == typeof(decimal?))
                {
                    return ((object)Convert.ToDecimal(data));
                }
                else if (destinationType == typeof(float) || destinationType == typeof(float?))
                {
                    return ((object)Convert.ToSingle(data));
                }
                else if (destinationType == typeof(double) || destinationType == typeof(double?))
                {
                    return ((object)Convert.ToDouble(data));
                }
                else if (destinationType == typeof(bool) || destinationType == typeof(bool?))
                {
                    return ((object)Convert.ToBoolean(data));
                }
                throw new NotSupportedException("不支持类型为" + destinationType.FullName + "的SQL参数");
            }
            catch
            {
                string msg = "类型转换错误：Can't cast '" + data.GetType().FullName + "' to '" + destinationType.FullName + "'.";
                throw new InvalidCastException(msg);
            }
        }
    }
}
