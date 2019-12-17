using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Web.Mvc;
using Fly.Framework.Common;
using Fly.WebService.Common;

namespace Fly.WebService.Controllers
{
    public class InStockRequestController : BaseController
    {
        [HttpGet]
        public ActionResult Ping()
        {
            return Content("OK");
        }
    }
}