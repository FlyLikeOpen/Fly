using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Configuration;
using System.Web;
using System.Runtime.Caching;

namespace Fly.Framework.Common
{
    internal static class CookieConfig
    {
        private const string COOKIE_CONFIG_FILE_PATH = "Configuration/Cookie.config";
        private const string COOKIE_CONFIG_FILE_PATH_NODE_NAME = "CookieConfigPath";

        private static string CookieConfigFilePath
        {
            get
            {
                string path = ConfigurationManager.AppSettings[COOKIE_CONFIG_FILE_PATH_NODE_NAME];
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = COOKIE_CONFIG_FILE_PATH;
                }
                string p = Path.GetPathRoot(path);
                if (p == null || p.Trim().Length <= 0) // relative path
                {
                    return Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, path);
                }
                return path;
            }
        }

        private static T GetWithLocalCache<T>(string cacheKey, Func<T> getter, params string[] filePathList)
            where T : class
        {
            object t = MemoryCache.Default.Get(cacheKey);
            if (t is DBNull)
            {
                return null;
            }
            T rst = t as T;
            if (rst != null)
            {
                return rst;
            }
            string locker = "Cookie_Cache_" + cacheKey;
            lock (locker)
            {
                rst = MemoryCache.Default.Get(cacheKey) as T;
                if (rst != null)
                {
                    return rst;
                }
                rst = getter();
                List<string> list = new List<string>(filePathList.Length);
                foreach (var file in filePathList)
                {
                    if (File.Exists(file))
                    {
                        list.Add(file);
                    }
                }
                if (list.Count > 0)
                {
                    CacheItemPolicy cp = new CacheItemPolicy();
                    cp.ChangeMonitors.Add(new HostFileChangeMonitor(list));
                    MemoryCache.Default.Set(cacheKey, (rst == null ? (object)DBNull.Value : (object)rst), cp);
                }
                return rst;
            }            
        }

        private static Dictionary<string, CookieConfigEntity> GetAllCookieConfig()
        {
            string path = CookieConfigFilePath;
            if (string.IsNullOrWhiteSpace(path) || File.Exists(path) == false)
            {
                return new Dictionary<string, CookieConfigEntity>(0);
            }
            return GetWithLocalCache<Dictionary<string, CookieConfigEntity>>("WEB_CookieConfig_GetCookieConfig", () =>
            {
                Dictionary<string, CookieConfigEntity> dic = new Dictionary<string, CookieConfigEntity>();
                XmlDocument doc = new XmlDocument();
                doc.Load(CookieConfigFilePath);
                XmlNodeList nodeList = doc.GetElementsByTagName("cookies");
                if (nodeList != null && nodeList.Count > 0)
                {
                    foreach (XmlNode xmlNode in nodeList)
                    {
                        if (xmlNode == null)
                        {
                            continue;
                        }
                        CookieConfigEntity entity = new CookieConfigEntity();
                        entity.NodeName = xmlNode.Attributes["nodeName"] != null ? xmlNode.Attributes["nodeName"].Value : null;
                        entity.PersistType = xmlNode.Attributes["persistType"] != null ? xmlNode.Attributes["persistType"].Value : null;
                        entity.SecurityLevel = xmlNode.Attributes["securityLevel"] != null ? xmlNode.Attributes["securityLevel"].Value : null;
                        if (string.IsNullOrWhiteSpace(entity.NodeName))
                        {
                            throw new ApplicationException("Not set node name for cookie config in file '" + path + "'");
                        }
                        if (dic.ContainsKey(entity.NodeName))
                        {
                            throw new ApplicationException("Duplicated cookie config of node '" + entity.NodeName + "' in file '" + path + "'");
                        }
                        if (string.IsNullOrWhiteSpace(entity.PersistType))
                        {
                            entity.PersistType = "Auto";
                        }
                        if (string.IsNullOrWhiteSpace(entity.SecurityLevel))
                        {
                            entity.SecurityLevel = "Low";
                        }
                        foreach (XmlNode childNode in xmlNode.ChildNodes)
                        {
                            if (childNode.NodeType == XmlNodeType.Element)
                            {
                                if (entity.Properties.ContainsKey(childNode.Name))
                                {
                                    entity.Properties[childNode.Name] = childNode.InnerText;
                                }
                                else
                                {
                                    entity.Properties.Add(childNode.Name, childNode.InnerText);
                                }
                            }
                        }
                        dic.Add(entity.NodeName, entity);
                    }
                }
                return dic;
            }, path);
        }

        private static CookieConfigEntity GetDefaultCookieConfig(string nodeName)
        {
            CookieConfigEntity defaultConfig = new CookieConfigEntity();
            defaultConfig.NodeName = "defaultConfig";
            defaultConfig.PersistType = "Auto";
            defaultConfig.SecurityLevel = "Low";
            defaultConfig.Properties["cookieName"] = nodeName;
            defaultConfig.Properties["hashkey"] = "di234fdgb-d3234kx-s534345-dfle-sdfiksdf";
            defaultConfig.Properties["rc4key"] = "sdddf-09xk-dsddd-sssdf9sdf2d-09dsfks";
            defaultConfig.Properties["domain"] = ((HttpContext.Current == null || HttpContext.Current.Request == null) ? "localhost" : HttpContext.Current.Request.Url.Host);
            defaultConfig.Properties["path"] = "/";
            defaultConfig.Properties["expires"] = "0";
            defaultConfig.Properties["securityExpires"] = "20";
            return defaultConfig;
        }

        public static CookieConfigEntity GetCookieConfig(string nodeName)
        {
            Dictionary<string, CookieConfigEntity> all = GetAllCookieConfig();
            CookieConfigEntity entity;
            if (all.TryGetValue(nodeName, out entity) && entity != null)
            {
                return entity;
            }
            return GetDefaultCookieConfig(nodeName);
        }
    }

    internal class CookieConfigEntity
    {
        public string NodeName { get; set; }
        public string PersistType { get; set; }
        public string SecurityLevel { get; set; }

        private Dictionary<string, string> m_Properties = new Dictionary<string, string>();
        public Dictionary<string, string> Properties { get { return m_Properties; } }
    }
}
