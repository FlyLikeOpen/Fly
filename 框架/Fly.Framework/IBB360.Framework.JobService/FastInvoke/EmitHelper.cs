using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace IBB360.Framework.JobService.FastInvoke
{
    internal interface IMethodEmit
    {
        void CreateMethod(ILGenerator ilg, MethodInfo methodInfo);
    }

    internal static class EmitHelper
    {
        private static ModuleBuilder s_ModuleBuilder = null;
        private static AssemblyBuilder s_AssemblyBuilder = null;
        private static object s_ModuleSyncObj = new object();

        private static ModuleBuilder GetModuleBuilder(bool isPersist, out AssemblyBuilder asmBuilder)
        {
            if (s_ModuleBuilder == null)
            {
                lock (s_ModuleSyncObj)
                {
                    if (s_ModuleBuilder == null)
                    {
                        AssemblyBuilderAccess aba;
                        if (isPersist)
                        {
                            aba = AssemblyBuilderAccess.RunAndSave;
                        }
                        else
                        {
                            aba = AssemblyBuilderAccess.Run;
                        }
                        AssemblyName an = new AssemblyName();
                        an.Name = "IBB360.DynamicType";
                        asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, aba, AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                        ModuleBuilder mdlBuilder;
                        if (isPersist)
                        {
                            mdlBuilder = asmBuilder.DefineDynamicModule("IBB360.DynamicType.Module_Impl", "ECommerce5.DynamicType.dll");
                        }
                        else
                        {
                            mdlBuilder = asmBuilder.DefineDynamicModule("IBB360.DynamicType.Module_Impl");
                        }
                        s_AssemblyBuilder = asmBuilder;
                        s_ModuleBuilder = mdlBuilder;
                    }
                }
            }
            asmBuilder = s_AssemblyBuilder;
            return s_ModuleBuilder;
        }

        private static string GenerateStringID()
        {
            long i = 1;
            byte[] array = Guid.NewGuid().ToByteArray();
            foreach (byte b in array)
            {
                i *= ((int)b + 1);
            }
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }

        public static Type CreateType(Type interfaceType, IMethodEmit methodEmitter)
        {
            return CreateType(interfaceType, methodEmitter, false);
        }

        public static Type CreateType(Type interfaceType, IMethodEmit methodEmitter, bool isPersist)
        {
            string name = string.Empty;
            if (interfaceType.IsGenericType)
            {
                name = interfaceType.GetGenericTypeDefinition().Name.Replace("`1", "_" + interfaceType.GetGenericArguments()[0].Name ) + "_" + GenerateStringID();
            }
            else
            {
                name = interfaceType.Name + "_" + GenerateStringID();
            }
            return CreateType(interfaceType, methodEmitter, isPersist, name);
        }

        public static Type CreateType(Type interfaceType, IMethodEmit methodEmitter, bool isPersist, string prefix)
        {
            AssemblyBuilder asmBuilder;
            ModuleBuilder mdlBuilder = GetModuleBuilder(isPersist, out asmBuilder);

            TypeAttributes typeAttributes =
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed;

            TypeBuilder typeBuilder = mdlBuilder.DefineType(prefix + "_Impl", typeAttributes);
            InnerCreateType(typeBuilder, interfaceType, methodEmitter);

            Type tmp = typeBuilder.CreateType();
            if (isPersist)
            {
                asmBuilder.Save("IBB360.DynamicType.dll");
            }
            return tmp;
        }

        private static void InnerCreateType(TypeBuilder typeBuilder, Type interfaceType, IMethodEmit methodEmitter)
        {
            typeBuilder.AddInterfaceImplementation(interfaceType);
            foreach (MemberInfo member in interfaceType.GetMembers(BindingFlags.Instance | BindingFlags.Public))
            {
                //约定-成员必须是方法，不能有属性，事件之类的
                if (member.MemberType != MemberTypes.Method)
                {
                    throw (new ApplicationException("Could not emit " + member.MemberType + " automatically!"));
                }
                CreateMethod(typeBuilder, (MethodInfo)member, methodEmitter);
            }

            //获取派生自的父接口
            Type[] typeList = interfaceType.GetInterfaces();
            if (typeList == null || typeList.Length <= 0)
            {
                return;
            }

            //为父接口实现方法
            for (int i = 0; i < typeList.Length; i++)
            {
                InnerCreateType(typeBuilder, typeList[i], methodEmitter);
            }
        }

        private static void CreateMethod(TypeBuilder typeBuilder, MethodInfo methodInfo, IMethodEmit methodEmitter)
        {
            ParameterInfo[] paramInfos = methodInfo.GetParameters();
            int paramLength = paramInfos.Length;

            //取得参数的类型数组
            Type[] paramTypes = new Type[paramLength];
            for (int i = 0; i < paramLength; i++)
            {
                paramTypes[i] = paramInfos[i].ParameterType;
            }

            //在m_TypeBuilder上建立新方法，参数类型与返回类型都与接口上的方法一致
            MethodBuilder mthBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, methodInfo.ReturnType, paramTypes);

            //指定新方法是实现接口的方法的。
            typeBuilder.DefineMethodOverride(mthBuilder, methodInfo);

            //复制新方法上的参数的名字和属性
            for (int i = 0; i < paramLength; i++)
            {
                ParameterInfo pi = paramInfos[i];
                //对于Instance,参数position由1开始
                mthBuilder.DefineParameter(i + 1, pi.Attributes, pi.Name);
            }

            Type returnType = methodInfo.ReturnType;

            //ILGenerator 是用于生成实现代码的对象
            ILGenerator ilg = mthBuilder.GetILGenerator();

            methodEmitter.CreateMethod(ilg, methodInfo);
        }
    }
}
