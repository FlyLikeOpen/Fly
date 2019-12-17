using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace Fly.Framework.SqlDbAccess
{
    public interface IDbFactory
    {
        DbCommand CreateCommand();
        DbConnection CreateConnection();
        DbConnection CreateConnection(string connectionString);
        DbDataAdapter CreateDataAdapter();
        DbParameter CreateParameter();
        string BuildParameterName(string name);
        bool SupportBatchSql { get; }
        bool SupportTransaction { get; }
    }
}
