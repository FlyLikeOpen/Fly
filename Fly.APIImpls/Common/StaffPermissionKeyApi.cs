using Fly.Framework.Common;
using Fly.APIs.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Fly.APIImpls.Common
{
    public class StaffPermissionKeyApi : IStaffPermissionKeyApi
    {
        private PermissionKey ParsePermissionNode(XmlNode node)
        {
            string text = string.Empty;
            string value = string.Empty;
            if (node.Attributes != null && node.Attributes.Count > 0)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    if (string.Equals(attr.Name, "Text", StringComparison.OrdinalIgnoreCase))
                    {
                        text = attr.Value;
                    }
                    if (string.Equals(attr.Name, "Value", StringComparison.OrdinalIgnoreCase))
                    {
                        value = attr.Value;
                    }
                }
            }
            List<PermissionKey> ps = null;
            if (node.ChildNodes != null && node.ChildNodes.Count > 0)
            {
                ps = new List<PermissionKey>(node.ChildNodes.Count);
                foreach (XmlNode nn in node.ChildNodes)
                {
                    if (nn.NodeType == XmlNodeType.Element && node.Name == "Permission")
                    {
                        var node1 = ParsePermissionNode(nn);
                        if (string.IsNullOrWhiteSpace(node1.Value) == false
                            || (node1.PermissionKeys != null && node1.PermissionKeys.Count > 0))
                        {
                            ps.Add(node1);
                        }
                    }
                }
            }
            PermissionKey p = new PermissionKey
            {
                Text = text,
                Value = value,
                PermissionKeys = ps == null ? new List<PermissionKey>(0) : ps
            };
            return p;
        }

        public IList<PermissionKey> GetAllPermissionKeys()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Configuration/permissions.config");
            if (File.Exists(filePath) == false)
            {
                throw new ApplicationException(string.Format("权限配置文件'{0}'不存在", filePath));
            }
            return Cache.GetWithLocalCache<List<PermissionKey>>("StaffPermissionKeyApi.GetAllPermissions", () =>
            {
                List<PermissionKey> list = null;
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);
                var root = doc.SelectNodes("//Permissions");
                if (root == null || root.Count <= 0 || root[0] == null)
                {
                    throw new ApplicationException("Configuration/permissions.config的配置有误，缺少根节点：Permissions（要区分大小写）");
                }
                XmlNodeList nodes = root[0].ChildNodes;
                if (nodes != null && nodes.Count > 0)
                {
                    list = new List<PermissionKey>(nodes.Count);
                    foreach (XmlNode node in nodes)
                    {
                        if (node.NodeType == XmlNodeType.Element && node.Name == "Permission")
                        {
                            var node1 = ParsePermissionNode(node);
                            if (string.IsNullOrWhiteSpace(node1.Value) == false
                                || (node1.PermissionKeys != null && node1.PermissionKeys.Count > 0))
                            {
                                list.Add(node1);
                            }
                        }
                    }
                }
                return (list == null ? new List<PermissionKey>(0) : list);
            }, filePath);
        }
    }
}
