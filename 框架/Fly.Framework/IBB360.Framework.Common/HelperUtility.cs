using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Text;
using System.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Fly.Framework.Common
{
	/// <summary>
	/// Helper Functions
	/// </summary>
	public static class HelperUtility
    {
        private const string SALT = "ibb360_bmhqg.com_dai8dsdf";
        private const string SUFFIX = "_sdf32DFm2";
        private const int CHECK_CODE_EXPIRED_MIN = 5;
        private static string GenerateCheckCode()
        {
            char code;
            var checkCode = String.Empty;
            var random = new Random(Guid.NewGuid().ToString().GetHashCode());

            for (var i = 0; i < 5; i++)
            {
                var number = random.Next();

                if (number % 2 == 0)
                    code = (char)('1' + (char)(number % 9));
                else
                    code = (char)('A' + (char)(number % 26));
                checkCode += code.ToString();
            }
            return checkCode;
        }

        public static byte[] GenerateCheckCodeImage(string checkType)
        {
            string checkCode = GenerateCheckCode();
            var obj = new { Code = checkCode, Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            string json = obj.ToJsonString2();
            string sign = Crypto.GetCrypto(CryptoType.MD5).Encrypt(json + SALT);
            string json2 = new { Data = Crypto.GetCrypto(CryptoType.DES).Encrypt(json), Sign = sign }.ToJsonString2();
            Cookie.Set<string>(checkType + SUFFIX, json2, CHECK_CODE_EXPIRED_MIN);
            Bitmap image = null;
            Graphics g = null;
            try
            {
                image = new Bitmap((int)Math.Ceiling((checkCode.Length * 12.5)), 22);
                g = Graphics.FromImage(image);
                //生成随机生成器

                var random = new Random();

                //清空图片背景色

                g.Clear(Color.White);

                //画图片的背景噪音线

                for (var i = 0; i < 10; i++)
                {
                    var x1 = random.Next(image.Width);
                    var x2 = random.Next(image.Width);
                    var y1 = random.Next(image.Height);
                    var y2 = random.Next(image.Height);

                    g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
                }

                var font = new Font("Arial", 13, (FontStyle.Bold));
                var brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Black, Color.Gray, 1.2f, true);
                g.DrawString(checkCode, font, brush, 2, 1);

                //画图片的前景噪音点

                for (var i = 0; i < 20; i++)
                {
                    var x = random.Next(image.Width);
                    var y = random.Next(image.Height);

                    image.SetPixel(x, y, Color.FromArgb(0x8b, 0x8b, 0x8b));
                }

                //画图片的边框线

                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);

                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                    return ms.ToArray();
                }
            }
            finally
            {
                if (g != null)
                {
                    g.Dispose();
                }
                if (image != null)
                {
                    image.Dispose();
                }
            }
        }

        public static bool VerifyCheckCode(string checkType, string checkCode)
        {
            try
            {
                string json2 = Cookie.Get<string>(checkType + SUFFIX);
                if (string.IsNullOrWhiteSpace(json2))
                {
                    return false;
                }
                var obj = DynamicJson.Parse(json2);
                string json = Crypto.GetCrypto(CryptoType.DES).Decrypt(obj.Data);
                string sign = obj.Sign;
                string sign2 = Crypto.GetCrypto(CryptoType.MD5).Encrypt(json + SALT);
                if (sign2 != sign)
                {
                    return false; // 验证签名失败
                }
                var obj2 = DynamicJson.Parse(json);
                string checkCode2 = obj2.Code;
                string tm = obj2.Timestamp;
                if (string.Equals(checkCode2, checkCode, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    return false; //验证码输入错误
                }
                DateTime d;
                if (DateTime.TryParse(tm, out d) == false)
                {
                    return false; // 时间戳格式错误
                }
                if (d.AddMinutes(CHECK_CODE_EXPIRED_MIN + 1) <= DateTime.Now)
                {
                    return false; // 时间戳过期
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public static bool IsMobileFormat(string str)
        {
            char[] parity = new char[] { '9', '8', '7', '6', '5', '4', '3', '2', '1', '0' };
            foreach (var c in str)
            {
                if (parity.Contains(c) == false)
                {
                    return false;
                }
            }
            // --- 之所以要增加上面的检查，是因为如果输入里有全角的数字字符，也能通过后面的正则式检查
            // 所以通过增加上面的数字字符检查，来排除掉全角的数字字符
            const string regPattern = "^1[3|4|5|6|7|8|9]\\d{9}$";
            return Regex.IsMatch(str, regPattern);
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
            {
                fuzzy = mobile.Substring(0, 3) + "****" + mobile.Substring(7);
            }
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
            {
                fuzzy = idCardNo.Substring(0, 6) + "********" + idCardNo.Substring(14);
            }
            return fuzzy;
        }

        private static List<int> s_ProvinceGBCodeList = new List<int>
        {
            11, 12, 13, 14, 15, 21, 22, 23, 31, 32, 33, 34, 35, 36, 37, 41, 42, 43, 44, 45, 46, 50,
            51, 52, 53, 54, 61, 62, 63, 64, 65, 71, 81, 82, 91
        };

        public static bool IsValidIdentityCode(string code)
        {
            string areaGBCode;
            DateTime birthday;
            return IsValidIdentityCode(code, out areaGBCode, out birthday);
        }

        public static bool IsValidIdentityCode(string code, out string areaGBCode)
        {
            DateTime birthday;
            return IsValidIdentityCode(code, out areaGBCode, out birthday);
        }

        public static bool IsValidIdentityCode(string code, out DateTime birthday)
        {
            string areaGBCode;
            return IsValidIdentityCode(code, out areaGBCode, out birthday);
        }

        public static bool IsValidIdentityCode(string code, out string areaGBCode, out DateTime birthday)
        {
            areaGBCode = null;
            birthday = default(DateTime);
            if (code == null)
            {
                return false;
            }
            code = code.Trim();
            if ((code.Length != 15 && code.Length != 18) || Regex.IsMatch(code, @"\d{6}(19|20)?\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}(\d|X|x)$") == false)
            {
                return false;
            }
            int pro = Convert.ToInt32(code.Substring(0, 2));
            if (s_ProvinceGBCodeList.Contains(pro) == false) // 验证省份
            {
                return false;
            }
            areaGBCode = code.Substring(0, 6);
            string birthdayStr = code.Length == 18 ? code.Substring(6, 8).Insert(6, "-").Insert(4, "-") : ("19" + code.Substring(6, 6).Insert(4, "-").Insert(2, "-"));
            if (DateTime.TryParse(birthdayStr, out birthday) == false) // 验证生日日期 
            {
                return false;
            }
            if (code.Length == 18) // 验证18位身份证的校验码
            {
                //加权因子
                int[] factor = new int[] { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
                //校验位
                string[] parity = new string[] { "1", "0", "X", "9", "8", "7", "6", "5", "4", "3", "2" };
                int sum = 0;
                for (var i = 0; i < 17; i++)
                {
                    sum += Convert.ToInt32(code[i].ToString()) * factor[i];
                }
                string last = parity[sum % 11];
                if (string.Equals(last, code[17].ToString(), StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    return false;
                }
            }
            return true;
        }

        ///// <summary>
        ///// 格式化距离，友好输出
        ///// </summary>
        ///// <param name="distance"></param>
        ///// <returns></returns>
        //public static string FormatDistance(float distance)
        //{
        //    distance = distance / 1000;

        //    string str = "";
        //    if (distance > 0)
        //    {
        //        if (distance > 30)
        //            str = "很远";
        //        else
        //            str = string.Format("< {0}km", distance.ToString("0.0"));
        //    }
        //    return str;
        //}

        ///// <summary>
        ///// 构造显示评分星星的html
        ///// </summary>
        ///// <param name="point"></param>
        ///// <returns></returns>
        //public static string BuildStars(float point)
        //{
        //    if (point < 0) { point = 0; }
        //    if (point > 5) { point = 5; }

        //    int intVal = Convert.ToInt32(Math.Floor(point));
        //    double floatVal = point - intVal;

        //    int fullStarCount = intVal;
        //    int halfStarCount = 0;
        //    int emptyStarCount = 5 - intVal;

        //    if (floatVal != 0)
        //    {
        //        halfStarCount = 1;
        //        emptyStarCount--;
        //    }

        //    string stars = "";
        //    for (int i = 1; i <= fullStarCount; i++)
        //    {
        //        stars += "<i class=\"fa fa-star\"></i>";
        //    }
        //    for (int i = 1; i <= halfStarCount; i++)
        //    {
        //        stars += "<i class=\"fa fa-star-half-o\"></i>";
        //    }
        //    for (int i = 1; i <= emptyStarCount; i++)
        //    {
        //        stars += "<i class=\"fa fa-star-o\"></i>";
        //    }

        //    return stars;
        //}

        /// <summary>
        /// 比较两个集合是否一样，一样的标准为：集合同为null或者集合里的元素都一样（顺序可以不一样，但数量要一样，且元素本身比较结果为一致）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <returns></returns>
        public static bool CompareListItems<T>(IEnumerable<T> list1, IEnumerable<T> list2) where T : IComparable
        {
            if (list1 == null && list2 == null)
            {
                return true;
            }
            if ((list1 == null && list2 != null) || (list1 != null && list2 == null))
            {
                return false;
            }
            if (list1.Count() != list2.Count())
            {
                return false;
            }
            foreach (var n in list1)
            {
                var m = list2.Where(x => x.CompareTo(n) == 0);
                if (m.Count() != 1)
                {
                    return false;
                }
            }
            foreach (var n in list2)
            {
                var m = list1.Where(x => x.CompareTo(n) == 0);
                if (m.Count() != 1)
                {
                    return false;
                }
            }
            return true;
        }

        public static T ToValue<T>(this DataRow row, int fieldIndex)
        {
            object fieldValue = row[fieldIndex];
            if (fieldValue == null || fieldValue == DBNull.Value)
            {
                return default(T);
            }

            //return (T)fieldValue;
            return Fly.Framework.Common.DataConvertor.GetValue<T>(fieldValue, null, null);
        }

        public static T ToValue<T>(this DataRow row, string fieldName)
        {
            object fieldValue = row[fieldName];
            if (fieldValue == null || fieldValue == DBNull.Value)
            {
                return default(T);
            }

            //return (T)fieldValue;
            return Fly.Framework.Common.DataConvertor.GetValue<T>(fieldValue, null, null);
        }

        public static string FormatXml(string unformattedXml, char indentChar = '\t')
        {
            XmlDocument xd = new XmlDocument();
            xd.LoadXml(unformattedXml);
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            XmlTextWriter xtw = null;
            try
            {
                xtw = new XmlTextWriter(sw);
                xtw.Formatting = Formatting.Indented;
                xtw.Indentation = 1;
                xtw.IndentChar = indentChar;
                xd.WriteTo(xtw);
            }
            finally
            {
                if (xtw != null)
                    xtw.Close();
            }
            return sb.ToString();
        }


        public static IList<T> BulkGetWithCache<T, K>(IEnumerable<K> idList, Func<IEnumerable<K>, IList<T>> realGetter, Func<T, K> keyPropertyGetter,
            CacheGetter<T> cacheGeter, CacheSetter cacheSetter, Func<K, string> keyGenerator)
        {
            if (idList == null || idList.Count() <= 0)
            {
                return new List<T>(0);
            }
            idList = idList.Distinct();
            List<T> list = new List<T>(idList.Count());
            List<K> noCacheIdList = new List<K>(idList.Count());
            foreach (var id in idList)
            {
                string key = keyGenerator(id);
                bool find;
                var t = cacheGeter(key, out find);
                if (find)
                {
                    list.Add(t);
                }
                else
                {
                    noCacheIdList.Add(id);
                }
            }
            if (noCacheIdList.Count > 0)
            {
                var rst = realGetter(noCacheIdList);
                if (rst != null && rst.Count > 0)
                {
                    list.AddRange(rst);
                    foreach (T obj in rst)
                    {
                        K id = keyPropertyGetter(obj);
                        string key = keyGenerator(id);
                        cacheSetter(key, obj);
                    }
                }
            }
            return list;
        }

        public static IList<T> BulkGetWithHttpCache<T, K>(IEnumerable<K> idList, Func<IEnumerable<K>, IList<T>> realGetter, Func<T, K> keyPropertyGetter, Func<K, string> keyGenerator)
        {
            return BulkGetWithCache<T, K>(idList, realGetter, keyPropertyGetter, Cache.GetFromHttpContextCache<T>, Cache.SetIntoHttpContextCache, keyGenerator);
        }

        public static IList<T> BulkGetByGuidIdWithHttpCache<T>(IEnumerable<Guid> idList, Func<IEnumerable<Guid>, IList<T>> realGetter, Func<T, Guid> keyPropertyGetter = null)
        {
            if (keyPropertyGetter == null)
            {
                keyPropertyGetter = obj => (Guid)Invoker.PropertyGet(obj, "Id");
            }
            return BulkGetWithCache<T, Guid>(idList, realGetter, keyPropertyGetter, Cache.GetFromHttpContextCache<T>, Cache.SetIntoHttpContextCache, id => "Get_" + id);
        }

        private static T GetFromLocalCache<T>(string key, out bool existedInCache)
        {
            object x = Cache.GetLocalCache(key);
            if (x != null)
            {
                existedInCache = true;
                return (T)x;
            }
            else
            {
                existedInCache = false;
                return default(T);
            }
        }

        public static IList<T> BulkGetWithLocalCache<T, K>(IEnumerable<K> idList, Func<IEnumerable<K>, IList<T>> realGetter, Func<T, K> keyPropertyGetter, Func<K, string> keyGenerator)
        {
            CacheSetter cacheSetter = (k, o) => Cache.SetLocalCache(k, o);
            return BulkGetWithCache<T, K>(idList, realGetter, keyPropertyGetter, GetFromLocalCache<T>, cacheSetter, keyGenerator);
        }

        public static IList<T> BulkGetByGuidIdWithLocalCache<T>(IEnumerable<Guid> idList, Func<IEnumerable<Guid>, IList<T>> realGetter, Func<T, Guid> keyPropertyGetter = null)
        {
            CacheSetter cacheSetter = (k, o) => Cache.SetLocalCache(k, o);
            if (keyPropertyGetter == null)
            {
                keyPropertyGetter = obj => (Guid)Invoker.PropertyGet(obj, "Id");
            }
            return BulkGetWithCache<T, Guid>(idList, realGetter, keyPropertyGetter, GetFromLocalCache<T>, cacheSetter, id => "Get_" + id);
        }

        private static object s_SyncSegmentObj = new object();
        private static bool s_HasSegmentInit = false;
        private static string[] s_Dicts = new string[] 
        {
            "1段", "2段", "3段", "4段", "5段", "6段", "1+段", "2+段", "12+段", "pre段"
        };

        public static IList<string> SegmentToWord(string text, bool ignoreCase)
        {
            if (s_HasSegmentInit == false)
            {
                lock (s_SyncSegmentObj)
                {
                    if (s_HasSegmentInit == false)
                    {
                        string configPath = ConfigurationManager.AppSettings["PanGu_ConfigFilePath"];
                        if (string.IsNullOrWhiteSpace(configPath) == false)
                        {
                            string p = Path.GetPathRoot(configPath);
                            if (p == null || p.Trim().Length <= 0) // 说明是相对路径
                            {
                                configPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, configPath);
                            }
                        }
                        else
                        {
                            configPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "pangu.xml");
                        }
                        PanGu.Segment.Init(configPath);
                        s_HasSegmentInit = true;
                    }
                }
            }
            List<string> dicts = new List<string>(s_Dicts.Length);
            foreach (var w in s_Dicts)
            {
                if (text.IndexOf(w, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    dicts.Add(w);
                    text = text.Replace(w, string.Empty);
                }
            }
            PanGu.Segment segment = new PanGu.Segment();
            var list = segment.DoSegment(text);
            List<string> rstList = new List<string>(list.Count + dicts.Count);
            rstList.AddRange(dicts);
            foreach (var w in list)
            {
                if (rstList.Contains(w.Word, ignoreCase ? StringComparer.CurrentCultureIgnoreCase : StringComparer.CurrentCulture) == false)
                {
                    rstList.Add(w.Word);
                }
            }
            return rstList;
        }

        public static string GetSubstring(this string input,int len)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            return input.Length < len ? input : input.Substring(0, len);
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受     
        }

        public static string HttpPost(string url, string postDataString, Encoding encoding, string contentType = null, string userAgent = null, string host = null, string referer = null, IDictionary<string, string> customHeaders = null)
        {
            bool isHttps = url.Trim().StartsWith("https:", StringComparison.InvariantCultureIgnoreCase);
            if (isHttps)
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            }
            byte[] postData = encoding.GetBytes(postDataString);
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
            if (isHttps)
            {
                myRequest.ProtocolVersion = HttpVersion.Version10;
            }
            myRequest.Method = "POST";
            myRequest.ContentLength = postData.Length;
            if (string.IsNullOrWhiteSpace(contentType) == false)
            {
                myRequest.ContentType = contentType;
            }
            if (string.IsNullOrWhiteSpace(userAgent) == false)
            {
                myRequest.UserAgent = userAgent;
            }
            if (string.IsNullOrWhiteSpace(host) == false)
            {
                myRequest.Host = host;
            }
            if (string.IsNullOrWhiteSpace(referer) == false)
            {
                myRequest.Referer = referer;
            }
            myRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
            if (customHeaders != null && customHeaders.Count > 0)
            {
                foreach (var entry in customHeaders)
                {
                    if (myRequest.Headers.AllKeys.Contains(entry.Key))
                    {
                        myRequest.Headers[entry.Key] = entry.Value;
                    }
                    else
                    {
                        myRequest.Headers.Add(entry.Key, entry.Value);
                    }
                }
            }
            using (Stream newStream = myRequest.GetRequestStream())
            {
                newStream.Write(postData, 0, postData.Length); //设置POST
                newStream.Close();
            }
            // 获取结果数据
            try
            {
                using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(myResponse.GetResponseStream(), encoding))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    string responseTxt;
                    using (StreamReader readStream = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                    {
                        responseTxt = readStream.ReadToEnd();
                    }
                    ex.Response.Close();
                    if (string.IsNullOrWhiteSpace(responseTxt) == false)
                    {
                        throw new ApplicationException("Http请求发生错误，对方服务器返回：" + responseTxt, ex);
                    }
                }
                throw;
            }
        }

        public static string HttpGet(string url, Encoding encoding, string contentType = null, string userAgent = null, string host = null, string referer = null, IDictionary<string, string> customHeaders = null)
        {
            bool isHttps = url.Trim().StartsWith("https:", StringComparison.InvariantCultureIgnoreCase);
            if (isHttps)
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            }
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
            if (isHttps)
            {
                myRequest.ProtocolVersion = HttpVersion.Version10;
            }
            myRequest.Method = "GET";
            if (string.IsNullOrWhiteSpace(contentType) == false)
            {
                myRequest.ContentType = contentType;
            }
            if (string.IsNullOrWhiteSpace(userAgent) == false)
            {
                myRequest.UserAgent = userAgent;
            }
            if (string.IsNullOrWhiteSpace(host) == false)
            {
                myRequest.Host = host;
            }
            if (string.IsNullOrWhiteSpace(referer) == false)
            {
                myRequest.Referer = referer;
            }
            myRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
            if (customHeaders != null && customHeaders.Count > 0)
            {
                foreach (var entry in customHeaders)
                {
                    if (myRequest.Headers.AllKeys.Contains(entry.Key))
                    {
                        myRequest.Headers[entry.Key] = entry.Value;
                    }
                    else
                    {
                        myRequest.Headers.Add(entry.Key, entry.Value);
                    }
                }
            }
            // 获取结果数据
            try
            {
                using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(myResponse.GetResponseStream(), encoding))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    string responseTxt;
                    using (StreamReader readStream = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                    {
                        responseTxt = readStream.ReadToEnd();
                    }
                    ex.Response.Close();
                    if (string.IsNullOrWhiteSpace(responseTxt) == false)
                    {
                        throw new ApplicationException("Http请求发生错误，对方服务器返回：" + responseTxt, ex);
                    }
                }
                throw;
            }
        }

        public static string MD5Encrypt(string input)
        {
            return MD5Encrypt(input, Encoding.UTF8);
        }

        public static string MD5Encrypt(string input, Encoding encoding)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] t = md5.ComputeHash(encoding.GetBytes(input));
            StringBuilder sb = new StringBuilder(32);
            for (int i = 0; i < t.Length; i++)
            {
                sb.Append(t[i].ToString("x").PadLeft(2, '0'));
            }
            return sb.ToString();
        }
    }

    public delegate T CacheGetter<T>(string cacheKey, out bool existedInCache);

    public delegate void CacheSetter(string cacheKey, object data);
}
