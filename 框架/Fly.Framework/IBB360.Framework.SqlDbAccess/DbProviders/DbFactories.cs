using System;
using System.Collections.Generic;
using System.Text;

namespace Fly.Framework.SqlDbAccess
{
    internal static class DbFactories
    {
        public static IDbFactory GetFactory(ProviderType providerType)
        {
            switch (providerType)
            {
                case ProviderType.SqlServer :
                    return SqlServerFactory.Instance;
                default :
                    throw new NotImplementedException();
            }
        }
    }

    public enum ProviderType
    {
        SqlServer
    }
}
