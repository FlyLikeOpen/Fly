using Fly.Framework.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Fly.UI.Common
{
    public class CheckCanLoginRouteAuthenticationAttribute : RouteAuthenticationAttribute
    {
        protected override bool CheckAuthentication()
        {
            var routeItem = RouteHelper.GetCurrentConfigRouteItem();
            bool need = routeItem == null ? false : routeItem.NeedLogin;
            if (need == false)
            {
                return true;
            }
            return base.CheckAuthentication() // 已经登录
                && ContextManager.Current.HasPermission("Fly_Login"); // 并且要有登录系统的权限
        }
    }
}