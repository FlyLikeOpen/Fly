using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace Fly.Framework.Common
{
    internal class CustomControllerFactory : DefaultControllerFactory
    {
        protected override Type GetControllerType(RequestContext requestContext, string controllerName)
        {
            string use = requestContext.RouteData.DataTokens["UseCustomController"] as string;
            if (use == "1")
            {
                return typeof(CustomController);
            }
            return base.GetControllerType(requestContext, controllerName);
        }
    }
}
