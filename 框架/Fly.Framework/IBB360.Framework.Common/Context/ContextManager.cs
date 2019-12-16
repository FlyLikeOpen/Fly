using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;

namespace Fly.Framework.Common
{
    public static class ContextManager
    {
        private static Type s_Type = null;

        public static void SetAsUnitTest(Guid userId, string sessionId, Dictionary<string, string> contextData = null)
        {
            UnitTestContext.SetContextData(userId, sessionId, contextData);
            s_Type = typeof(UnitTestContext);
        }

        public static void SetContextType(Type type)
        {
            if (typeof(IContext).IsAssignableFrom(type) == false)
            {
                throw new ApplicationException(string.Format("The type '{0}' has not implemented the interface '{1}'.", type.AssemblyQualifiedName, typeof(IContext).AssemblyQualifiedName));
            }
            s_Type = type;
        }

        private static Type GetContextType()
        {
            if (s_Type != null)
            {
                return s_Type;
            }
            string contextTypeName = ConfigurationManager.AppSettings["ContextType"];
            if (contextTypeName != null && contextTypeName.Trim().Length > 0)
            {
                s_Type = Type.GetType(contextTypeName, true);
            }
            else // 默认使用WebContext
            {
                //s_Type = Type.GetType("Fly.Web.Core.WebContext, Fly.Web.Core", true);
                throw new ApplicationException("请先配置config文件中AppSettings节点的ContextType，用以指定上下文IContext的实现类");
            }
            return s_Type;
        }

        public static IContext Current
        {
            get
            {
                Type t = GetContextType();
                if (t == typeof(UnitTestContext))
                {
                    return new UnitTestContext();
                }
                return  (IContext)Invoker.CreateInstance(t);
            }
        }
    }
}
