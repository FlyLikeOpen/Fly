using System;
using System.Web;
using AopAlliance.Intercept;
using IBB360.Framework.Common;

namespace IBB360.Framework.AOP
{
	public class PerformanceTuningMethodInterceptor : IMethodInterceptor
    {
		public object Invoke(IMethodInvocation invocation)
        {
            string assemblyName = invocation.TargetType.Assembly.GetName().Name;
            string className = invocation.TargetType.Name;
            string methodName = invocation.Method.Name;
            if(assemblyName == "IBB360.Socials" && className == "ActivityApi" && methodName == "BulkInsertProfilerMessages")
            {
                return invocation.Proceed();
            }
			PerfProfiler p = new PerfProfiler();
			p.OnMethodEnter();
			try
			{
				return invocation.Proceed();
			}
			catch
			{
				p.OnMethodException();
                throw;
			}
            finally
            {
                string requestUrl = "";
                if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.Url != null)
                    requestUrl = HttpContext.Current.Request.Url.ToString();
                p.OnMethodExit(requestUrl, assemblyName, className, methodName);
            }
		}
	}
}
