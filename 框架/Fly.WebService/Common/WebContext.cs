using System;
using System.Configuration;
using System.Linq;
using System.Web;
using Fly.Framework.Common;
using Fly.APIs.Common;
using Fly.Objects.Common;

namespace Fly.WebService.Common
{
    public class WebContext : ContextBase<HttpContext>
    {
        protected override HttpContext RealContext
        {
            get { return HttpContext.Current; }
        }

        protected override object GetValueFromRealContext(HttpContext context, string key)
        {
            if (key.Equals("IsAdmin"))
            {
                return "true";
            }
            if (key.Equals("AuthCheck"))
            {
                return new Func<string, bool>(HasPermission);
            }
            return context.Items[key];
        }

        private bool HasPermission(string functionAuthKey)
        {
            Guid uid = this.UserId;
            if (uid == Guid.Empty)
            {
                return false;
            }
            return Api<IStaffUserApi>.Instance.HasPermission(uid, functionAuthKey);
        }

        protected override void SetValueFromRealContext(HttpContext context, string key, object v)
        {
            context.Items[key] = v;
        }

        protected override Guid GetUserId(HttpContext context)
        {
            if (context == null)
            {
                return Guid.Empty;
            }
            StaffUser u = context.Items["Admin-User-Data"] as StaffUser;
            if (u != null)
            {
                return u.Id;
            }
            Guid uid = Guid.Empty; //Cookie.Get<Guid>("CenterStaffUser");
            string configedId = ConfigurationManager.AppSettings["SystemUserId"];
            if (string.IsNullOrWhiteSpace(configedId) == false)
            {
                Guid.TryParse(configedId.Trim(), out uid);
            }
            if (uid == Guid.Empty)
            {
                throw new ApplicationException("没有配置web.config文件中的Key为SystemUserId的appSettings节点");
                //return Guid.Empty;
            }
            u = Api<IStaffUserApi>.Instance.Get(uid);
            if (u == null)
            {
                throw new ApplicationException("web.config文件中的Key为SystemUserId的appSettings节点所配置的UserId："+ uid + "，找不到对应的StaffUser数据");
                //Cookie.Remove("CenterStaffUser");
                //return Guid.Empty;
            }
            context.Items["Admin-User-Data"] = u;
            return u.Id;
        }

        protected override string GetSessionId(HttpContext context)
        {
            throw new NotSupportedException();
        }
        
        protected override string GetClientIP(HttpContext context)
        {
            string result = String.Empty;
            result = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (null == result || result.Trim() == String.Empty)
            {
                result = context.Request.ServerVariables["REMOTE_ADDR"];
            }
            if (null == result || result.Trim() == String.Empty)
            {
                result = context.Request.UserHostAddress;
            }
            if (null == result || result.Trim() == String.Empty)
            {
                return "Unknown";
            }
            return result;
        }

        protected override string GetRequestUserAgent(HttpContext context)
        {
            return context.Request.UserAgent;
        }

        protected override DeviceType GetDeviceType(HttpContext context)
        {
            return DeviceNameDetector.GetDeviceType(context.Request.UserAgent);
        }

        private Geography ResolveLocation(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }
            string[] ary = json.Trim().Split(',');
            float tmp1, tmp2;
            if ((ary.Length == 2 || ary.Length == 3) && float.TryParse(ary[0], out tmp1) && float.TryParse(ary[1], out tmp2))
            {
                if (ary.Length == 3 && ary[2] != null)
                {
                    var d = ary[2].Trim().ToLower();
                    if (d == "weixin")
                    {
                        return LocationResolver.ConvertLocation((decimal)tmp2, (decimal)tmp1, GeographyType.QQ_Google_AmapMap, GeographyType.GPS);
                    }
                    else if (d == "baidu")
                    {
                        return LocationResolver.ConvertLocation((decimal)tmp2, (decimal)tmp1, GeographyType.BaiduMap, GeographyType.GPS);
                    }
                }
                return new Geography { Longitude = tmp1, Latitude = tmp2 }; // GPS
            }
            return null;
        }

        protected override Geography GetLocation(HttpContext context)
        {
            Geography g = context.Items["Client-Location-Data"] as Geography;
            if (g != null)
            {
                return g;
            }
            string key = "x-Fly-client-location";
            var collection = HttpContext.Current.Request.Headers;
            if (collection.AllKeys.Contains(key))
            {
                g = ResolveLocation(collection[key]);
            }
            if (g == null)
            {
                var cookie = HttpContext.Current.Request.Cookies[key];
                if (cookie != null)
                {
                    g = ResolveLocation(cookie.Value);
                }
            }
            if (g == null)
            {
                g = new Geography { Longitude = 0, Latitude = 0 };
            }
            context.Items["Client-Location-Data"] = g;
            return g;
        }
    }

    public static class WebContextExtensions
    {
        public static string GetCurrentUserLoginId(this IContext context)
        {
            Guid uid = context.UserId;
            if(uid == Guid.Empty)
            {
                return string.Empty;
            }
            var u = Api<IStaffUserApi>.Instance.Get(uid);
            if(u == null)
            {
                return string.Empty;
            }
            return u.LoginId;
        }

        public static string GetCurrentUserDisplayName(this IContext context)
        {
            Guid uid = context.UserId;
            if (uid == Guid.Empty)
            {
                return string.Empty;
            }
            var u = Api<IStaffUserApi>.Instance.Get(uid);
            if (u == null)
            {
                return string.Empty;
            }
            return u.DisplayName;
        }

        public static long GetCurrentUserIdNumber(this IContext context)
        {
            Guid uid = context.UserId;
            if (uid == Guid.Empty)
            {
                return 0;
            }
            var u = Api<IStaffUserApi>.Instance.Get(uid);
            if (u == null)
            {
                return 0;
            }
            return 0;
        }

        public static bool HasPermission(this IContext context, string permissionKey)
        {
            if(string.IsNullOrWhiteSpace(permissionKey))
            {
                return true;
            }
            Guid uid = context.UserId;
            if (uid == Guid.Empty)
            {
                return false;
            }
            return Api<IStaffUserApi>.Instance.HasPermission(uid, permissionKey);
        }

    }
}