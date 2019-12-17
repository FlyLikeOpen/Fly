using Fly.Framework.Common;
using Fly.APIs.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace Fly.UI.Common
{
    public static class Menu
    {
        public static IList<MenuItem> GetMenus()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Configuration/Menu.config").ToLower();
            return Cache.GetWithLocalCache("Menu.GetMenus", () =>
            {
                return LoadMenuData(filePath);
            }, filePath);
        }

        private static IList<MenuItem> LoadMenuData(string filePath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            return BuildChildren(doc.SelectSingleNode("/configuration"));
        }

        private static List<MenuItem> BuildChildren(XmlNode node)
        {
            var list = GetChildrenNodes(node, "item");
            if(list == null || list.Length <= 0)
            {
                return new List<MenuItem>(0);
            }
            List<MenuItem> rst = new List<MenuItem>(list.Length);
            foreach (XmlNode item in list)
            {
                MenuItem menu = new MenuItem();
                menu.Children = BuildChildren(item);
                menu.Title = GetNodeAttribute(item, "title");
                menu.Icon = GetNodeAttribute(item, "icon");
                if (menu.Children.FindAll(x => x.Show).Count <= 0)
                {
                    menu.Url = GetNodeAttribute(item, "url").ToLower();
                    menu.AuthKey = GetNodeAttribute(item, "authKey");
                    string show = GetNodeAttribute(item, "show");
                    menu.Show = (string.IsNullOrWhiteSpace(show) || string.Equals(show, "true", StringComparison.InvariantCultureIgnoreCase));
                }
                else
                {
                    menu.Show = true;
                }
                rst.Add(menu);
            }
            return rst;
        }

        private static XmlNode[] GetChildrenNodes(XmlNode node, string nodeName)
        {
            if (node == null || node.ChildNodes == null || node.ChildNodes.Count <= 0)
            {
                return new XmlNode[0];
            }
            List<XmlNode> nodeList = new List<XmlNode>(node.ChildNodes.Count);
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == nodeName)
                {
                    nodeList.Add(child);
                }
            }
            return nodeList.ToArray();
        }

        private static string GetNodeAttribute(XmlNode node, string attributeName)
        {
            if (node.Attributes == null
                        || node.Attributes[attributeName] == null
                        || node.Attributes[attributeName].Value == null
                        || node.Attributes[attributeName].Value.Trim() == string.Empty)
            {
                return string.Empty;
            }
            return node.Attributes[attributeName].Value.Trim();
        }

        public static bool IsCurrentMenu(MenuItem menu, bool checkChildren)
        {
            if (HttpContext.Current == null || HttpContext.Current.Request == null)
            {
                return false;
            }
            string currentUrl = HttpContext.Current.Request.Url.AbsolutePath;
            if (menu.HasChildren == false)
            {
                if (string.IsNullOrWhiteSpace(menu.Url))
                {
                    return false;
                }
                return currentUrl.Equals(menu.Url, StringComparison.InvariantCultureIgnoreCase);
            }
			else
			{
				bool curr = currentUrl.Equals(menu.Url, StringComparison.InvariantCultureIgnoreCase);
				if (curr)
					return true;
			}
            if (checkChildren)
            {
                foreach (var item in menu.Children)
                {
                    if (IsCurrentMenu(item, checkChildren))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HasPermission(MenuItem menu)
        {
            if (ContextManager.Current.UserId == Guid.Empty)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(menu.AuthKey) == false
                && Api<IStaffUserApi>.Instance.HasPermission(ContextManager.Current.UserId, menu.AuthKey) == false)
            {
                return false;
            }
            var subList = menu.Children.FindAll(x => x.Show);
            if (subList.Count <= 0)
            {
                return true;
            }
            foreach (var item in subList)
            {
                if (HasPermission(item))
                {
                    return true;
                }
            }
            return false;
        }
        
        public static IList<MenuItem> GetBreadcrumb()
        {
            if (HttpContext.Current == null)
            {
                return null;
            }
            Func<MenuItem, List<MenuItem>, bool> func = null;
            func = (menuData, list) =>
            {
                bool isOK = false;
                var subList = menuData.Children;
                var subListCount = menuData.Children.Count;
                if (subList != null && subListCount > 0)
                {
                    foreach (var m in subList)
                    {
                        if (func(m, list))
                        {
                            isOK = true;
                            break;
                        }
                    }
                }
                if (isOK == false)
                {
                    if (string.IsNullOrWhiteSpace(menuData.Url) == false)
                    {
                        string pageUrl = HttpContext.Current.Request.Url.AbsolutePath;
                        pageUrl = pageUrl.Trim().TrimEnd(new char[] { '/', '\\' }) + "/";
                        string u = menuData.Url.Trim().TrimEnd(new char[] { '/', '\\' }) + "/";
                        isOK = string.Compare(u, pageUrl, true) == 0;
                    }
                }
                if (isOK)
                {
                    list.Add(menuData);
                }
                return isOK;
            };
            List<MenuItem> rst = new List<MenuItem>();
            var topList = GetMenus();
            foreach (var m in topList)
            {
                if (func(m, rst))
                {
                    rst.Reverse();
                    break;
                }
            }
            return rst;
        }
    }

    public class MenuItem
    {
        public string Title { get; set; }

        public string Icon { get; set; }

        public string Url { get; set; }

        public bool Show { get; set; }

        public string AuthKey { get; set; }

        public List<MenuItem> Children { get; set; }

        public bool HasChildren
        {
            get
            {
                return Children != null && Children.Count > 0;
            }
        }
    }
}