using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    internal class TimerContainer
    {
        public Timer Timer { get; private set; }

        private int m_ExecutedTimes = 0;
        public int ExecutedTimes { get { return m_ExecutedTimes; } }

        private int m_MaxExecutionTimes = -1;
        public int MaxExecutionTimes { get { return m_MaxExecutionTimes; } }

        public object State { get; private set; }

        public TimerContainer(Timer timer, int? maxExecutionTimes = null, object state = null)
        {
            Timer = timer;
            m_ExecutedTimes = 0;
            m_MaxExecutionTimes = maxExecutionTimes.GetValueOrDefault(-1);
            State = state;
        }

        public void IncreaseExecutedTimes(int times = 1)
        {
            //if (times <= 0)
            //{
            //    throw new ArgumentOutOfRangeException("times", "入参times必须大于0");
            //}

            // 以下代码是使用无锁方式来解决并发冲突，确保是多线程操作安全（利用CPU原子操作Interlocked）
            int initialValue;
            int computedValue;
            do
            {
                initialValue = m_ExecutedTimes;
                computedValue = initialValue + times;
            }
            while (initialValue != Interlocked.CompareExchange(ref m_ExecutedTimes, computedValue, initialValue));
        }

        public void ClearExecutedTimesToZero()
        {
            // 以下代码是使用无锁方式来解决并发冲突，确保是多线程操作安全（利用CPU原子操作Interlocked）
            Interlocked.Exchange(ref m_ExecutedTimes, 0);
        }

        public void SetMaxExecutionTimes(int? maxExecutionTimes)
        {
            // 以下代码是使用无锁方式来解决并发冲突，确保是多线程操作安全（利用CPU原子操作Interlocked）
            Interlocked.Exchange(ref m_MaxExecutionTimes, maxExecutionTimes.GetValueOrDefault(-1));
        }
    }

    public class TimerJob
    {
        private static readonly Dictionary<string, TimerContainer> s_TimerList = new Dictionary<string, TimerContainer>();

        public static void RegisterJob(string jobId, Action<object> executingAction, int intervalMillisecond, int? maxExecutionTimes = null, object state = null)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentNullException("jobId");
            }
            if (executingAction == null)
            {
                throw new ArgumentNullException("executingAction");
            }
            if (s_TimerList.ContainsKey(jobId))
            {
                throw new ApplicationException("注册了重复的Job Id：" + jobId);
            }
            Timer timer = new Timer(new TimerCallback(s =>
            {
                try
                {
                    TimerContainer t;
                    if (s_TimerList.TryGetValue(s.ToString(), out t) && t != null)
                    {
                        if (t.MaxExecutionTimes == -1 || t.MaxExecutionTimes > t.ExecutedTimes)
                        {
                            try
                            {
                                executingAction(t.State);
                            }
                            catch (Exception ex1)
                            {
                                string jid = (s == null) ? "[未知JobId]" : s.ToString();
                                Logger.Error("JobId为" + jid + "的TimerJob执行发生程序异常：\r\n\r\n" + ex1.ToString());
                            }
                            t.IncreaseExecutedTimes();
                        }
                    }
                }
                catch (Exception ex)
                {
                    string jid = (s == null) ? "[未知JobId]" : s.ToString();
                    Logger.Error("JobId为" + jid + "的TimerJob执行发生程序异常[外围异常]：\r\n\r\n" + ex.ToString());
                }
            }), jobId, Timeout.Infinite, intervalMillisecond);
            s_TimerList.Add(jobId, new TimerContainer(timer, maxExecutionTimes, state));
            timer.Change(5000, intervalMillisecond);
        }

        public static void ChangeJobInterval(string jobId, int intervalMillisecond)
        {
            TimerContainer t;
            if (s_TimerList.TryGetValue(jobId, out t) && t != null && t.Timer != null)
            {
                t.Timer.Change(intervalMillisecond, intervalMillisecond);
            }
        }

        public static void PauseJob(string jobId)
        {
            ChangeJobInterval(jobId, Timeout.Infinite);
        }

        public static void SetJobMaxExecutionTimes(string jobId, int? maxExecutionTimes)
        {
            TimerContainer t;
            if (s_TimerList.TryGetValue(jobId, out t) && t != null)
            {
                t.SetMaxExecutionTimes(maxExecutionTimes);
            }
        }

        public static int GetJobExecutedTimes(string jobId)
        {
            TimerContainer t;
            if (s_TimerList.TryGetValue(jobId, out t) && t != null)
            {
                return t.ExecutedTimes;
            }
            return 0;
        }

        public static void ClearJobExecutedTimesToZero(string jobId)
        {
            TimerContainer t;
            if (s_TimerList.TryGetValue(jobId, out t) && t != null)
            {
                t.ClearExecutedTimesToZero();
            }
        }
    }
}
