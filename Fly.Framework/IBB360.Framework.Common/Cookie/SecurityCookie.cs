using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Fly.Framework.Common
{
    internal class SecurityCookie : ICookieEncryption
    {
        #region ICookieEncryption Members

        public string EncryptCookie<T>(T obj, Dictionary<string, string> parameters)
        {
            string strCookieValue = string.Empty;
            string strEncCookieValue = string.Empty;
            string strSHA1Sign = string.Empty;

            strCookieValue = Serialization.JsonSerialize2(obj);
            
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

            try
            {
                if (cookieValue.Length <= 44)
                {
                    return result;
                }
                strSHA1Sign = cookieValue.Substring(0, 44);
                strEncCookieValue = cookieValue.Substring(44);
                // check sign
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
                result = Serialization.JsonDeserialize2<T>(strContent);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("SecurityCookie反编码Cookie出错。错误信息：\r\n" + ex);
                return result;
            }
        }

        #endregion
    }
}
