using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Xml;

namespace Fly.Framework.Common
{
    public static class FilterConfig
    {
        private const string COOKIE_CONFIG_FILE_PATH = "Configuration/MvcFilter.config";
        private const string COOKIE_CONFIG_FILE_PATH_NODE_NAME = "MvcFilterConfigPath";
        private static string FilterConfigPath
        {
            get
            {
                string path = ConfigurationManager.AppSettings[COOKIE_CONFIG_FILE_PATH_NODE_NAME];
                if (string.IsNullOrWhiteSpace(path))
                {
                    path = COOKIE_CONFIG_FILE_PATH;
                }
                string p = Path.GetPathRoot(path);
                if (string.IsNullOrWhiteSpace(p)) // relative path
                {
                    return Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, path);
                }
                return path;
            }
        }

        public static void RegisterGlobalFilters()
        {
            RegisterGlobalFilters(GlobalFilters.Filters);
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            if (File.Exists(FilterConfigPath) == false)
            {
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(FilterConfigPath);
            XmlNode root = doc.DocumentElement;
            XmlNodeList filterList = root.SelectNodes("filter");
            if (filterList != null && filterList.Count > 0)
            {
                int i = -1;
                foreach (XmlNode node in filterList)
                {
                    string type = GetAttributeValue(node, "type");
                    if (string.IsNullOrWhiteSpace(type) == false)
                    {
                        Type t = Type.GetType(type, true);
                        filters.Add(Activator.CreateInstance(t), i);
                        i++;
                    }
                }
            }
        }

        private static string GetAttributeValue(XmlNode node, string attrName)
        {
            XmlAttribute pathAtt = node.Attributes[attrName];
            if (pathAtt == null || string.IsNullOrWhiteSpace(pathAtt.Value))
            {
                return string.Empty;
            }
            else
            {
                return pathAtt.Value.Trim();
            }
        }
    }
}
