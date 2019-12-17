using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.SqlDbAccess.Component
{
    public class NumberAndIdMapper
    {
        private Dictionary<Guid, long> m_IdToNumberMapper = new Dictionary<Guid, long>();
        private Dictionary<long, Guid> m_NumberToIdMapper = new Dictionary<long, Guid>();
        private object m_SyncObj = new object();
        private string m_Sql1;
        private string m_Sql2;

        public NumberAndIdMapper(string tableName)
            : this("[Id]", "[IdNumber]", tableName)
        {

        }

        public NumberAndIdMapper(string idFieldName, string numberFieldName, string tableName)
        {
            m_Sql1 = "SELECT " + idFieldName + ", " + numberFieldName + " FROM " + tableName + " WITH(NOLOCK) WHERE " + numberFieldName + " IN ({0})";
            m_Sql2 = "SELECT " + idFieldName + ", " + numberFieldName + " FROM " + tableName + " WITH(NOLOCK) WHERE " + idFieldName + " IN ('{0}')";
        }

        public void SetMapping(Guid id, long number)
        {
            lock (m_SyncObj)
            {
                if (m_IdToNumberMapper.ContainsKey(id))
                {
                    m_IdToNumberMapper[id] = number;
                }
                else
                {
                    m_IdToNumberMapper.Add(id, number);
                }
                if (m_NumberToIdMapper.ContainsKey(number))
                {
                    m_NumberToIdMapper[number] = id;
                }
                else
                {
                    m_NumberToIdMapper.Add(number, id);
                }
            }
        }

        public void SetMapping(List<KeyValuePair<Guid, long>> items)
        {
            if (items == null || items.Count <= 0)
            {
                return;
            }
            lock (m_SyncObj)
            {
                foreach (var item in items)
                {
                    Guid id = item.Key;
                    long number = item.Value;
                    if (m_IdToNumberMapper.ContainsKey(id))
                    {
                        m_IdToNumberMapper[id] = number;
                    }
                    else
                    {
                        m_IdToNumberMapper.Add(id, number);
                    }
                    if (m_NumberToIdMapper.ContainsKey(number))
                    {
                        m_NumberToIdMapper[number] = id;
                    }
                    else
                    {
                        m_NumberToIdMapper.Add(number, id);
                    }
                }
            }
        }

        public Guid? GetId(long idNumber)
        {
            var dic = GetId(new long[] { idNumber });
            Guid id;
            if (dic.TryGetValue(idNumber, out id))
            {
                return id;
            }
            return null;
        }

        public long? GetIdNumber(Guid id)
        {
            var dic = GetIdNumber(new Guid[] { id });
            long num;
            if (dic.TryGetValue(id, out num))
            {
                return num;
            }
            return null;
        }

        public IDictionary<long, Guid> GetId(IEnumerable<long> idNumberList)
        {
            if (idNumberList == null)
            {
                return new Dictionary<long, Guid>(0);
            }
            int c = idNumberList.Count();
            if (c <= 0)
            {
                return new Dictionary<long, Guid>(0);
            }
            Dictionary<long, Guid> dic = new Dictionary<long, Guid>(c * 2);
            List<long> noCacheList = new List<long>(c);
            Guid id;
            foreach (var number in idNumberList)
            {
                if (dic.ContainsKey(number) || noCacheList.Contains(number))
                {
                    continue;
                }
                if (m_NumberToIdMapper.TryGetValue(number, out id))
                {
                    dic.Add(number, id);
                }
                else
                {
                    noCacheList.Add(number);
                }
            }
            if (noCacheList.Count <= 0)
            {
                return dic;
            }

            List<long> noCacheList2 = new List<long>(noCacheList.Count);
            lock (m_SyncObj)
            {
                foreach (var num in noCacheList)
                {
                    if (m_NumberToIdMapper.TryGetValue(num, out id))
                    {
                        dic.Add(num, id);
                    }
                    else
                    {
                        noCacheList2.Add(num);
                    }
                }
                if (noCacheList2.Count <= 0)
                {
                    return dic;
                }
                string numsStr = string.Join(", ", noCacheList2);
                string sql = string.Format(m_Sql1, numsStr);
                var list = SqlHelper.ExecuteList<dynamic>(r => new { Id = r.GetGuid(0), IdNumber = Convert.ToInt64(r[1]) }, DbInstance.OnlyRead, sql);
                foreach (var item in list)
                {
                    Guid nid = item.Id;
                    long num = item.IdNumber;
                    dic.Add(num, nid);
                    m_NumberToIdMapper.Add(num, nid);
                    if (m_IdToNumberMapper.ContainsKey(nid))
                    {
                        m_IdToNumberMapper[nid] = num;
                    }
                    else
                    {
                        m_IdToNumberMapper.Add(nid, num);
                    }
                }
                return dic;
            }
        }

        public IDictionary<Guid, long> GetIdNumber(IEnumerable<Guid> idList)
        {
            if (idList == null)
            {
                return new Dictionary<Guid, long>(0);
            }
            int c = idList.Count();
            if (c <= 0)
            {
                return new Dictionary<Guid, long>(0);
            }
            Dictionary<Guid, long> dic = new Dictionary<Guid, long>(c * 2);
            List<Guid> noCacheList = new List<Guid>(c);
            long number;
            foreach (var bId in idList)
            {
                if (dic.ContainsKey(bId) || noCacheList.Contains(bId))
                {
                    continue;
                }
                if (m_IdToNumberMapper.TryGetValue(bId, out number))
                {
                    dic.Add(bId, number);
                }
                else
                {
                    noCacheList.Add(bId);
                }
            }
            if (noCacheList.Count <= 0)
            {
                return dic;
            }

            List<Guid> noCacheList2 = new List<Guid>(noCacheList.Count);
            lock (m_SyncObj)
            {
                foreach (var id in noCacheList)
                {
                    if (m_IdToNumberMapper.TryGetValue(id, out number))
                    {
                        dic.Add(id, number);
                    }
                    else
                    {
                        noCacheList2.Add(id);
                    }
                }
                if (noCacheList2.Count <= 0)
                {
                    return dic;
                }
                string idsStr = string.Join("', '", noCacheList2);
                string sql = string.Format(m_Sql2, idsStr);
                var list = SqlHelper.ExecuteList<dynamic>(r => new { Id = r.GetGuid(0), IdNumber = Convert.ToInt64(r[1]) }, DbInstance.OnlyRead, sql);
                foreach (var item in list)
                {
                    Guid nid = item.Id;
                    long num = item.IdNumber;
                    dic.Add(nid, num);
                    m_IdToNumberMapper.Add(nid, num);
                    if (m_NumberToIdMapper.ContainsKey(num))
                    {
                        m_NumberToIdMapper[num] = nid;
                    }
                    else
                    {
                        m_NumberToIdMapper.Add(num, nid);
                    }
                }
                return dic;
            }
        }

    }
}
