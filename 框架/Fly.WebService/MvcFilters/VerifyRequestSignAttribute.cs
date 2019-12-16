using Fly.Framework.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Fly.WebService.MvcFilters
{
    public class VerifyRequestSignAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var routeItem = RouteHelper.GetCurrentConfigRouteItem();
            bool need = routeItem == null ? false : routeItem.NeedLogin;
            if (need == false) // 不需要验证签名
            {
                return;
            }
            string requestSign = filterContext.RequestContext.HttpContext.Request.Headers["IBB-Service-Sign"];
            string timestamp = filterContext.RequestContext.HttpContext.Request.Headers["IBB-Service-Timestamp"];
            string salt = filterContext.RequestContext.HttpContext.Request.Headers["IBB-Service-Salt"];
            DateTime time;
            if (string.IsNullOrWhiteSpace(timestamp) || DateTime.TryParse(timestamp, out time) == false)
            {
                throw new ApplicationException(string.Format("无效的timestamp；IBB-Service-Sign：{0}；IBB-Service-Timestamp：{1}；IBB-Service-Salt：{2}", requestSign, timestamp, salt));
            }
            if (time.AddMinutes(10) < DateTime.Now)
            {
                throw new ApplicationException(string.Format("此请求的签名已过期；IBB-Service-Sign：{0}；IBB-Service-Timestamp：{1}；IBB-Service-Salt：{2}", requestSign, timestamp, salt));
            }
            string privateKey = ConfigurationManager.AppSettings["ServicePrivateKey"];
            string sign = MD5Crypto(timestamp + salt + privateKey);
            if (sign != requestSign)
            {
                throw new ApplicationException(string.Format("请求的签名错误；IBB-Service-Sign：{0}；IBB-Service-Timestamp：{1}；IBB-Service-Salt：{2}", requestSign, timestamp, salt));
            }
        }

        private static string MD5Crypto(string str)
        {
            MD5 md5 = MD5.Create();
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                sb.Append(s[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}