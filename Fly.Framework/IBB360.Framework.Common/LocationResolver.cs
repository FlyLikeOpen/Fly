using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Fly.Framework.Common
{
    public static class LocationResolver
    {
        #region Private Member

        private static string[] s_Provinces = new string[]{
            "河北", "山西", "辽宁", "吉林", "黑龙江", "江苏", "浙江 ",
            "安徽", "福建", "江西", "山东", "河南", "湖北", "湖南", "广东",
            "海南", "四川", "贵州", "云南", "陕西", "甘肃", "青海", "台湾",
            "内蒙古", "广西", "西藏", "宁夏", "新疆", "香港", "澳门"
        };
        private static Regex s_DuappRegex = new Regex("<div class=\"data\">([^\\d\\.]+?)</div>", RegexOptions.Compiled);
        private static Regex s_IP138Regex = new Regex("<td align=\"center\"><ul class=\"ul1\"><li>本站数据：(.+?)</li>", RegexOptions.Compiled);
        private static Regex s_ChinazRegex = new Regex(" ==>> (.+?)</strong><br />", RegexOptions.Compiled);
        private static Regex s_IPcnRegex = new Regex("</code>&nbsp;来自：(.+?)</p><p>GeoIP:", RegexOptions.Compiled);
        private static Regex s_882667Regex = new Regex("</p><p>地 区：<span class=\"shenlansezi\" >(.+?)</span></p><p>International IP query：<span class=\"lansezi\" >", RegexOptions.Compiled);
        private static Regex s_ChaxunchinaRegex = new Regex("\\[结果2\\](.+?)。查询中国提供全世界范围IP地址查询，能够准确提供IP所在地理位置信息。\" />", RegexOptions.Compiled);
        private static Regex s_T086Regex = new Regex("</a><br/>所在区域：<b class=\"f1\">(.+?)</b>", RegexOptions.Compiled);
        private static Regex s_114laRegex = new Regex("<td>([^\\d\\.]+?)</td>", RegexOptions.Compiled);
        private static Regex s_580sanRegex = new Regex("</strong>的地址是：<strong style='color:#ff0000'>(.+?)</strong><br/>", RegexOptions.Compiled);
        private static int s_IPIndex = 0;
        private static Func<string, string>[] s_IPFuncArray = new Func<string, string>[]
        {
            GetCityByIPWith114la,
            //GetCityByIPWith580san,
            //GetCityByIPWith882667,
            GetCityByIPWith911cha,
            GetCityByIPWithBaidu,
            //GetCityByIPWithBaiduApiStore,
            //GetCityByIPWithChaxunchina,
            //GetCityByIPWithChinaz,
            //GetCityByIPWithDuapp,
            //GetCityByIPWithGpsso,
            //GetCityByIPWithIP138,
            //GetCityByIPWithIPcn,
            GetCityByIPWithPCOnline,
            GetCityByIPWithQQMap,
            //GetCityByIPWithSina,
            GetCityByIPWithT086,
            GetCityByIPWithTaobao
        };

        private static Regex s_MobileIPcnRegex = new Regex("</code>&nbsp;所在城市: (.+?)<br />", RegexOptions.Compiled);
        private static int s_MobileIndex = 0;
        private static Func<string, string>[] s_MobileFuncArray = new Func<string, string>[]
        {
            //GetCityByMobileWithShowji,
            GetCityByMobileWithSogou,
            GetCityByMobileWithBaidu
            //GetCityByMobileWithIPcn
        };

        private static int s_LocationIndex = 0;
        private static int s_GeographyIndex = 0;

        private const string ErrorFolderName = "LocationError";

        #endregion

        private static int s_QQMapKeysIndex = 0;
        private static string[] s_QQMapKeys = new string[] 
        {
            "VQSBZ-VDSHO-PGGWV-SXPZH-6GL6K-RMFCD",
            "UFKBZ-GUSKW-YFURN-RNUDI-2VHWH-LCBHF",
            "EOFBZ-O2GWX-VKU4N-ZHXJ7-DTWHO-PUFXS",
            "WVIBZ-WVAKD-IZW4A-H5JOO-5LALF-5ZB5G",
            "IVOBZ-XI7CX-YFL4P-ZRMXA-C7NHT-NCBPZ",
            "M3BBZ-ISHCS-FA6OJ-6RCWC-6RPOS-WXBNU",
            "7HEBZ-ZAFKQ-5JV5H-GZPSE-ZHQSE-KHFRH",
            "APMBZ-J22WO-FWOWM-SMTAT-P3H45-ZUBPZ",
            "HODBZ-PPVKS-AALO5-6XJ5P-ILROE-HGFTY",
            "F4WBZ-DTSW3-GBP35-YZVJV-JE3AZ-ZZF75",
            "FVQBZ-MFD3X-WKB4N-ZZVJB-FNJH2-3WBVN",
            "7YRBZ-H6QKG-X6AQL-ICL2N-4LMFJ-6NBCX",
            "KVABZ-LCHCD-Z7X43-HWFFL-4JXLO-KBFPI",
            "WMNBZ-3KYKX-4KF4L-ZD4SL-6Y2HS-LHFD4",
            "4QXBZ-IWDKQ-HNR5Z-GYCX4-WGMS6-5NFQQ",
            "FXNBZ-G32WR-SQUW7-WS5NQ-PZDEV-ITBNG"
        };

        private static string GetAvaliableQQMapKey(string serviceName)
        {
            int tmp = Interlocked.Increment(ref s_QQMapKeysIndex);
            int start_index = tmp % s_QQMapKeys.Length;
            string tmpKey = s_QQMapKeys[start_index];
            if (Cache.GetLocalCache("qq_" + serviceName + tmpKey) == null)
            {
                return tmpKey;
            }

            int tmp_index = start_index + 1;
            while (s_QQMapKeys.Length > 0 && tmp_index != start_index)
            {
                if (tmp_index >= s_QQMapKeys.Length)
                {
                    tmp_index = 0;
                }
                tmpKey = s_QQMapKeys[tmp_index];
                if (Cache.GetLocalCache("qq_" + serviceName + tmpKey) == null)
                {
                    return tmpKey;
                }
                tmp_index++;
            }
            Logger.Write(ErrorFolderName, true, "[" + serviceName + "]QQMap所有的Key都已经达到当调用日次数限额了");
            return null;
        }

        private static void SetQQMapKeyLimited(string key, string serviceName)
        {
            // 缓存在第二天的零点零分零秒过期
            int s = (int)DateTime.Today.AddDays(1).Date.Subtract(DateTime.Now).TotalMinutes;
            if (s < 0)
            {
                s = 0 - s;
            }
            if (s == 0)
            {
                return;
            }
            Cache.SetLocalCache("qq_" + serviceName + key, "1", true, s);
        }

        #region Private GetCityByIP

        private static string GetCityByIPWithQQMap(string ip)
        {
            string json = string.Empty;
            try
            {
                string k = GetAvaliableQQMapKey("GetCityByIP");
                if (string.IsNullOrWhiteSpace(k))
                {
                    return null;
                }
                string url = string.Format("http://apis.map.qq.com/ws/location/v1/ip?ip={0}&key={1}&output=json", ip, k);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "apis.map.qq.com";
                req.Referer = "http://apis.map.qq.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != 0)
                {
                    if (rs.status == 121 || rs.message == "此key每日调用量已达到上限")
                    {
                        SetQQMapKeyLimited(k, "GetCityByIP");
                        return null;  // return GetCityByIPWithQQMap(ip);
                    }
                    else
                    {
                        Logger.Write(ErrorFolderName, true, string.Format("根据IP请求腾讯地图获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                        return null;
                    }
                }
                return rs.result.ad_info.city;
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求腾讯地图获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByIPWithTaobao(string ip)
        {
            string json = string.Empty;
            try
            {
                string url = string.Format("http://ip.taobao.com/service/getIpInfo.php?ip={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "ip.taobao.com";
                req.Referer = "http://ip.taobao.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.code != 0)
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据IP请求淘宝IP服务获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                    return null;
                }
                return rs.data.city;
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求淘宝IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByIPWithBaidu(string ip)
        {
            string json = string.Empty;
            try
            {
                string url = string.Format("https://sp0.baidu.com/8aQDcjqpAAV3otqbppnN2DJv/api.php?query={0}&resource_id=6006&format=json", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "sp0.baidu.com";
                req.Referer = "http://sp0.baidu.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("GBK")))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != "0")
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据IP请求百度IP服务获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                    return null;
                }
                string addr = rs.data[0].location;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1];
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求百度IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByIPWithBaiduApiStore(string ip)
        {
            string json = string.Empty;
            try
            {
                string url = string.Format("http://apis.baidu.com/apistore/iplookupservice/iplookup?ip={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Headers.Add("apikey", "2e8ab4cbdd02488e779c4d523d76569e");
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "apis.baidu.com";
                req.Referer = "http://apis.baidu.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.errNum != 0)
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据IP请求百度Api商店中IP服务获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                    return null;
                }
                return rs.retData.city;
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求百度Api商店中IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByIPWithSina(string ip)
        {
            string json = string.Empty;
            try
            {
                string url = string.Format("http://int.dpool.sina.com.cn/iplookup/iplookup.php?format=json&ip={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "int.dpool.sina.com.cn";
                req.Referer = "http://int.dpool.sina.com.cn/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Write(ErrorFolderName, true, "根据IP请求新浪IP服务获取城市信息时出错，返回数据为空");
                    return null;
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.ret != 1)
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据IP请求新浪IP服务获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                    return null;
                }
                return rs.city;
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求新浪IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByIPWithPCOnline(string ip)
        {
            string json = string.Empty;
            try
            {
                string url = string.Format("http://whois.pconline.com.cn/ipJson.jsp?ip={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "whois.pconline.com.cn";
                req.Referer = "http://whois.pconline.com.cn/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("GBK")))
                {
                    json = reader.ReadToEnd();
                }
                json = json.Replace("if(window.IPCallBack) {IPCallBack(", "").Replace(");}", "");
                dynamic rs = DynamicJson.Parse(json);
                return rs.city;
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求pconline的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByIPWithGpsso(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = "http://www.gpsso.com/WebService/IP/GetIP.asmx/GetCityByIp";
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "www.gpsso.com";
                req.Referer = "http://www.gpsso.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                byte[] aryBuf = Encoding.UTF8.GetBytes("IP=" + ip);
                req.ContentLength = aryBuf.Length;
                using (Stream writer = req.GetRequestStream())
                {
                    writer.Write(aryBuf, 0, aryBuf.Length);
                    writer.Close();
                }
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    xml = reader.ReadToEnd();
                }
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                string rst = doc.SelectSingleNode("API/RESULTS").InnerText;
                if (rst != "0")
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据IP请求gpsso.com的IP服务获取城市信息时状态码出错，返回信息：\r\n{0}", xml));
                    return null;
                }
                string addr = doc.SelectSingleNode("API/ADDRESS").InnerText;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1];
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求gpsso.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWithDuapp(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = "http://ipquery.duapp.com/index.php";
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "ipquery.duapp.com";
                req.Referer = "http://ipquery.duapp.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                byte[] aryBuf = Encoding.GetEncoding("GBK").GetBytes("InputIPBox=" + ip);
                req.ContentLength = aryBuf.Length;
                using (Stream writer = req.GetRequestStream())
                {
                    writer.Write(aryBuf, 0, aryBuf.Length);
                    writer.Close();
                }
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("GBK")))
                {
                    xml = reader.ReadToEnd();
                }

                var m = s_DuappRegex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1].Replace("内蒙古", "").Replace("新疆", "").Replace("广西", "").Replace("宁夏", "").Replace("西藏", "");
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求ipquery.duapp.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWithIP138(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("http://www.ip138.com/ips138.asp?ip={0}&action=2", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "www.ip138.com";
                req.Referer = "http://www.ip138.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("gb2312")))
                {
                    xml = reader.ReadToEnd();
                }
                var m = s_IP138Regex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1];
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求ip138.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWithChinaz(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("http://ip.chinaz.com/?IP={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "ip.chinaz.com";
                req.Referer = "http://ip.chinaz.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    xml = reader.ReadToEnd();
                }
                Regex reg = new Regex(" ==>> ([^\\d\\.]+?)</strong><br />", RegexOptions.Compiled);
                var m = reg.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1].Replace("内蒙古", "").Replace("新疆", "").Replace("广西", "").Replace("宁夏", "").Replace("西藏", "");
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求Chinaz.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWithIPcn(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("http://www.ip.cn/index.php?ip={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "www.ip.cn";
                req.Referer = "http://www.ip.cn/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    xml = reader.ReadToEnd();
                }
                var m = s_IPcnRegex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1];
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求ip.cn的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWith882667(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("http://www.882667.com/ip_{0}.html", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "www.882667.com";
                req.Referer = "http://www.882667.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("GBK")))
                {
                    xml = reader.ReadToEnd();
                }
                var m = s_882667Regex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1].Replace("内蒙古", "").Replace("新疆", "").Replace("广西", "").Replace("宁夏", "").Replace("西藏", "");
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求882667.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWithT086(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("http://ip.t086.com/?ip={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "ip.t086.com";
                req.Referer = "http://ip.t086.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("GBK")))
                {
                    xml = reader.ReadToEnd();
                }
                var m = s_T086Regex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1].Replace("内蒙古", "").Replace("新疆", "").Replace("广西", "").Replace("宁夏", "").Replace("西藏", "");
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求t086.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWithChaxunchina(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("http://ip.chaxunchina.com/?ip={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "ip.chaxunchina.com";
                req.Referer = "http://ip.chaxunchina.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    xml = reader.ReadToEnd();
                }
                var m = s_ChaxunchinaRegex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1].Replace("内蒙古", "").Replace("新疆", "").Replace("广西", "").Replace("宁夏", "").Replace("西藏", "");
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求Chaxunchina.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWith114la(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("http://tool.114la.com/index.php?ct=site&ac=ip&q={0}", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "tool.114la.com";
                req.Referer = "http://tool.114la.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    xml = reader.ReadToEnd();
                }
                var m = s_114laRegex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                var rst = s[s.Length - 1].Replace("内蒙古", "").Replace("新疆", "").Replace("广西", "").Replace("宁夏", "").Replace("西藏", "");
                if (rst.Contains("市"))
                {
                    string[] gos = rst.Split('市');
                    return gos[0] + "市";
                }
                return rst;
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求114la.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWith911cha(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("https://ip.911cha.com/{0}.html", ip);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "ip.911cha.com";
                req.Referer = "https://ip.911cha.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    xml = reader.ReadToEnd();
                }
                Regex reg = new Regex("<p>主站数据：" + ip + " → ([^\\d\\.]+?)</p>", RegexOptions.Compiled);
                var m = reg.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1].Replace("内蒙古", "").Replace("新疆", "").Replace("广西", "").Replace("宁夏", "").Replace("西藏", "");
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求911cha.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        private static string GetCityByIPWith580san(string ip)
        {
            string xml = string.Empty;
            try
            {
                string url = "http://www.580san.com/";
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "POST";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Host = "www.580san.com";
                req.Referer = "http://www.580san.com/";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
                req.ContentType = "application/x-www-form-urlencoded";
                byte[] aryBuf = Encoding.GetEncoding("GBK").GetBytes("searchip=" + ip);
                req.ContentLength = aryBuf.Length;
                using (Stream writer = req.GetRequestStream())
                {
                    writer.Write(aryBuf, 0, aryBuf.Length);
                    writer.Close();
                }
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    xml = reader.ReadToEnd();
                }
                var m = s_580sanRegex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string addr1 = ary[0];
                var s = addr1.Split(new string[] { "自治区", "省", "区划", "地区" }, StringSplitOptions.RemoveEmptyEntries);
                return s[s.Length - 1].Replace("内蒙古", "").Replace("新疆", "").Replace("广西", "").Replace("宁夏", "").Replace("西藏", "");
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据IP请求580san.com的IP服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        #endregion

        #region Private GetCityByMobile

        private static string GetCityByMobileWithShowji(string mobile)
        {
            string json = string.Empty;
            try
            {
                string url = string.Format("http://v.showji.com/Locating/showji.com20150416273007.aspx?m={0}&output=json", mobile);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                req.UserAgent="Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.85 Safari/537.36";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8");
                req.Headers.Add("Cache-Control", "max-age=0");
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.QueryResult != "True")
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据Mobile请求showji.com服务获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                    return null;
                }
                return rs.City + "市";
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据Mobile请求showji.com服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByMobileWithSogou(string mobile)
        {
            string json = string.Empty;
            try
            {
                string url = string.Format("http://www.sogou.com/websearch/phoneAddress.jsp?phoneNumber={0}&cb=handlenumber", mobile);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("GBK")))
                {
                    json = reader.ReadToEnd();
                }
                json = json.Replace("handlenumber(\"", "").Replace("\");", "");
                var ary = json.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string x = ary[0].Trim();
                if (x == "北京" || x == "上海" || x == "天津" || x == "重庆")
                {
                    return x + "市";
                }
                foreach (var p in s_Provinces)
                {
                    x = x.Replace(p, "");
                }
                return x + "市";
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据Mobile请求Sogou服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByMobileWithBaidu(string mobile)
        {
            string json = string.Empty;
            try
            {
                string url = string.Format("https://sp0.baidu.com/8aQDcjqpAAV3otqbppnN2DJv/api.php?query={0}&resource_id=6004&format=json", mobile);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("GBK")))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != "0")
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据Mobile请求百度服务获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                    return null;
                }
                return rs.data[0].city + "市";
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据Mobile请求百度服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static string GetCityByMobileWithIPcn(string mobile)
        {
            string xml = string.Empty;
            try
            {
                string url = string.Format("http://www.ip.cn/db.php?num={0}", mobile);
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    xml = reader.ReadToEnd();
                }
                var m = s_MobileIPcnRegex.Match(xml);
                string addr = m.Groups[1].Value;
                string[] ary = addr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string x = ary[0];
                if (x == "北京" || x == "上海" || x == "天津" || x == "重庆")
                {
                    return x + "市";
                }
                foreach (var p in s_Provinces)
                {
                    x = x.Replace(p, "");
                }
                return x + "市";
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据Mobile请求ip.cn服务获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", xml, ex));
                return null;
            }
        }

        #endregion

        #region Private Convert

        private static Geography ConvertToBaidu(decimal latitude, decimal longitude, GeographyType from)
        {
            if(from == GeographyType.BaiduMap)
            {
                return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
            }
            string fromStr = from == GeographyType.QQ_Google_AmapMap ? "3" : "1";
            string url = string.Format("http://api.map.baidu.com/geoconv/v1/?coords={0},{1}&from={2}&to=5&ak=QD0fvXTMRNBwt4jkAfhoKaEU", (decimal)longitude, (decimal)latitude, fromStr);
            string json = string.Empty;
            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != 0)
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据请求百度地图经纬度转换出错，返回信息：\r\n{0}", json));
                    return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
                }
                string x = rs.result[0].x + "";
                string y = rs.result[0].y + "";
                return new Geography { Latitude = Convert.ToSingle(x), Longitude = Convert.ToSingle(y) };
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据请求百度地图经纬度转换出错，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
            }
        }

        private static Geography ConvertToQQ(decimal latitude, decimal longitude, GeographyType from)
        {
            if (from == GeographyType.QQ_Google_AmapMap)
            {
                return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
            }
            string coord_type;
            if (from == GeographyType.GPS)
            {
                coord_type = "1";
            }
            else // baidu map
            {
                coord_type = "3";
            }
            string k = GetAvaliableQQMapKey("Translate");
            if (string.IsNullOrWhiteSpace(k))
            {
                return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
            }
            string url = string.Format("http://apis.map.qq.com/ws/coord/v1/translate/?locations={0},{1}&key={3}&type={2}", (decimal)latitude, (decimal)longitude, coord_type, k);
            string json = string.Empty;
            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != 0)
                {
                    if (rs.status == 121 || rs.message == "此key每日调用量已达到上限")
                    {
                        SetQQMapKeyLimited(k, "Translate");
                        //return ConvertToQQ(latitude, longitude, from);
                        return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
                    }
                    else
                    {
                        Logger.Write(ErrorFolderName, true, string.Format("根据请求腾讯地图经纬度转换出错，返回信息：\r\n{0}", json));
                        return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
                    }
                }
                string x = rs.locations[0].lat + "";
                string y = rs.locations[0].lng + "";
                return new Geography { Latitude = Convert.ToSingle(x), Longitude = Convert.ToSingle(y) };
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据请求腾讯地图经纬度转换出错，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
            }
        }

        #endregion

        #region Private GetAddressByLocation

        private static string GetAddressByLocationWithBaiduMap(decimal latitude, decimal longitude, GeographyType type,
            out string countryName, out string provinceName, out string cityName, out string districtName, out string streetName)
        {
            countryName = null;
            provinceName = null;
            cityName = null;
            districtName = null;
            streetName = null;
            string coord_type;
            decimal la = latitude;
            decimal lo = longitude;
            if (type == GeographyType.GPS)
            {
                coord_type = "wgs84ll";
            }
            else if (type == GeographyType.QQ_Google_AmapMap)
            {
                var t = ConvertToBaidu(latitude, longitude, GeographyType.QQ_Google_AmapMap);
                la = (decimal)t.Latitude;
                lo = (decimal)t.Longitude;
                coord_type = "bd09ll";
            }
            else // default, baidu
            {
                coord_type = "bd09ll";
            }
            string url = string.Format("http://api.map.baidu.com/geocoder/v2/?location={0},{1}&output=json&pois=1&coordtype={2}&ak=QD0fvXTMRNBwt4jkAfhoKaEU", (decimal)la, (decimal)lo, coord_type);
            string json = string.Empty;
            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != 0)
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据经纬度请求百度地图获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                    return null;
                }
                countryName = rs.result.addressComponent.country;
                provinceName = rs.result.addressComponent.province;
                cityName = rs.result.addressComponent.city;
                districtName = rs.result.addressComponent.district;
                streetName = rs.result.addressComponent.street;
                return rs.result.formatted_address;
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据经纬度请求百度地图获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }


        private static string GetAddressByLocationWithQQMap(decimal latitude, decimal longitude, GeographyType type,
            out string countryName, out string provinceName, out string cityName, out string districtName, out string streetName)
        {
            countryName = null;
            provinceName = null;
            cityName = null;
            districtName = null;
            streetName = null;
            string coord_type;
            if(type == GeographyType.GPS)
            {
                coord_type = "1";
            }
            else if (type == GeographyType.BaiduMap)
            {
                coord_type = "3";
            }
            else // default, qq\google\高德
            {
                coord_type = "5";
            }
            string k = GetAvaliableQQMapKey("GetAddressByLocation");
            if (string.IsNullOrWhiteSpace(k))
            {
                return null;
            }
            string url = string.Format("http://apis.map.qq.com/ws/geocoder/v1/?location={0},{1}&key={3}&get_poi=0&coord_type={2}", (decimal)latitude, (decimal)longitude, coord_type, k);
            string json = string.Empty;
            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != 0)
                {
                    if (rs.status == 121 || rs.message == "此key每日调用量已达到上限")
                    {
                        SetQQMapKeyLimited(k, "GetAddressByLocation");
                        //return GetAddressByLocationWithQQMap(latitude, longitude, type, out countryName, out provinceName, out cityName, out districtName, out streetName);
                        return null;
                    }
                    else
                    {
                        Logger.Write(ErrorFolderName, true, string.Format("根据经纬度请求腾讯地图获取城市信息时状态码出错，返回信息：\r\n{0}", json));
                        return null;
                    }
                }
                countryName = rs.result.address_component.nation;
                provinceName = rs.result.address_component.province;
                cityName = rs.result.address_component.city;
                districtName = rs.result.address_component.district;
                streetName = rs.result.address_component.street;
                return rs.result.address;
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据经纬度请求腾讯地图获取城市信息时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        #endregion

        #region Private GetLocationByAddress

        private static Geography GetLocationByAddressWithBaiduMap(string cityName, string address, GeographyType type)
        {
            string url = string.Format("http://api.map.baidu.com/geocoder/v2/?city={0}&address={1}&output=json&ak=QD0fvXTMRNBwt4jkAfhoKaEU", HttpUtility.UrlEncode(cityName), HttpUtility.UrlEncode(address));
            string json = string.Empty;
            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != 0)
                {
                    Logger.Write(ErrorFolderName, true, string.Format("根据地址请求百度地图获取经纬度时状态码出错，返回信息：\r\n{0}", json));
                    return null;
                }
                dynamic loc = rs.result.location;
                decimal latitude = Convert.ToDecimal(loc.lat + "");
                decimal longitude = Convert.ToDecimal(loc.lng + "");
                return ConvertLocation(latitude, longitude, GeographyType.BaiduMap, type);
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据地址请求百度地图获取经纬度时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        private static Geography GetLocationByAddressWithQQMap(string cityName, string address, GeographyType type)
        {
            string k = GetAvaliableQQMapKey("GetLocationByAddress");
            if (string.IsNullOrWhiteSpace(k))
            {
                return null;
            }
            string url = string.Format("http://apis.map.qq.com/ws/geocoder/v1/?region={0}&address={1}&key={2}", HttpUtility.UrlEncode(cityName), HttpUtility.UrlEncode(address), k);
            string json = string.Empty;
            try
            {
                HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
                req.Method = "GET";
                using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
                using (Stream stream = res.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    json = reader.ReadToEnd();
                }
                dynamic rs = DynamicJson.Parse(json);
                if (rs.status != 0)
                {
                    if (rs.status == 121 || rs.message == "此key每日调用量已达到上限")
                    {
                        SetQQMapKeyLimited(k, "GetLocationByAddress");
                        return null; //return GetLocationByAddressWithQQMap(cityName, address, type);
                    }
                    else
                    {
                        Logger.Write(ErrorFolderName, true, string.Format("根据地址请求腾讯地图获取经纬度时状态码出错，返回信息：\r\n{0}", json));
                        return null;
                    }
                }
                dynamic loc = rs.result.location;
                decimal latitude = Convert.ToDecimal(loc.lat + "");
                decimal longitude = Convert.ToDecimal(loc.lng + "");
                return ConvertLocation(latitude, longitude, GeographyType.QQ_Google_AmapMap, type);
            }
            catch (Exception ex)
            {
                Logger.Write(ErrorFolderName, true, string.Format("根据地址请求腾讯地图获取经纬度时出现异常，返回信息：\r\n{0}\r\n，错误信息：\r\n{1}", json, ex));
                return null;
            }
        }

        #endregion

        public static string GetCityNameByIP(string ip, bool resolveByAllServices = false)
        {
            int tmp = Interlocked.Increment(ref s_IPIndex);
            int index = tmp % s_IPFuncArray.Length;
            string city = s_IPFuncArray[index](ip);
            int tmp_index = index + 1;
            while (resolveByAllServices && string.IsNullOrWhiteSpace(city) && s_IPFuncArray.Length > 1 && tmp_index != index)
            {
                if (tmp_index >= s_IPFuncArray.Length)
                {
                    tmp_index = 0;
                }
                city = s_IPFuncArray[tmp_index](ip);
                tmp_index++;
            }
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }
            return city;
        }

        public static string GetCityNameByMobile(string mobile, bool resolveByAllServices = false)
        {
            int tmp = Interlocked.Increment(ref s_MobileIndex);
            int index = tmp % s_MobileFuncArray.Length;
            string city = s_MobileFuncArray[index](mobile);
            int tmp_index = index + 1;
            while (resolveByAllServices && string.IsNullOrWhiteSpace(city) && s_MobileFuncArray.Length > 1 && tmp_index != index)
            {
                if (tmp_index >= s_MobileFuncArray.Length)
                {
                    tmp_index = 0;
                }
                city = s_MobileFuncArray[tmp_index](mobile);
                tmp_index++;
            }
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }
            return city;
        }

        public static string GetAddressByLocation(decimal latitude, decimal longitude, GeographyType type,
            out string countryName, out string provinceName, out string cityName, out string districtName, out string streetName)
        {
            int tmp = Interlocked.Increment(ref s_LocationIndex);
            int index = tmp % 2;
            if (index == 0)
            {
                var rst = GetAddressByLocationWithBaiduMap(latitude, longitude, type, out countryName, out provinceName, out cityName, out districtName, out streetName);
                if (rst == null)
                {
                    return GetAddressByLocationWithQQMap(latitude, longitude, type, out countryName, out provinceName, out cityName, out districtName, out streetName);
                }
                return rst;
            }
            else
            {
                var rst = GetAddressByLocationWithQQMap(latitude, longitude, type, out countryName, out provinceName, out cityName, out districtName, out streetName);
                if (rst == null)
                {
                    return GetAddressByLocationWithBaiduMap(latitude, longitude, type, out countryName, out provinceName, out cityName, out districtName, out streetName);
                }
                return rst;
            }
        }

        public static Geography GetLocationByAddress(string cityName, string address, GeographyType type)
        {
            int tmp = Interlocked.Increment(ref s_GeographyIndex);
            int index = tmp % 2;
            if (index == 0)
            {
                var g = GetLocationByAddressWithBaiduMap(cityName, address, type);
                if (g == null)
                {
                    g = GetLocationByAddressWithQQMap(cityName, address, type);
                }
                return g;
            }
            else
            {
                var g = GetLocationByAddressWithQQMap(cityName, address, type);
                if (g == null)
                {
                    g = GetLocationByAddressWithBaiduMap(cityName, address, type);
                }
                return g;
            }
        }

        public static Geography GetBaiduMapLocationByAddress(string cityName, string address)
        {
            return GetLocationByAddressWithBaiduMap(cityName, address, GeographyType.BaiduMap);
        }

        public static Geography GetQQMapLocationByAddress(string cityName, string address)
        {
            return GetLocationByAddressWithQQMap(cityName, address, GeographyType.QQ_Google_AmapMap);
        }

        public static Geography ConvertLocation(decimal latitude, decimal longitude, GeographyType from, GeographyType to)
        {
            if (from == to)
            {
                return new Geography { Latitude = (float)latitude, Longitude = (float)longitude };
            }
            if (to == GeographyType.BaiduMap)
            {
                return ConvertToBaidu(latitude, longitude, from);
            }
            else if (to == GeographyType.QQ_Google_AmapMap)
            {
                return ConvertToQQ(latitude, longitude, from);
            }
            else // GeographyType.GPS
            {
                Geography g;
                if (from == GeographyType.BaiduMap)
                {
                    g = ConvertToBaidu(latitude, longitude, GeographyType.GPS); // 先把百度地图坐标当成GPS坐标到百度地图中进行转换
                }
                else // from == GeographyType.QQ_Google_AmapMap
                {
                    g = ConvertToQQ(latitude, longitude, GeographyType.GPS); // 先把腾讯地图坐标当成GPS坐标到腾讯地图中进行转换
                }
                // 进行偏移量的处理，得到近似的坐标
                decimal x = latitude * 2 - (decimal)g.Latitude;
                decimal y = longitude * 2 - (decimal)g.Longitude;
                return new Geography { Longitude = (float)y, Latitude = (float)x };
            }
        }
    }
}
