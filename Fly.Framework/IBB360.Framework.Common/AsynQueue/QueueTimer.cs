using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    public class QueueTimer<T>
    {
        private volatile LocalMemoryQueue<T> m_Queue;
        private Timer m_FlushTimer;

        private int m_ElapseMillisecond;
        private int m_MaxBatchCount;
        private Action<List<T>> m_Handler;
        private int m_Started;

        public QueueTimer(Action<List<T>> handler, int elapseMillisecond = 1000 * 60, bool autoStartFlush = false, int boundedCapacity = 10240, int maxBatchCount = 200)
        {
            m_Handler = handler;
            m_Queue = new LocalMemoryQueue<T>(boundedCapacity);
            m_ElapseMillisecond = elapseMillisecond;
            m_MaxBatchCount = maxBatchCount;
            int dueTime = autoStartFlush ? m_ElapseMillisecond : Timeout.Infinite;
            m_Started = autoStartFlush ? 1 : 0;
            m_FlushTimer = new Timer(new TimerCallback(FlushQueue), null, dueTime, Timeout.Infinite);
        }

        private void DoAction(List<T> msgList)
        {
            if(m_Handler != null)
            {
                try
                {
                    m_Handler(msgList);
                }
                catch (Exception ex)
                {
                    Logger.Error("QueueTimer : 队列消息处理出现异常\r\n" + ex);
                }
            }
        }

        public void Enqueue(T msg)
        {
            m_Queue.Enqueue(msg);
            if(m_Queue.Count >= m_MaxBatchCount)
            {
                Interlocked.Exchange(ref m_Started, 1);
                m_FlushTimer.Change(0, Timeout.Infinite);
            }
            else if (Interlocked.Exchange(ref m_Started, 1) == 0)
            {
                m_FlushTimer.Change(m_ElapseMillisecond, Timeout.Infinite);
            }
        }

        private void FlushQueue(object ar)
        {
            try
            {
                List<T> list = new List<T>(m_MaxBatchCount);
                do
                {
                    T msg;
                    if (m_Queue.Dequeue(out msg) < 0) // queue is empty.
                    {
                        DoAction(list);
                        break;
                    }
                    list.Add(msg);
                    if (list.Count >= m_MaxBatchCount)
                    {
                        DoAction(list);
                        list.Clear();
                    }
                } while (true);
            }
            finally
            {
                m_FlushTimer.Change(m_ElapseMillisecond, Timeout.Infinite);
            }
        }
    }
}
