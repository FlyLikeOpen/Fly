using System;
using System.Text;
using System.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Fly.Framework.SqlDbAccess
{
    internal static class ConnectionStringManager
    {
        public const string DEFAULT_CONN_KEY = "Global";
        private static Dictionary<string, ConnStrSetting> m_ConnStrMaps = new Dictionary<string, ConnStrSetting>();
        private static object s_syncObject = new object();

        public static string GetDefaultConnectionString()
        {
            return GetConnectionString(DEFAULT_CONN_KEY);
        }

        public static string GetConnectionString(string name)
        {
            if (m_ConnStrMaps.ContainsKey(name))
            {
                return m_ConnStrMaps[name].ConnectionString;
            }
            if (ConfigurationManager.ConnectionStrings[name] != null)
            {                
                string connStr = ConfigurationManager.ConnectionStrings[name].ConnectionString;
                ConnStrSetting tmp = new ConnStrSetting(name, connStr,
                    ConvertProviderNameToType(ConfigurationManager.ConnectionStrings[name].ProviderName));
                lock (s_syncObject)
                {
                    if (!m_ConnStrMaps.ContainsKey(name))
                    {
                        m_ConnStrMaps.Add(name, tmp);
                    }
                }
                return connStr;
            }
            throw new ApplicationException("Can't find the Connection String Key '" + name + "'. It hasn't been configurated.");
        }

        public static ProviderType GetDefaultProviderType()
        {
            return GetProviderType(DEFAULT_CONN_KEY);
        }

        public static ProviderType GetProviderType(string name)
        {
            if (m_ConnStrMaps.ContainsKey(name))
            {
                return m_ConnStrMaps[name].ProviderType;
            }
            if (ConfigurationManager.ConnectionStrings[name] != null)
            {
                ProviderType proType = ConvertProviderNameToType(ConfigurationManager.ConnectionStrings[name].ProviderName);
                ConnStrSetting tmp = new ConnStrSetting(name,
                    ConfigurationManager.ConnectionStrings[name].ConnectionString, proType);
                lock (s_syncObject)
                {
                    if (!m_ConnStrMaps.ContainsKey(name))
                    {
                        m_ConnStrMaps.Add(name, tmp);
                    }
                }
                return proType;
            }
            throw new ApplicationException("Can't find the Connection String Key '" + name + "'. It hasn't been configurated.");
        }

        public static void SetConnectionString(string name, string connStr, ProviderType providerType)
        {
            ConnStrSetting tmp = new ConnStrSetting(name, connStr, providerType);
            if (m_ConnStrMaps.ContainsKey(name))
            {
                m_ConnStrMaps[name] = tmp;
            }
            else
            {
                m_ConnStrMaps.Add(name, tmp);
            }
        }

        public static void SetSqlServerConnectionString(string name, string connStr)
        {
            SetConnectionString(name, connStr, ProviderType.SqlServer);
        }

        public static void SetDefaultConnectionString(string connStr, ProviderType providerType)
        {
            SetConnectionString(DEFAULT_CONN_KEY, connStr, providerType);
        }

        private static ProviderType ConvertProviderNameToType(string providerName)
        {
            string name = providerName.Trim().ToLower();
            switch (name)
            {
                case "sqlclient":
                case "sqlserver":
                case "system.data.sqlclient":
                    return ProviderType.SqlServer;
                default:
                    throw new System.Configuration.ConfigurationErrorsException("Not support this database provider '" + providerName + "'.");
            }
        }

        private class ConnStrSetting
        {
            private string m_Name;
            private string m_ConnectionString;
            private ProviderType m_ProviderType;

            public ConnStrSetting(string name, string connStr, ProviderType proType)
            {
                m_Name = name;
                m_ConnectionString = connStr;
                m_ProviderType = proType;
            }

            public string Name
            {
                get
                {
                    return m_Name;
                }
            }

            public string ConnectionString
            {
                get
                {
                    return m_ConnectionString;
                }
            }

            public ProviderType ProviderType
            {
                get
                {
                    return m_ProviderType;
                }
            }
        }
    }
}
