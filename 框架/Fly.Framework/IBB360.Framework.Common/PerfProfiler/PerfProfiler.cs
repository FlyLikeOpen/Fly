using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class PerfProfiler
    {
        private static bool? s_Disabled;
        public static bool Disabled
        {
            get
            {
                if (s_Disabled == null)
                {
                    string str = ConfigurationManager.AppSettings["PerfProfiler_Disabled"];
                    bool dis;
                    if (string.IsNullOrWhiteSpace(str) == false && bool.TryParse(str, out dis))
                    {
                        s_Disabled = dis;
                    }
                    else
                    {
                        s_Disabled = false;
                    }
                }
                return s_Disabled.Value;
            }
        }

        private static AsynQueueManager<ProfilerMessage> s_Queue;
        public static void Start(GlobalFilterCollection filters, Action<List<ProfilerMessage>> queueItemHandler, int boundedCapacity = 512, int itemCountPerBatchToHandle = 256,
            int dequeueThreadAmountMaxLimit = 1, int flushIntervalSeconds = 60, Action<Exception> exceptionHandler = null)
        {
            if (PerfProfiler.Disabled == false)
            {
                s_Queue = new AsynQueueManager<ProfilerMessage>(new LocalMemoryQueue<ProfilerMessage>(boundedCapacity), queueItemHandler, itemCountPerBatchToHandle, dequeueThreadAmountMaxLimit, flushIntervalSeconds, exceptionHandler);
                filters.Add(new PerfProfilerActionFilter(s_Queue));
            }
        }

        private ExecutionInfo info;
        public void OnMethodEnter()
        {
            if (PerfProfiler.Disabled == false)
            {
                info = new ExecutionInfo();
                info.Start();
            }
        }

        public void OnMethodExit(string requestUrl, string assemblyName, string classTypeName, string methodName)
        {
            if (PerfProfiler.Disabled == false)
            {
                if (info != null)
                {
                    info.Stop();
                    ProfilerMessage msg = info.ToMessage(requestUrl, assemblyName, classTypeName, methodName);
                    if (s_Queue != null)
                    {
                        s_Queue.Enqueue(msg);
                    }
                }
            }
        }

        public void OnMethodException()
        {
            if (PerfProfiler.Disabled == false)
            {
                if (info != null)
                {
                    info.HasThrowException = true;
                }
            }
        }
    }
}
