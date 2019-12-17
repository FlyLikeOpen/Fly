using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    public static class EnumHelper
    {
        private static Dictionary<Type, EnumDesc> s_Dictionary = new Dictionary<Type, EnumDesc>();
        private static object s_SyncObj = new object();

        public static bool IsGenericEnum(Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && type.GetGenericArguments() != null
                    && type.GetGenericArguments().Length == 1 && type.GetGenericArguments()[0].IsEnum);
        }

        public static string ToDescription(this Enum value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            Type t = value.GetType();
            EnumDesc desc;
            if (s_Dictionary.TryGetValue(t, out desc) == false)
            {
                lock (s_SyncObj)
                {
                    if (s_Dictionary.TryGetValue(t, out desc) == false)
                    {
                        desc = new EnumDesc(t);
                        s_Dictionary.Add(t, desc);
                    }
                }
            }
            return desc.GetDesc((int)(object)value);
        }

        private class EnumDesc
        {
            private Dictionary<int, string> m_Description;

            public EnumDesc(Type type)
            {
                m_Description = new Dictionary<int, string>();
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                foreach (FieldInfo fi in fields)
                {
                    object enumValue = Enum.ToObject(type, fi.GetValue(type));
                    object[] attrs = fi.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    if (attrs != null && attrs.Length > 0 && attrs[0] is DescriptionAttribute)
                    {
                        DescriptionAttribute desc = (DescriptionAttribute)attrs[0];
                        Add((int)enumValue, desc.Description);
                    }
                    else
                    {
                        string name = Enum.GetName(type, enumValue);
                        Add((int)enumValue, name);
                    }
                }
            }

            private void Add(int enumValue, string desc)
            {
                if (desc == null)
                {
                    return;
                }
                if (m_Description.ContainsKey(enumValue))
                {
                    m_Description[enumValue] = desc;
                }
                else
                {
                    m_Description.Add(enumValue, desc);
                }
            }

            public string GetDesc(int enumValue)
            {
                string desc;
                if (m_Description.TryGetValue(enumValue, out desc))
                {
                    return desc;
                }
                return null;
            }
        }
    }
}
