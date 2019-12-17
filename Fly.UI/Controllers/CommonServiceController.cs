using Fly.Framework.Common;
using Fly.APIs.Common;
using Fly.Objects;
using Fly.UI.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Fly.UI.Controllers
{
    public class CommonServiceController : BaseController
    {
        [HttpPost]
        public ActionResult LoadPartialViewForm(string viewPath)
        {
            StringWriter sw = new StringWriter();
            base.RenderPartial(sw, viewPath, null, null);
            string html = sw.ToString();
            return Json(new { html = html });
        }

        private object BuildSelectorReturnObj(object id, string name, string query)
        {
            return new { id = id.ToString(), name = name + (string.IsNullOrWhiteSpace(query) || name.Contains(query) ? string.Empty : " | 输入: " + query) };
        }

        // 搜索Staff用户
        [HttpPost]
        public string SearchUser(string query, bool? valid)
        {
            int t;
            var list = Api<IStaffUserApi>.Instance.Query(query, valid.GetValueOrDefault(true) ? new DataStatus[] { DataStatus.Enabled } : new DataStatus[] { DataStatus.Disabled }, null, null, 0, 200, out t);
            var rs = list.Select(x => new { id = x.Id, name = x.DisplayName });

            return rs.ToJsonString2();
        }
    }
}