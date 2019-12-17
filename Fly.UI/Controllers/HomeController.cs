using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Fly.UI.Common;
using Fly.APIs.Common;
using Fly.Framework.Common;

namespace Fly.UI.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Logout()
        {
            Api<IStaffUserApi>.Instance.Logout();
            return Redirect(LinkUrl("Default"));
        }
        public ActionResult MobileLogout()
        {
            Api<IStaffUserMessageManagerApi>.Instance.SetStaffUserOffline(ContextManager.Current.UserId);
            Api<IStaffUserApi>.Instance.Logout();
            return Redirect(LinkUrl("Home/Mobile"));
        }
       
        public ActionResult CheckLogin(string userName, string password, bool remeberUserName)
        {
            var r = Api<IStaffUserApi>.Instance.Login(userName, password);
            if (r)
            {
                if (remeberUserName)
                {
                    Cookie.Set("LoginId", userName, 60 * 24 * 3650);
                }
                else
                {
                    Cookie.Remove("LoginId");
                }
            }
            return Json(new { succeed = r });
        }
	}
}