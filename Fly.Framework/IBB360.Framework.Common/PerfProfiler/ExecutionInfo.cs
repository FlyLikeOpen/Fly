using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    internal class ExecutionInfo
    {
        //private Stopwatch m_Stopwatch = new Stopwatch();
        private DateTime m_StartTime;
        private DateTime m_EndTime;
        private bool m_HasThrowException = false;
        private long m_StartTicks;
        private long m_EndTicks;
        private long m_Frequency;
        private Stopwatch m_Watch;
        private long m_ElapsedMilliseconds;

        [ThreadStatic]
        private static Guid s_ThreadID = Guid.NewGuid();
        [ThreadStatic]
        private static int s_Depth;

        private static string s_ServerIP;
        private static string s_AppId;
        private static string GetServerIP() // 不需要加锁，因为即使多线程时发生了并发冲突也不会有问题，加锁反而带来更大的性能开销
        {
            if (s_ServerIP == null)
            {
                IPAddress ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(i => i.AddressFamily == AddressFamily.InterNetwork);
                s_ServerIP = ip.ToString();
            }
            return s_ServerIP;
        }

        private static string GetAppId()
        {
            if (s_AppId == null)
            {
                string id = ConfigurationManager.AppSettings["PerfProfiler_AppId"];
                s_AppId = string.IsNullOrWhiteSpace(id) ? "Unknown" : id.Trim();
                if(s_AppId.Length > 16)
                {
                    s_AppId = s_AppId.Substring(0, 16);
                }
            }
            return s_AppId;
        }

        public bool HasThrowException
        {
            get { return m_HasThrowException; }
            set { m_HasThrowException = value; }
        }

        public void Start()
        {
            s_Depth++;
            m_StartTime = DateTime.Now;
            m_StartTicks = Stopwatch.GetTimestamp();
            m_Watch = Stopwatch.StartNew();
        }

        public void Stop()
        {
            m_Watch.Stop();
            m_EndTicks = Stopwatch.GetTimestamp();
            m_Frequency = Stopwatch.Frequency;
            m_EndTime = DateTime.Now;
            m_ElapsedMilliseconds = m_Watch.ElapsedMilliseconds;
            s_Depth--;
        }

        public ProfilerMessage ToMessage(string url, string assembly, string classType, string method)
        {
            if (s_ThreadID == Guid.Empty)
            {
                s_ThreadID = Guid.NewGuid();
            }
            return new ProfilerMessage()
            {
                AppServerIP = GetServerIP(),
                AppID = GetAppId(),
                ThreadID = s_ThreadID,
                CallStackDepth = s_Depth,
                AssemblyFullName = assembly,
                ClassTypeFullName = classType,
                MethodName = method,
                RequestRawUrl = url,
                RequestUrl = string.IsNullOrWhiteSpace(url) ? url : url.Split('?')[0],

                ExecutionStartTime = m_StartTime,
                ExecutionEndTime = m_EndTime,
                ExecutionStartTicks = m_StartTicks,
                ExecutionEndTicks = m_EndTicks,
                ExecutionFrequency = m_Frequency,
                HasThrowException = m_HasThrowException,
                ExecutionTicks = (double)m_ElapsedMilliseconds / 1000 //(double)(m_EndTicks - m_StartTicks) / m_Frequency
            };
        }
    }
}
