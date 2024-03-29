﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    internal class MobileCookiePersister : ICookiePersist
    {
        #region ICookiePersist Members

        public void Save(string cookieName, string cookieValue, Dictionary<string, string> parameters)
        {
            var collection = HttpContext.Current.Response.Headers;
            if (collection.AllKeys.Contains(MobileCookie.COOKIE_NAME))
            {
                string json = collection[MobileCookie.COOKIE_NAME];
                dynamic x1 = DynamicJson.Parse(json);
                x1[cookieName] = cookieValue;
                string v = x1.ToString();
                collection[MobileCookie.COOKIE_NAME] = v;
            }
            else
            {
                dynamic x1 = new DynamicJson();
                x1[cookieName] = cookieValue;
                string v = x1.ToString();
                HttpContext.Current.Response.AppendHeader(MobileCookie.COOKIE_NAME, v);
            }            
        }

        public string Get(string cookieName, Dictionary<string, string> parameters)
        {
            var collection = HttpContext.Current.Request.Headers;
            if (collection.AllKeys.Contains(MobileCookie.COOKIE_NAME))
            {
                string json = collection[MobileCookie.COOKIE_NAME];
                dynamic x1 = DynamicJson.Parse(json);
                if (x1.IsDefined(cookieName))
                {
                    object t = x1[cookieName];
                    return t == null ? string.Empty : t.ToString();
                }
            }
            return string.Empty;
        }

        public void Remove(string cookieName, Dictionary<string, string> parameters)
        {
            var collection = HttpContext.Current.Response.Headers;
            if (collection.AllKeys.Contains(MobileCookie.COOKIE_NAME))
            {
                string json = collection[MobileCookie.COOKIE_NAME];
                dynamic x1 = DynamicJson.Parse(json);
                if (x1.IsDefined(cookieName))
                {
                    x1.Delete(cookieName);
                    string v = x1.ToString();
                    collection[MobileCookie.COOKIE_NAME] = v;
                }
            }
        }

        #endregion
    }

    internal static class MobileCookie
    {
        internal const string COOKIE_NAME = "x-Fly-mobile-cookie";

        public static void CopyRequestHeaderToResponse()
        {
            var collection = HttpContext.Current.Request.Headers;
            if (collection.AllKeys.Contains(MobileCookie.COOKIE_NAME))
            {
                string v = collection[MobileCookie.COOKIE_NAME];
                HttpContext.Current.Response.AppendHeader(MobileCookie.COOKIE_NAME, v);
            }
        }
    }
}
