using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIImpls
{
    public static class Utils
    {
        public static IEnumerable<T> StringToList<T>(string convertStr, Func<string, T> converter)
        {
            return StringToList<T>(convertStr, r => r.Split(','), converter);
        }
        public static IEnumerable<T> StringToList<T>(string convertStr, Func<string,string[]> toStringArray, Func<string, T> converter)
        {
            if (string.IsNullOrWhiteSpace(convertStr))
            {
                return new List<T>(0);
            }
            var list = toStringArray(convertStr);
            var result = list.Where(r=> !string.IsNullOrWhiteSpace(r)).Select(r =>converter(r));
            return result != null ? result : new List<T>(0);
        }
        public static string GenerateSocketMsg(string msgKey,string msgBody)
        {
            return string.Format("msgKey:{0};msgbody:{1}", msgKey, msgBody);
        }
    }
}
