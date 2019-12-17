using Fly.Framework.SqlDbAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIImpls
{
    internal class CodeAndIdMapper
    {
        private Dictionary<Guid, string> m_IdToCodeMapper = new Dictionary<Guid, string>();
        private Dictionary<string, Guid> m_CodeToIdMapper = new Dictionary<string, Guid>();
        private object m_SyncObj = new object();
        private string m_Sql1;
        private string m_Sql2;

        public CodeAndIdMapper(string tableName) : this("[Id]", "[Code]", tableName)
        {

        }

        public CodeAndIdMapper(string idFieldName, string codeFieldName, string tableName)
        {
            m_Sql1 = "SELECT " + idFieldName + ", " + codeFieldName + " FROM " + tableName + " WITH(NOLOCK) WHERE " + codeFieldName + " IN ('{0}')";
            m_Sql2 = "SELECT " + idFieldName + ", " + codeFieldName + " FROM " + tableName + " WITH(NOLOCK) WHERE " + idFieldName + " IN ('{0}')";
        }

        public void SetMapping(Guid id, string code)
        {
            code = code.ToUpper();
            lock (m_SyncObj)
            {
                if (m_IdToCodeMapper.ContainsKey(id))
                {
                    m_IdToCodeMapper[id] = code;
                }
                else
                {
                    m_IdToCodeMapper.Add(id, code);
                }
                if (m_CodeToIdMapper.ContainsKey(code))
                {
                    m_CodeToIdMapper[code] = id;
                }
                else
                {
                    m_CodeToIdMapper.Add(code, id);
                }
            }
        }

        public void SetMapping(List<KeyValuePair<Guid, string>> items)
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
                    string code = item.Value;
                    if (m_IdToCodeMapper.ContainsKey(id))
                    {
                        m_IdToCodeMapper[id] = code;
                    }
                    else
                    {
                        m_IdToCodeMapper.Add(id, code);
                    }
                    if (m_CodeToIdMapper.ContainsKey(code))
                    {
                        m_CodeToIdMapper[code] = id;
                    }
                    else
                    {
                        m_CodeToIdMapper.Add(code, id);
                    }
                }
            }
        }

        public Guid? GetId(string code)
        {
            code = code.ToUpper();
            var dic = GetId(new string[] { code });
            Guid id;
            if (dic.TryGetValue(code, out id))
            {
                return id;
            }
            return null;
        }

        public string GetCode(Guid id)
        {
            var dic = GetCode(new Guid[] { id });
            string num;
            if (dic.TryGetValue(id, out num))
            {
                return num;
            }
            return null;
        }

        public IDictionary<string, Guid> GetId(IEnumerable<string> codeList)
        {
            codeList = codeList.Select(r => r.ToUpper());
            if (codeList == null)
            {
                return new Dictionary<string, Guid>(0);
            }
            int c = codeList.Distinct().Count();
            if (c <= 0)
            {
                return new Dictionary<string, Guid>(0);
            }
            Dictionary<string, Guid> dic = new Dictionary<string, Guid>(c * 2);
            List<string> noCacheList = new List<string>(c);
            Guid id;
            foreach (var code in codeList)
            {
                if (dic.ContainsKey(code) || noCacheList.Contains(code))
                {
                    continue;
                }
                if (m_CodeToIdMapper.TryGetValue(code, out id))
                {
                    dic.Add(code, id);
                }
                else
                {
                    noCacheList.Add(code);
                }
            }
            if (noCacheList.Count <= 0)
            {
                return dic;
            }

            List<string> noCacheList2 = new List<string>(noCacheList.Count);
            lock (m_SyncObj)
            {
                foreach (var num in noCacheList)
                {
                    if (m_CodeToIdMapper.TryGetValue(num, out id))
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
                string numsStr = string.Join("','", noCacheList2);
                string sql = string.Format(m_Sql1, numsStr);
                var list = SqlHelper.ExecuteList<dynamic>(r => new { Id = r.GetGuid(0), Code = r.GetString(1) }, DbInstance.OnlyRead, sql);
                foreach (var item in list)
                {
                    Guid nid = item.Id;
                    string code = item.Code;
                    dic.Add(code, nid);
                    m_CodeToIdMapper.Add(code, nid);
                    if (m_IdToCodeMapper.ContainsKey(nid))
                    {
                        m_IdToCodeMapper[nid] = code;
                    }
                    else
                    {
                        m_IdToCodeMapper.Add(nid, code);
                    }
                }
                return dic;
            }
        }

        public IDictionary<Guid, string> GetCode(IEnumerable<Guid> idList)
        {
            if (idList == null)
            {
                return new Dictionary<Guid, string>(0);
            }
            int c = idList.Distinct().Count();
            if (c <= 0)
            {
                return new Dictionary<Guid, string>(0);
            }
            Dictionary<Guid, string> dic = new Dictionary<Guid, string>(c * 2);
            List<Guid> noCacheList = new List<Guid>(c);
            string code;
            foreach (var bId in idList)
            {
                if (dic.ContainsKey(bId) || noCacheList.Contains(bId))
                {
                    continue;
                }
                if (m_IdToCodeMapper.TryGetValue(bId, out code))
                {
                    dic.Add(bId, code);
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
                    if (m_IdToCodeMapper.TryGetValue(id, out code))
                    {
                        dic.Add(id, code);
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
                string idsStr = string.Join("','", noCacheList2);
                string sql = string.Format(m_Sql2, idsStr);
                var list = SqlHelper.ExecuteList<dynamic>(r => new { Id = r.GetGuid(0), Code = r.GetString(1) }, DbInstance.OnlyRead, sql);
                foreach (var item in list)
                {
                    Guid nid = item.Id;
                    string cc = item.Code;
                    dic.Add(nid, cc);
                    m_IdToCodeMapper.Add(nid, cc);
                    if (m_CodeToIdMapper.ContainsKey(cc))
                    {
                        m_CodeToIdMapper[cc] = nid;
                    }
                    else
                    {
                        m_CodeToIdMapper.Add(cc, nid);
                    }
                }
                return dic;
            }
        }
    }
}
