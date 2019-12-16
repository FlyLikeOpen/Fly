using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using Fly.Framework.Common;
using Fly.APIs.Common;

namespace Fly.WebService.Common
{
	public static class Utility
	{
		public static string ConvertIntListToString(IList<int> intList)
		{
			if (intList == null || intList.Count == 0)
				return string.Empty;

			StringBuilder outputBuilder = new StringBuilder();
			foreach (int intValue in intList)
			{
				if (outputBuilder.Length > 0)
					outputBuilder.Append(" ");

				outputBuilder.Append(intValue.ToString(CultureInfo.InvariantCulture));
			}

			return outputBuilder.ToString();
		}

        public static List<T> ConvertIntToEnum<T>(IEnumerable<int> list) where T : struct
        {
            if (typeof(T).IsEnum == false)
            {
                throw new NotSupportedException("泛型参数T只能是枚举类型");
            }
            if (list == null || list.Count() <= 0)
            {
                return new List<T>(0);
            }
            list = list.Distinct();
            List<T> rlist = new List<T>(list.Count());
            foreach (var v in list)
            {
                if (Enum.IsDefined(typeof(T), v))
                {
                    rlist.Add((T)(object)v);
                }
            }
            return rlist;
        }

		public static List<int> ConvertStringToIntList(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return new List<int>(0);

			List<int> intList = new List<int>();
			string[] tokens = input.Split(new string[] { " ", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string token in tokens)
			{
				int tmp;
				if (int.TryParse(token, out tmp))
					intList.Add(tmp);
			}

			return intList;
		}

        public static List<long> ConvertStringToLongList(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<long>(0);

            List<long> intList = new List<long>();
            string[] tokens = input.Split(new char[] { ' ', ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string token in tokens)
            {
                long tmp;
                if (long.TryParse(token, out tmp))
                    intList.Add(tmp);
            }

            return intList;
        }

		public static List<Guid> ConvertStringToGuidList(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return new List<Guid>(0);

			List<Guid> guidList = new List<Guid>();
			string[] tokens = input.Split(new string[] { " ", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string token in tokens)
			{
				Guid tmp = Guid.Empty;
				if (Guid.TryParse(token, out tmp))
					guidList.Add(tmp);
			}

			return guidList;
		}

		public static T GetPropertyValue<T>(object obj, string propertyName)
		{
			object v = null;
			try
			{
				v = DataBinder.Eval(obj, propertyName);
			}
			catch
			{
				return default(T);
			}
			return DataConvertor.GetValue<T>(v, propertyName, null);
		}

        public static T ParseFormVal<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default(T);

            string val = HttpContext.Current.Request.Form.Get(key);
            if (string.IsNullOrWhiteSpace(val))
                return default(T);

            try
            {
                return DataConvertor.GetValue<T>(val, key, null);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public static List<T> ParseFormToEnumList<T>(string key) where T : struct
        {
            if (typeof(T).IsEnum == false)
            {
                throw new NotSupportedException("泛型参数T只能是枚举类型");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                return new List<T>(0);
            }
            string val = HttpContext.Current.Request.Form.Get(key);
            if (string.IsNullOrWhiteSpace(val))
            {
                return new List<T>(0);
            }
            return ConvertIntToEnum<T>(ConvertStringToIntList(val));
        }

		/// <summary>
		/// 通过QueryString的key获取相应的value，并转换成所需的类型
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public static T ParseQueryStringVal<T>(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				return default(T);

			string val = HttpContext.Current.Request.QueryString.Get(key);
			if (string.IsNullOrWhiteSpace(val))
				return default(T);

			try
			{
				return DataConvertor.GetValue<T>(val, key, null);
			}
			catch (Exception)
			{
				return default(T);
			}
		}

        public static List<T> ParseQueryStringToEnumList<T>(string key) where T : struct
        {
            if (typeof(T).IsEnum == false)
            {
                throw new NotSupportedException("泛型参数T只能是枚举类型");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                return new List<T>(0);
            }
            string val = HttpContext.Current.Request.QueryString.Get(key);
            if (string.IsNullOrWhiteSpace(val))
            {
                return new List<T>(0);
            }
            return ConvertIntToEnum<T>(ConvertStringToIntList(val));
        }

		/// <summary>
		/// 删除Html文本里的Html标签
		/// </summary>
		/// <param name="html"></param>
		/// <returns></returns>
		public static string RemoveHTMLTag(string html)
		{
			if (string.IsNullOrWhiteSpace(html))
				return "";

			Regex regex = new Regex("<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			return regex.Replace(html, "");
		}

		/// <summary>
		/// 模糊化手机号码，返回类似格式的手机号：189****7554
		/// </summary>
		/// <param name="mobile"></param>
		/// <returns></returns>
		public static string FuzzyMobile(string mobile)
		{
			string fuzzy = mobile;
			if (string.IsNullOrWhiteSpace(mobile) == false && mobile.Length > 7)
				fuzzy = mobile.Substring(0, 3) + "****" + mobile.Substring(7);
			return fuzzy;
		}

		/// <summary>
		/// 模糊化身份证号，返回类似格式的身份证号：420528********0255
		/// </summary>
		/// <param name="idCardNo"></param>
		/// <returns></returns>
		public static string FuzzyIdCardNo(string idCardNo)
		{
			string fuzzy = idCardNo;
			if (string.IsNullOrWhiteSpace(idCardNo) == false && idCardNo.Length > 14)
				fuzzy = idCardNo.Substring(0, 6) + "********" + idCardNo.Substring(14);
			return fuzzy;
		}

		public static string ShowTimeFriendly(double seconds)
		{
			int tmp = (int)Math.Floor(seconds);
			return ShowTimeFriendly(tmp);
		}

		/// <summary>
		/// 友好的显示一段时间
		/// </summary>
		/// <param name="seconds">秒数</param>
		/// <returns></returns>
		public static string ShowTimeFriendly(int seconds)
		{
			if (seconds <= 0)
			{
				return "";
			}

			int minute = 60; // 60s
			int hour = minute * 60; // 60min

			string time = "";
			int remain = seconds;
			if (remain > 0 && remain >= hour)
			{
				int hh = remain / hour;
				time += hh + "小时";
				remain = remain % hour;
			}
			if (remain > 0 && remain >= minute)
			{
				int mm = remain / minute;
				time += mm + "分钟";
				remain = remain % minute;
			}
			if (remain > 0)
			{
				time += remain + "秒";
			}

			return time;
		}
	}
}