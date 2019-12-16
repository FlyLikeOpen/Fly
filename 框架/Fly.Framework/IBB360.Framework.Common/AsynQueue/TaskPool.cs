using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    public class TaskPool
    {
        public static TaskPool Instance { get; } = new TaskPool();

        private class TaskOperator
        {
            public Action Flush { get; set; }

            public Action<object> Enqueue { get; set; }

            public DateTime NextExecuteTime { get; set; }

            public Func<IEnumerable> GetAllTasks { get; set; }

            public int PollingIntervalSeconds { get; set; }

            public int ThreadCount { get; set; }
        }

        private Dictionary<Type, TaskOperator> m_Operators;
        private Timer m_PollingTimer;

        private TaskPool()
        {
            m_Operators = new Dictionary<Type, TaskOperator>();
            m_PollingTimer = new Timer(new TimerCallback(Polling), null, 100, 100);
        }

        public TaskPool RegisterQueue<T>(Action<IEnumerable<T>> taskHandler, int pollingIntervalSeconds = 10, int queueCapacity = 10240, int maxBatchCount = 200, int threadCount = 1) where T : class
        {
            Type t = typeof(T);
            TaskOperator op;
            if (m_Operators.TryGetValue(t, out op) == false)
            {
                op = new TaskOperator { NextExecuteTime = DateTime.Now.AddSeconds(-1), PollingIntervalSeconds = pollingIntervalSeconds, ThreadCount = (threadCount <= 0 ? 1 : threadCount) };
                IQueue<T> queue = new LocalMemoryQueue<T>(queueCapacity);
                op.Enqueue = task => queue.Enqueue((T)task);
                op.GetAllTasks = () => queue.GetAllItems();
                op.Flush = () =>
                {
                    try
                    {
                        List<T> list = new List<T>(maxBatchCount);
                        do
                        {
                            T msg;
                            if (queue.Dequeue(out msg) < 0) // queue is empty.
                            {
                                if (list.Count > 0)
                                {
                                    taskHandler(list);
                                }
                                break;
                            }
                            list.Add(msg);
                            if (list.Count >= maxBatchCount)
                            {
                                taskHandler(list);
                                list.Clear();
                                Thread.Sleep(10);
                            }
                        } while (true);
                    }
                    catch(Exception ex)
                    {
                        Logger.Error(ex);
                    }
                };
                m_Operators.Add(t, op);
            }
            return this;
        }

        public void Enqueue<T>(T task)
        {
            Type t = typeof(T);
            TaskOperator op;
            if (m_Operators.TryGetValue(t, out op) == false)
            {
                throw new ApplicationException("没有注册针对类型“" + t.FullName + "”的队列，请先调用Fly.Framework.Common.TaskPool的实例方法RegisterQueue<" + t.Name + ">方法来进行注册。");
            }
            op.Enqueue(task);
        }

        public IList<T> GetUnprocessdTasks<T>()
        {
            Type t = typeof(T);
            TaskOperator op;
            if (m_Operators.TryGetValue(t, out op) == false)
            {
                throw new ApplicationException("没有注册针对类型“" + t.FullName + "”的队列，请先调用Fly.Framework.Common.TaskPool的实例方法RegisterQueue<" + t.Name + ">方法来进行注册。");
            }
            return (IList<T>)op.GetAllTasks();
        }

        private void Polling(object state)
        {
            try
            {
                List<TaskOperator> list = new List<TaskOperator>(m_Operators.Values);
                foreach (var taskOperator in list)
                {
                    if (DateTime.Now >= taskOperator.NextExecuteTime)
                    {
                        taskOperator.NextExecuteTime = taskOperator.NextExecuteTime.AddSeconds(taskOperator.PollingIntervalSeconds);
                        for (var i = 0; i < taskOperator.ThreadCount; i++)
                        {
                            taskOperator.Flush.BeginInvoke(new AsyncCallback(r =>
                            {
                                TaskOperator op = r.AsyncState as TaskOperator;
                                if (op != null)
                                {
                                    op.Flush.EndInvoke(r);
                                }
                            }), taskOperator);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
