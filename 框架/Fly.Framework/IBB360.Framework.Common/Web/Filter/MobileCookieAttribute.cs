using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class MobileCookieAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            MobileCookie.CopyRequestHeaderToResponse();
        }
    }
}
