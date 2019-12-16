using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Fly.Framework.Common
{
    internal class HighSecurityCookie : ICookieEncryption
    {
        private static string GetClientIP()
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null)
            {
                return string.Empty;
            }
            string result = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (null == result || result.Trim() == String.Empty)
            {
                result = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }

            if (null == result || result.Trim() == String.Empty)
            {
                result = HttpContext.Current.Request.UserHostAddress;
            }
            if (null == result)
            {
                return string.Empty;
            }
            return result.Trim();
        }

        #region ICookieEncryption Members

        public string EncryptCookie<T>(T obj, Dictionary<string, string> parameters)
        {
            string strCookieValue = string.Empty;
            string strEncCookieValue = string.Empty;
            string strSHA1Sign = string.Empty;
            string[] arrayCookieValue = new string[3];

            int securityExpires = 0;
            int.TryParse(parameters["securityExpires"], out securityExpires);

            arrayCookieValue[0] = Serialization.JsonSerialize2(obj);
            arrayCookieValue[1] = DateTime.Now.AddMinutes(securityExpires).ToString();
            arrayCookieValue[2] = GetClientIP();
            strCookieValue = Serialization.JsonSerialize2(arrayCookieValue);

            strEncCookieValue = Crypto.GetCrypto(CryptoType.DES).Encrypt(strCookieValue + parameters["rc4key"]).Trim();
            strSHA1Sign = Crypto.GetCrypto(CryptoType.SHA256).Encrypt(strEncCookieValue + parameters["hashkey"]);
            strEncCookieValue = HttpUtility.UrlEncode(strEncCookieValue);
            strEncCookieValue = strSHA1Sign + strEncCookieValue;

            return strEncCookieValue;
        }

        public T DecryptCookie<T>(string cookieValue, Dictionary<string, string> parameters)
        {
            T result = default(T);
            string strEncCookieValue = string.Empty;
            string strContent = string.Empty;
            string strSHA1Sign = string.Empty;
            string strShA1Temp = string.Empty;
            string[] arrayCookieValue = new string[2];

            try
            {
                if (cookieValue.Length <= 44)
                {
                    return result;
                }
                strSHA1Sign = cookieValue.Substring(0, 44);
                strEncCookieValue = cookieValue.Substring(44);
                //  check sign
                strShA1Temp = Crypto.GetCrypto(CryptoType.SHA256).Encrypt(HttpUtility.UrlDecode(strEncCookieValue).Trim() + parameters["hashkey"]);
                if (strSHA1Sign != strShA1Temp)
                {
                    return result;
                }
                strEncCookieValue = HttpUtility.UrlDecode(strEncCookieValue);
                strContent = Crypto.GetCrypto(CryptoType.DES).Decrypt(strEncCookieValue);
                string rc4key = parameters["rc4key"];
                if (strContent.EndsWith(rc4key) == false)
                {
                    return result;
                }
                strContent = strContent.Substring(0, strContent.Length - rc4key.Length);
                if (strContent.Length <= 0)
                {
                    return result;
                }

                arrayCookieValue = Serialization.JsonDeserialize2<string[]>(strContent);
                if (arrayCookieValue != null && arrayCookieValue.Length == 3)
                {
                    if (DateTime.Parse(arrayCookieValue[1]) > DateTime.Now && GetClientIP() == arrayCookieValue[2])
                    {
                        result = Serialization.JsonDeserialize2<T>(arrayCookieValue[0]);

                        int securityExpires = 0;
                        int.TryParse(parameters["securityExpires"], out securityExpires);
                        Cookie.Set<T>(parameters["nodeName"], result, securityExpires);
                    }
                }
                return result;
            }
            catch(Exception ex)
            {
                Logger.Error("HighSecurityCookie反编码Cookie出错。错误信息：\r\n" + ex);
                return result;
            }
        }

        #endregion
    }
}
