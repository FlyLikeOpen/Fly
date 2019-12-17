using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Caching;
using System.Net;
using System.IO;
using System.CodeDom;
using System.Web.Services.Description;
using System.Xml.Serialization;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace Fly.Framework.Common
{
    public static class SoapClient
    {
        public const string Namespace = "Fly.Framework.Common.ServiceClient.Soap";

        private static bool IsNullableType(this Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && type.GetGenericArguments() != null
                    && type.GetGenericArguments().Length == 1);
        }

        private static object[] GetArgs(object[] args, Assembly asy, MethodInfo me, out object[] args2)
        {
            if (args == null)
            {
                args2 = null;
                return null;
            }
            List<object> list = new List<object>(args.Length * 2);
            List<object> list2 = new List<object>(args.Length * 2);
            ParameterInfo[] pArray = me.GetParameters();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                {
                    object x = SoapEntityMapping.ConvertToSoapObject(args[i], asy);
                    list.Add(x);
                    list2.Add(x);
                    Type t = args[i].GetType();
                    if (t.IsValueType)
                    {
                        if (t.IsNullableType())
                        {
                            list.Add(args[i] != null);
                        }
                        else
                        {
                            list.Add(true);
                        }
                    }
                }
                else
                {
                    int len1 = list.Count;
                    int len2 = list2.Count;
                    if (pArray.Length <= len1 || pArray.Length <= len2)
                    {
                        throw new ApplicationException("WebService方法的入参数量不正确");
                    }
                    Type t = pArray[len1].ParameterType;
                    list.Add(DataConvertor.GetValueByType(t, null, null, null));
                    if (t.IsValueType)
                    {
                        list.Add(false);
                    }
                    t = pArray[len2].ParameterType;
                    list2.Add(DataConvertor.GetValueByType(t, null, null, null));
                }
            }
            args2 = list2.ToArray();
            return list.ToArray();
        }

        public static void Invoke(string url, string methodName, params object[] args)
        {
            Invoke<object>(url, methodName, args);
        }

        public static T Invoke<T>(string url, string methodName, params object[] args)
        {
            object proxy = CreateServiceProxyInstance(url);
            MethodInfo me = proxy.GetType().GetMethod(methodName);
            object[] realArgs2;
            object[] realArgs = GetArgs(args, proxy.GetType().Assembly, me, out realArgs2);
            if (typeof(T).IsValueType)
            {
                try
                {
                    T obj = default(T);
                    bool hasValue = false;
                    List<object> list = new List<object>(realArgs);
                    list.Add(obj);
                    list.Add(hasValue);
                    object[] x = list.ToArray();
                    me.Invoke(proxy, x);
                    hasValue = (bool)x[x.Length - 1];
                    if (hasValue)
                    {
                        object tmp = x[x.Length - 2];
                        return (T)SoapEntityMapping.ConvertFromSoapObject(tmp, typeof(T));
                    }
                    else
                    {
                        return default(T);
                    }
                }
                catch
                {
                    object tmp = Invoker.MethodInvoke(proxy, methodName, realArgs2);
                    return (T)SoapEntityMapping.ConvertFromSoapObject(tmp, typeof(T));
                }
            }
            else
            {
                object tmp;
                try
                {
                    tmp = Invoker.MethodInvoke(proxy, methodName, realArgs);
                }
                catch
                {
                    tmp = Invoker.MethodInvoke(proxy, methodName, realArgs2);
                }
                return (T)SoapEntityMapping.ConvertFromSoapObject(tmp, typeof(T));
            }
        }

        public static void InvokeAsync(string url, string methodName, Action callback, Action<Exception> errorHandler, params object[] args)
        {
            object proxy = CreateServiceProxyInstance(url);
            string beginMethodName = "Begin" + methodName;
            string endMethodName = "End" + methodName;
            MethodInfo m = proxy.GetType().GetMethod(beginMethodName);
            object[] realArgs2;
            object[] realArgs = GetArgs(args, proxy.GetType().Assembly, m, out realArgs2);
            AsyncCallback cb = ar =>
            {
                bool hasError = false;
                try
                {
                    Invoker.MethodInvoke(ar.AsyncState, endMethodName, ar);
                }
                catch (Exception ex)
                {
                    hasError = true;
                    errorHandler(ex);
                }
                if (hasError == false && callback != null)
                {
                    callback();
                }
            };
            try
            {
                List<object> list = new List<object>(realArgs);
                list.Add(cb);
                list.Add(proxy);
                Invoker.MethodInvoke(proxy, beginMethodName, list.ToArray());
            }
            catch
            {
                List<object> list = new List<object>(realArgs2);
                list.Add(cb);
                list.Add(proxy);
                //MethodInfo m = proxy.GetType().GetMethod(beginMethodName);
                Invoker.MethodInvoke(proxy, beginMethodName, list.ToArray());
            }
        }

        public static void InvokeAsync<T>(string url, string methodName, Action<T> callback, Action<Exception> errorHandler, params object[] args)
        {
            object proxy = CreateServiceProxyInstance(url);
            string beginMethodName = "Begin" + methodName;
            string endMethodName = "End" + methodName;
            Type t = proxy.GetType();
            MethodInfo beginMethod = t.GetMethod("Begin" + methodName);
            MethodInfo endMethod = t.GetMethod(endMethodName);
            object[] realArgs2;
            object[] realArgs = GetArgs(args, proxy.GetType().Assembly, beginMethod, out realArgs2);
            //MethodInfo beginMethod = t.GetMethod("Begin" + methodName);
            AsyncCallback cb = ar =>
            {
                T obj = default(T);
                bool hasError = false;
                try
                {
                    if (typeof(T).IsValueType)
                    {
                        try
                        {
                            bool hasValue = false;
                            object[] args1 = new object[] { ar, obj, hasValue };
                            endMethod.Invoke(ar.AsyncState, args1);
                            hasValue = (bool)args1[2];
                            if (hasValue)
                            {
                                obj = (T)SoapEntityMapping.ConvertFromSoapObject(args1[1], typeof(T));
                            }
                            else
                            {
                                obj = default(T);
                            }
                        }
                        catch
                        {
                            object[] args1 = new object[] { ar, obj };
                            endMethod.Invoke(ar.AsyncState, args1);
                            obj = (T)SoapEntityMapping.ConvertFromSoapObject(args1[1], typeof(T));
                        }
                    }
                    else
                    {
                        object tmp = Invoker.MethodInvoke(ar.AsyncState, endMethodName, ar);
                        obj = (T)SoapEntityMapping.ConvertFromSoapObject(tmp, typeof(T));
                    }
                }
                catch (Exception ex)
                {
                    hasError = true;
                    if (errorHandler != null)
                    {
                        errorHandler(ex);
                    }
                }
                if (hasError == false && callback != null)
                {
                    callback(obj);
                }
            };

            try
            {
                List<object> list = new List<object>(realArgs);
                list.Add(cb);
                list.Add(proxy);
                Invoker.MethodInvoke(proxy, beginMethodName, list.ToArray());
            }
            catch
            {
                List<object> list = new List<object>(realArgs2);
                list.Add(cb);
                list.Add(proxy);
                Invoker.MethodInvoke(proxy, beginMethodName, list.ToArray());
            }
        }

        public static object InitProxyInstance(string url)
        {
            return CreateServiceProxyInstance(url);
        }

        #region Private 

        private static object s_SyncObj_Instance = new object();
        private static object CreateServiceProxyInstance(string url)
        {
            string key = "SoapClient_" + url.ToUpper().Trim();
            object proxy = MemoryCache.Default.Get(key);
            if (proxy != null)
            {
                return proxy;
            }
            lock (key)
            {
                proxy = MemoryCache.Default.Get(key);
                if (proxy != null)
                {
                    return proxy;
                }
                Type t = CreateServiceProxyType(url);
                proxy = Activator.CreateInstance(t);
                MemoryCache.Default.Set(key, proxy, new CacheItemPolicy()
                {
                    AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(5)),
                });
                return proxy;
            }
        }

        private static Type CreateServiceProxyType(string url)
        {
            string @namespace = Namespace;
            WebClient wc = new WebClient();
            ServiceDescription sd;
            try
            {
                using (Stream stream = wc.OpenRead(url))
                {
                    sd = ServiceDescription.Read(stream);
                }
            }
            catch
            {
                try
                {
                    using (Stream stream = wc.OpenRead(url + "?singleWsdl"))
                    {
                        sd = ServiceDescription.Read(stream);
                    }
                }
                catch
                {
                    using (Stream stream = wc.OpenRead(url + "?WSDL"))
                    {
                        sd = ServiceDescription.Read(stream);
                    }
                }
            }
            string classname = sd.Services[0].Name;

            //生成客户端代理类代码
            CodeNamespace cn = new CodeNamespace(@namespace);
            CodeCompileUnit ccu = new CodeCompileUnit();
            ccu.Namespaces.Add(cn);

            ServiceDescriptionImporter sdi = new ServiceDescriptionImporter();
            sdi.ProtocolName = "Soap";
            sdi.Style = ServiceDescriptionImportStyle.Client;
            sdi.CodeGenerationOptions = CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateOldAsync;
            sdi.AddServiceDescription(sd, "", "");
            sdi.Import(cn, ccu);

            //设定编译参数
            CompilerParameters cplist = new CompilerParameters();
            cplist.GenerateExecutable = false;
            cplist.GenerateInMemory = true;
            cplist.ReferencedAssemblies.Add("System.dll");
            cplist.ReferencedAssemblies.Add("System.XML.dll");
            cplist.ReferencedAssemblies.Add("System.Web.Services.dll");
            cplist.ReferencedAssemblies.Add("System.Data.dll");
            //cplist.ReferencedAssemblies.Add("Fly.Framework.Common.dll");

            //编译代理类
            CSharpCodeProvider csc = new CSharpCodeProvider();
            CompilerResults cr = csc.CompileAssemblyFromDom(cplist, ccu);
            if (true == cr.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SoapServiceProxy为服务地址“" + url + "”生成代理类失败.\r\n");
                foreach (CompilerError ce in cr.Errors)
                {
                    sb.AppendLine(ce.ToString());
                }
                throw new ApplicationException(sb.ToString());
            }

            //生成代理实例
            Assembly assembly = cr.CompiledAssembly;
            return assembly.GetType(@namespace + "." + classname, true, true);
        }

        #endregion
    }
}
