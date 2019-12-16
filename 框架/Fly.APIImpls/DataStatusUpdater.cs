using Fly.Framework.Common;
using Fly.Framework.SqlDbAccess;
using Fly.APIs;
using Fly.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIImpls
{
    internal class DataStatusUpdater
    {
        private string m_UpdateSql;
        private bool m_UpdateLog;
        public DataStatusUpdater(string tableName) : this("[Id]", "[Status]", tableName, true)
        {

        }

        public DataStatusUpdater(string tableName, bool updateLog)
            : this("[Id]", "[Status]", tableName, updateLog)
        {

        }

        public DataStatusUpdater(string idFieldName, string statusFieldName, string tableName, bool updateLog)
        {
            m_UpdateLog = updateLog;
            if (updateLog)
            {
                m_UpdateSql = "UPDATE " + tableName + " SET " + statusFieldName + "={0}, [UpdatedOn]=GETDATE(), [UpdatedBy]='{2}' WHERE " + idFieldName + "='{1}' AND " + statusFieldName + "<>{0}";
            }
            else
            {
                m_UpdateSql = "UPDATE " + tableName + " SET " + statusFieldName + "={0} WHERE " + idFieldName + "='{1}' AND " + statusFieldName + "<>{0}";
            }
        }

        public bool Enable(Guid id)
        {
            string sql = m_UpdateLog ? string.Format(m_UpdateSql, (int)DataStatus.Enabled, id, ContextManager.Current.UserId) : string.Format(m_UpdateSql, (int)DataStatus.Enabled, id);
            int i = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql);
            return i > 0;
        }

        public bool Disable(Guid id)
        {
            string sql = m_UpdateLog ? string.Format(m_UpdateSql, (int)DataStatus.Disabled, id, ContextManager.Current.UserId) : string.Format(m_UpdateSql, (int)DataStatus.Disabled, id);
            int i = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql);
            return i > 0;
        }

        public bool Delete(Guid id)
        {
            string sql = m_UpdateLog ? string.Format(m_UpdateSql, (int)DataStatus.Deleted, id, ContextManager.Current.UserId) : string.Format(m_UpdateSql, (int)DataStatus.Deleted, id);
            int i = SqlHelper.ExecuteNonQuery(DbInstance.CanWrite, sql);
            return i > 0;
        }
    }
}
