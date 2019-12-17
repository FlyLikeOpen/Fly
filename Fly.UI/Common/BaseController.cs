using Fly.Framework.Common;
using Fly.APIs.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Mvc;

namespace Fly.UI.Common
{
    public class BaseController : Controller
    {
		protected void RenderPartial(StringWriter writer, string viewPath, object model, ViewDataDictionary viewData, string layoutPath = "~/Views/Shared/_Layout.cshtml")
		{
			TextWriter tw = TextWriter.Null;
			HtmlHelper htmlHelper = new HtmlHelper(new ViewContext(ControllerContext,
				new RazorView(ControllerContext, viewPath, layoutPath, false, null),
				new ViewDataDictionary(),
				new TempDataDictionary(), tw), new ViewPage());

			ViewDataDictionary dictionary = null;
			if (model == null)
			{
				if (viewData == null)
					dictionary = new ViewDataDictionary(htmlHelper.ViewData);
				else
					dictionary = new ViewDataDictionary(viewData);
			}
			else if (viewData == null)
				dictionary = new ViewDataDictionary(model);
			else
			{
				ViewDataDictionary dictionary2 = new ViewDataDictionary(viewData) { Model = model };
				dictionary = dictionary2;
			}
			ViewContext viewContext = new ViewContext(htmlHelper.ViewContext, htmlHelper.ViewContext.View, dictionary, htmlHelper.ViewContext.TempData, writer);
			var viewResult = ViewEngines.Engines.FindPartialView(viewContext, viewPath);
			if (viewResult.View != null)
				viewResult.View.Render(viewContext, writer);
		}

        protected string ResUrl(string relativePath)
        {
            return relativePath.ToLower();
        }

        protected string OssUrl(string fileName, int width = 0, int height = 0, int rotation = 0, bool addWaterMark = true)
        {
            return fileName;
        }

        protected string LinkUrl(string routeName, dynamic routeValues = null)
        {
            return RouteHelper.BuildUrl(routeName, routeValues);
        }

        protected string LinkUrl(string routeName, IDictionary<string, string> routeValues)
        {
            return RouteHelper.BuildUrl(routeName, routeValues);
        }

        protected string D(DateTime datetime)
        {
            return D(datetime, true);
        }

        protected string D(DateTime datetime, bool withTimePart)
        {
            if (withTimePart)
            {
                return datetime.ToString("yyyy-MM-dd HH:mm");
            }
            return datetime.ToString("yyyy-MM-dd");
        }

        protected string D(DateTime? datetime)
        {
            if (datetime == null)
            {
                return string.Empty;
            }
            return D(datetime.Value);
        }

        protected string D(DateTime? datetime, bool withTimePart)
        {
            if (datetime == null)
            {
                return string.Empty;
            }
            return D(datetime.Value, withTimePart);
        }

        protected string C(decimal? price)
        {
            return C(price, true);
        }

        protected string C(decimal? price, bool withMoneySymbol)
        {
            if (price == null)
            {
                return string.Empty;
            }
            return C(price.Value, withMoneySymbol);
        }

        protected string C(decimal price)
        {
            return C(price, true);
        }

        protected string C(decimal price, bool withMoneySymbol)
        {
            if (withMoneySymbol)
            {
                string tmp = Fly.Objects.Common.Currency.DefaultCurrency.Format(Math.Abs(price)); //string.Format("¥{0:N2}", Math.Abs(price));
				if (price < 0)
					tmp = "-" + tmp;
				return tmp;
            }
            return string.Format("{0:N2}", price);
        }

        protected JsonResult Json()
        {
            return Json(null);
        }

        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            //behavior = JsonRequestBehavior.AllowGet; //Request.IsLocal ? JsonRequestBehavior.AllowGet : behavior;
            return new MJsonResult { Data = data, ContentType = contentType, ContentEncoding = contentEncoding, JsonRequestBehavior = behavior };
        }

        protected JsonResult Json2(bool hasException, object msgData)
        {
            return Json(new { hasException, msgData});
        }

        protected JsonResult Json2()
        {
            return Json2(false, null);
        }

        protected T GetUrlParameterValue<T>(string parameterName)
        {
            object v;
            if (RouteHelper.TryGetRouteValueOfCurrentRoute(parameterName, out v) && v != null)
            {
                return DataConvertor.GetValue<T>(v, parameterName, null);
            }
            return default(T);
        }

        public string DisplayName(Guid? uid)
        {
            if (uid == null)
            {
                return string.Empty;
            }
            Objects.Common.StaffUser u = Api<IStaffUserApi>.Instance.Get(uid.Value);
            if (u != null)
            {
                return u.DisplayName;
            }
            return string.Empty;
        }
    }

    public class MJsonResult : JsonResult
    {
        public override void ExecuteResult(ControllerContext context)
        {
            object d = this.Data;
            if (d != null)
            {
                this.Data = new { error = false, data = d };
            }
            else
            {
                this.Data = new { error = false };
            }
            base.ExecuteResult(context);
        }
    }
}