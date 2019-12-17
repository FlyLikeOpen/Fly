using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class WebsiteClosedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string url = ConfigurationManager.AppSettings["WebsiteClosedUrl"];
            if (string.IsNullOrWhiteSpace(url) == false)
            {
                filterContext.Result = new RedirectResult(url, false);
            }
        }
    }
}
