using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Fly.Framework.Common
{
    internal class NormalCookie : ICookieEncryption
    {
        #region ICookieEncryption Members

        public string EncryptCookie<T>(T obj, Dictionary<string, string> parameters)
        {
            var tmp = (object)obj;
            if (tmp == null)
            {
                return null;
            }
            Type t = typeof(T);
            if (t == typeof(string) || t == typeof(int) || t == typeof(long) || t == typeof(double) || t == typeof(float) || t == typeof(decimal) || t == typeof(Guid))
            {
                return obj.ToString();
            }
            if (t == typeof(DateTime))
            {
                return ((DateTime)tmp).Ticks.ToString();
            }
            return Serialization.JsonSerialize2(obj);
        }

        public T DecryptCookie<T>(string cookieValue, Dictionary<string, string> parameters)
        {
            if(string.IsNullOrWhiteSpace(cookieValue))
            {
                return default(T);
            }
            Type t = typeof(T);
            if (t == typeof(string))
            {
                return (T)(object)cookieValue;
            }
            if (t == typeof(int))
            {
                int tmp = default(int);
                int.TryParse(cookieValue, out tmp);
                return (T)(object)tmp;
            }
            if (t == typeof(long))
            {
                long tmp = default(long);
                long.TryParse(cookieValue, out tmp);
                return (T)(object)tmp;
            }
            if (t == typeof(double))
            {
                double tmp = default(double);
                double.TryParse(cookieValue, out tmp);
                return (T)(object)tmp;
            }
            if (t == typeof(float))
            {
                float tmp = default(float);
                float.TryParse(cookieValue, out tmp);
                return (T)(object)tmp;
            }
            if (t == typeof(decimal))
            {
                decimal tmp = default(decimal);
                decimal.TryParse(cookieValue, out tmp);
                return (T)(object)tmp;
            }
            if (t == typeof(Guid))
            {
                Guid tmp = default(Guid);
                Guid.TryParse(cookieValue, out tmp);
                return (T)(object)tmp;
            }
            if (t == typeof(DateTime))
            {
                DateTime tmp = default(DateTime);
                long ticks;
                if (long.TryParse(cookieValue, out ticks) && ticks > 0)
                {
                    tmp = new DateTime(ticks);
                }
                return (T)(object)tmp;
            }
            return Serialization.JsonDeserialize2<T>(cookieValue);
        }

        #endregion
    }
}
