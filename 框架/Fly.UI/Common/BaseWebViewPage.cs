using Fly.Framework.Common;
using Fly.APIs.Common;
using System;
using System.Collections.Generic;
using System.Web.UI;

namespace Fly.UI.Common
{
    public abstract class BaseWebViewPage<TModel> : System.Web.Mvc.WebViewPage<TModel>
    {
		public string PageTitle
		{
			get
			{
				object tmp;
				if (ViewData.TryGetValue("_page_title", out tmp) && tmp != null)
				{
					return tmp as string;
				}
				return "";
			}
			set
			{
				ViewData.Add("_page_title", value);
			}
		}

        public string RouteName { get { return RouteHelper.CurrentRouteName; } }

        public string ControllerName { get { return RouteHelper.CurrentControllerName; } }

        public string ActionName { get { return RouteHelper.CurrentActionName; } }

        public string R(string relativePath)
        {
            return relativePath.ToLower();
        }

        public string OssUrl(string fileName, int width = 0, int height = 0, int rotation = 0, bool addWaterMark = true)
        {
            return fileName;
        }

        public string LinkUrl(string routeName, dynamic routeValues = null)
        {
            return RouteHelper.BuildUrl(routeName, routeValues);
        }

        public string LinkUrl(string routeName, IDictionary<string, string> routeValues)
        {
            return RouteHelper.BuildUrl(routeName, routeValues);
        }

        public string D(DateTime datetime)
        {
            return D(datetime, true);
        }

        public string D(DateTime datetime, bool withTimePart)
        {
            if (withTimePart)
            {
                return datetime.ToString("yyyy-MM-dd HH:mm");
            }
            return datetime.ToString("yyyy-MM-dd");
        }

        public string D(DateTime? datetime)
        {
            if (datetime == null)
            {
                return string.Empty;
            }
            return D(datetime.Value);
        }

        public string D(DateTime? datetime, bool withTimePart)
        {
            if (datetime == null)
            {
                return string.Empty;
            }
            return D(datetime.Value, withTimePart);
        }

        public string C(decimal? price)
        {
            return C(price, true);
        }

        public string C(decimal? price, bool withMoneySymbol)
        {
            if (price == null)
            {
                return string.Empty;
            }
            return C(price.Value, withMoneySymbol);
        }

        public string C(decimal price)
        {
            return C(price, true);
        }

        public string C(decimal price, bool withMoneySymbol)
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

		public string DisplayName(Guid? uid)
		{
			if (!uid.HasValue)
				return string.Empty;
			return DisplayName(uid.Value);
		}

		public string DisplayName(Guid uid)
		{
			string name = string.Empty;
			Objects.Common.StaffUser u = Api<IStaffUserApi>.Instance.Get(uid);
			if (u != null)
				name = u.DisplayName;
			return name;
		}

        public T GetUrlParameterValue<T>(string parameterName)
        {
            object v;
            if (RouteHelper.TryGetRouteValueOfCurrentRoute(parameterName, out v) && v != null)
            {
                return DataConvertor.GetValue<T>(v, parameterName, null);
            }
            return default(T);
        }
        
        /// <summary>
        /// 解析View的非强类型Model上的属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public T ParseModel<T>(object model, string propertyName)
        {
            object v = null;
            try
            {
                v = DataBinder.Eval(model, propertyName);
            }
            catch
            {
                return default(T);
            }
            return DataConvertor.GetValue<T>(v, propertyName, null);
        }

        /// <summary>
        /// 解析View的非强类型Model上的属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public T ParseModel<T>(string propertyName)
        {
            return this.ParseModel<T>(this.Model, propertyName);
        }
    }

    public abstract class BaseWebViewPage : BaseWebViewPage<dynamic>
    {

    }
}