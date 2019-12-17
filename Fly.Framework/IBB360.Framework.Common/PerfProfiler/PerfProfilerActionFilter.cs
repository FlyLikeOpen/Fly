using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class PerfProfilerActionFilter : IActionFilter
    {
        private AsynQueueManager<ProfilerMessage> m_Queue;
        public PerfProfilerActionFilter(AsynQueueManager<ProfilerMessage> queue)
        {
            m_Queue = queue;
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (PerfProfiler.Disabled == false)
            {
                ExecutionInfo info = new ExecutionInfo();
                filterContext.RequestContext.HttpContext.Items["ibb360_PerfProfilerActionFilter"] = info;
                info.Start();
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (PerfProfiler.Disabled == false)
            {
                ExecutionInfo info = filterContext.RequestContext.HttpContext.Items["ibb360_PerfProfilerActionFilter"] as ExecutionInfo;
                if(info != null)
                {
                    info.Stop();
                    ProfilerMessage msg = info.ToMessage(filterContext.RequestContext.HttpContext.Request.RawUrl, "none", "none", "none");
                    if (m_Queue != null)
                    {
                        m_Queue.Enqueue(msg);
                    }
                }
            }
        }
    }
}
