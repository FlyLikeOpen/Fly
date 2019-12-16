using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Fly.Framework.Common
{
    public class RouteRequestTraceAttribute : RequestTraceAttribute
    {
        protected override bool CheckNeedTrace(HttpContextBase httpContext)
        {
            var item = RouteHelper.GetCurrentConfigRouteItem();
            return base.CheckNeedTrace(httpContext) && (item == null || item.Trace);
        }
    }
}
