using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class CustomController : Controller
    {
        public ActionResult Common()
        {
            string viewPath = RouteData.DataTokens["viewPath"] as string;
            return View(viewPath);
        }
    }
}
