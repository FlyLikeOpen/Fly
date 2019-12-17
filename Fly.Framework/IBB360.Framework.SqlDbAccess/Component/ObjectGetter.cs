using Fly.Framework.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.SqlDbAccess.Component
{
    public class ObjectGetter<T> where T : class, new()
    {
        private string m_Sql1;
        private string m_Sql2;

        public ObjectGetter(string tableName)
            : this("[Id]", tableName)
        {

        }

        public ObjectGetter(string idFieldName, string tableName)
            : this(idFieldName, tableName, null)
        {

        }

        public ObjectGetter(string idFieldName, string tableName, string selectFields)
        {
            if (string.IsNullOrWhiteSpace(selectFields))
            {
                selectFields = "*";
            }
            if (tableName.IndexOf("WITH(NOLOCK)", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                tableName = tableName + " WITH(NOLOCK)";
            }
            m_Sql1 = "SELECT TOP 1 " + selectFields + " FROM " + tableName + " WHERE " + idFieldName + "='{0}'";
            m_Sql2 = "SELECT " + selectFields + " FROM " + tableName + " WHERE " + idFieldName + " IN ('{0}')";
        }

        public T Get(Guid id)
        {
            return Cache.GetWithHttpContextCache<T>("Get_" + id, () =>
            {
                string sql = string.Format(m_Sql1, id);
                return SqlHelper.ExecuteEntity<T>(DbInstance.OnlyRead, sql);
            });
        }

        public void ClearCacheForGet(Guid id)
        {
            Cache.RemoveFromHttpContextCache("Get_" + id);
        }

        public IList<T> Get(IEnumerable<Guid> idList)
        {
            if (idList == null || idList.Count() <= 0)
            {
                return new List<T>(0);
            }
            string str = string.Join("','", idList.Distinct());
            return Cache.GetWithHttpContextCache<IList<T>>("BulkGet_" + str, () =>
            {
                string sql = string.Format(m_Sql2, str);
                return SqlHelper.ExecuteEntityList<T>(DbInstance.OnlyRead, sql);
            });
        }

        public IList<T> QueryByIdNumber(NumberAndIdMapper mapper, long idNumber, out int totalCount)
        {
            totalCount = 0;
            Guid? gid = mapper.GetId(idNumber);
            if (gid == null)
            {
                return new List<T>(0);
            }
            var entity = Get(gid.Value);
            if (entity == null)
            {
                return new List<T>(0);
            }
            totalCount = 1;
            return new List<T> { entity };
        }
    }
}
