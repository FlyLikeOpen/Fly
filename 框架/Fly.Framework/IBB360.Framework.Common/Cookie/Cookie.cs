using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Fly.Framework.Common
{
    public static class Cookie
    {
        #region Private Member

        private static void LoadConfig(string nodeName, out string persistType, out string securityLevel,
            out Dictionary<string, string> parameters)
        {
            var entity = CookieConfig.GetCookieConfig(nodeName);
            parameters = entity.Properties;
            persistType = entity.PersistType;
            securityLevel = entity.SecurityLevel;
        }

        private static ICookiePersist s_Mobile = new MobileCookiePersister();
        private static ICookiePersist s_Web = new WebCookiePersister();
        private static ICookiePersist CreatePersister(string persistType)
        {
            switch (persistType.ToUpper())
            {
                case "MOBILE":
                    return s_Mobile;
                case "WEB":
                    return s_Web;
                case "AUTO":
                    return DiscernPersistType();
                default:
                    return (ICookiePersist)Activator.CreateInstance(Type.GetType(persistType, true));
            }
        }

        private static ICookiePersist DiscernPersistType()
        {
            if (HttpContext.Current.Request.Headers.AllKeys.Contains(MobileCookie.COOKIE_NAME)
                //|| HttpContext.Current.Request.Browser.IsMobileDevice
                //|| (HttpContext.Current.Request.QueryString.AllKeys.Count()>0&&HttpContext.Current.Request.QueryString["type"].ToUpper()=="MOBILE")
                )
            {
                return s_Mobile;
            }
            return s_Web;
        }

        private static ICookieEncryption s_Normal = new NormalCookie();
        private static ICookieEncryption s_Security = new SecurityCookie();
        private static ICookieEncryption s_HighSecurity = new HighSecurityCookie();
        private static ICookieEncryption CreateCookieHelper(string securityLevel)
        {
            switch (securityLevel.ToUpper())
            {
                case "LOW":
                    return s_Normal;
                case "MIDDLE":
                    return s_Security;
                case "HIGH":
                    return s_HighSecurity;
                default:
                    return (ICookieEncryption)Activator.CreateInstance(Type.GetType(securityLevel, true));
            }
        }

        private static string GetCookieName(string nodeName, Dictionary<string, string> parameters)
        {
            string name;
            if (parameters != null && parameters.TryGetValue("cookieName", out name)
                && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            return nodeName;
        }

        #endregion

        public static void Set<T>(string nodeName, T obj, int? expiredMinutes = null)
        {
            string persistType;
            string securityLevel;
            Dictionary<string, string> parameters;
            LoadConfig(nodeName, out persistType, out securityLevel, out parameters);

            if (expiredMinutes.HasValue)
            {
                parameters["expires"] = expiredMinutes.Value.ToString();
            }

            ICookiePersist persister = CreatePersister(persistType);
            ICookieEncryption safer = CreateCookieHelper(securityLevel);
            string cookieValue = safer.EncryptCookie<T>(obj, parameters);
            persister.Save(GetCookieName(nodeName, parameters), cookieValue, parameters);
        }

        public static T Get<T>(string nodeName)
        {
            string persistType;
            string securityLevel;
            Dictionary<string, string> parameters;
            LoadConfig(nodeName, out persistType, out securityLevel, out parameters);

            ICookiePersist persister = CreatePersister(persistType);
            string cookieValue = persister.Get(GetCookieName(nodeName, parameters), parameters);
            ICookieEncryption safer = CreateCookieHelper(securityLevel);
            return safer.DecryptCookie<T>(cookieValue, parameters);
        }

        public static void Remove(string nodeName)
        {
            string persistType;
            string securityLevel;
            Dictionary<string, string> parameters;
            LoadConfig(nodeName, out persistType, out securityLevel, out parameters);

            ICookiePersist persister = CreatePersister(persistType);
            persister.Remove(GetCookieName(nodeName, parameters), parameters);
        }
    }
}
