using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Fly.Framework.Common
{
    public static class Api<T> where T : class
    {
        private static volatile T s_Instance = null;
        private static volatile object s_SyncObj = new object();

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    lock (s_SyncObj)
                    {
                        if (s_Instance == null)
                        {
                            Type t = typeof(T);
                            if (t.IsInterface == false)
                            {
                                throw new ApplicationException("Api<T>中的T只能使用接口类型");
                            }
                            string ns = t.Namespace.Replace(".APIs.", ".APIImpls.");
                            string name = t.Name.Substring(1);
                            string assmeblyName;
                            if (ns.Count(c => c == '.') > 1)
                            {
                                assmeblyName = string.Join(".", ns.Split('.'), 0, 2);
                            }
                            else
                            {
                                assmeblyName = ns;
                            }
                            string typeName = ns + "." + name + ", " + assmeblyName;
                            s_Instance = (T)Activator.CreateInstance(Type.GetType(typeName, true));
                        }
                    }
                }
                return s_Instance;
            }
        }

        public static void Register(string implClassTypeName)
        {
            //Register(typeof(T), implClassTypeName);
            Type t = Type.GetType(implClassTypeName, true);
            if (typeof(T).IsAssignableFrom(t) == false)
            {
                throw new ApplicationException("The type '" + t.AssemblyQualifiedName + "' is not implemented from type '" + typeof(T).AssemblyQualifiedName + "'.");
            }
            lock (s_SyncObj)
            {
                s_Instance = (T)Activator.CreateInstance(t);
            }
        }

        private static volatile Dictionary<string, T> s_Dicts = new Dictionary<string, T>(StringComparer.InvariantCultureIgnoreCase);
        private static volatile object s_SyncObj2 = new object();

        public static T GetInstance(string name, bool throwExceptionWhenNotFound = false)
        {
            T obj;
            if (s_Dicts.TryGetValue(name, out obj))
            {
                return obj;
            }
            if (throwExceptionWhenNotFound)
            {
                throw new ApplicationException("Not found the impl class type for name '" + name + "'.");
            }
            return null;
        }

        public static IEnumerable<T> GetAllInstance()
        {
            return s_Dicts.Values;
        }

        public static void Register(string name, string implClassTypeName)
        {
            Type t = Type.GetType(implClassTypeName, true);
            if (typeof(T).IsAssignableFrom(t) == false)
            {
                throw new ApplicationException("The type '" + t.AssemblyQualifiedName + "' is not implemented from type '" + typeof(T).AssemblyQualifiedName + "'.");
            }
            T obj = (T)Activator.CreateInstance(t);
            lock (s_SyncObj2)
            {
                if(s_Dicts.ContainsKey(name))
                {
                    s_Dicts[name] = obj;
                }
                else
                {
                    s_Dicts.Add(name, obj);
                }
            }
        }


        //private static void Register(string interfaceTypeName, string implClassTypeName)
        //{
        //    Register(Type.GetType(interfaceTypeName, true), implClassTypeName);
        //}

        //private static void Register(Type interfaceType, string implClassTypeName)
        //{
        //    Register(interfaceType, Type.GetType(implClassTypeName, true));
        //}

        //private static void Register(Type interfaceType, Type implClassType)
        //{
        //    if (interfaceType.IsAssignableFrom(implClassType) == false)
        //    {
        //        throw new ApplicationException("The type '" + implClassType.AssemblyQualifiedName + "' is not implemented from type '" + interfaceType.AssemblyQualifiedName + "'.");
        //    }
        //}
    }

    public class Api
    {
        private static Dictionary<Type, Api> s_Dic = new Dictionary<Type, Api>();
        private static object s_SyncObj = new object();

        public static Api RegisterInterface(Type interfaceType)
        {
            if (interfaceType.IsInterface == false)
            {
                throw new ApplicationException("类型\"" + interfaceType.FullName + "\"不为接口");
            }
            Api api;
            if(s_Dic.TryGetValue(interfaceType, out api))
            {
                return api;
            }
            lock(s_SyncObj)
            {
                if (s_Dic.TryGetValue(interfaceType, out api))
                {
                    return api;
                }
                api = new Api(interfaceType);
                s_Dic.Add(interfaceType, api);
            }
            return api;
        }

        public static Api RegisterInterface(string interfaceTypeName)
        {
            return RegisterInterface(Type.GetType(interfaceTypeName, true));
        }

        private MethodInfo m_MethodInfo;
        private Api(Type type)
        {
            m_MethodInfo = typeof(Api<>).MakeGenericType(type).GetMethod("Register", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);
        }

        public Api Register(string name, string implClassTypeName)
        {
            m_MethodInfo.Invoke(null, new object[] { name, implClassTypeName });
            return this;
        }
    }
}
