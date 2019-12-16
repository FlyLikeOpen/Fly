using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class RouteAuthenticationAttribute : AuthenticationAttribute
    {
        protected override bool CheckAuthentication()
        {
            var routeItem = RouteHelper.GetCurrentConfigRouteItem();
            bool need = routeItem == null ? false : routeItem.NeedLogin;
            if(need == false)
            {
                return true;
            }
            return base.CheckAuthentication();
        }
    }
}
