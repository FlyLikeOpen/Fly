using Fly.Framework.Common;
using Fly.Objects.Common;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Fly.UI.Common
{
    public static class BizHtmlHelperExtensions
    {
        /// <summary>
        /// 生成前台通用的可输可选Selector，需和JS，Controller配合
        /// </summary>
        /// <param name="name">input 标签的name, id, class</param>
        /// <param name="cls">整个控件的class</param>
        /// <param name="bname">当前选中的Label，多选的话需要拼接成字符串</param>
        /// <param name="bid">当前选中的Value，多选的话需要拼接成字符串</param>
        /// <param name="disabled">是否禁用</param>
        /// <param name="notInit">是否不初始化</param>
        /// <param name="extraParams">额外的参数，会传到查询的Controller里</param>
        /// <param name="multiple">是否允许多选</param>
        /// <returns></returns>
        private static MvcHtmlString IBBComboSelector(string name, string cls, string bname, string bid, bool? disabled = false, bool? notInit = false, Dictionary<string, string> extraParams = null, bool? multiple = false)
        {
            string disabledVal = disabled.GetValueOrDefault() ? "1" : "0";
            string notInitVal = notInit.GetValueOrDefault() ? "1" : "0";
            string multipleVal = multiple.GetValueOrDefault() ? "1" : "0";

            string extraJson = "";
            if (extraParams != null && extraParams.Count > 0)
                extraJson = extraParams.ToJsonString2();

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<div class='{0}' data-notinit='{1}'>", cls, notInitVal);
            sb.AppendFormat("<div class='control-holder'></div>");
            sb.AppendFormat("<input type='hidden' name='{0}' id='{0}' class='value-holder {0}' data-names='{1}' data-ids='{2}' data-disabled='{3}' data-extra='{4}' data-multiple='{5}' value='{2}' />", name, bname, bid, disabledVal, extraJson, multipleVal);
            sb.AppendFormat("</div>");

            return MvcHtmlString.Create(sb.ToString());
        }

        // 防止Name中有特殊字符，使用此字符来代理英文逗号作为分隔符
        private const string IBBComboSelectorNameSeparator = "#$#";

        public static MvcHtmlString UserSelector(this HtmlHelper htmlHelper, string name, StaffUser user = null, bool? disabled = false, bool? notInit = false)
        {
            string bname = user == null ? "" : user.DisplayName;
            string bid = user == null ? "" : user.Id.ToString();

            return IBBComboSelector(name, "user-selector", bname, bid, disabled, notInit);
        }


        public static MvcHtmlString RenderStrList(this HtmlHelper htmlHelper, IEnumerable<string> list)
        {
            string str = string.Empty;
            if (list != null && list.Any())
            {
                str = string.Join("<br/>", list);
            }
            return MvcHtmlString.Create(str);
        }
    }
}