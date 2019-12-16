using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Web;

namespace Fly.Framework.Common
{
    public static class Cache
    {
        private const string CACHE_LOCKER_PREFIX = "C_L_";

        public static T GetWithCache<K, T>(IDictionary<K, T> cache, K key, Func<K, T> getter)
        {
            T u;
            if (cache.TryGetValue(key, out u))
            {
                return u;
            }
            u = getter(key);
            cache.Add(key, u);
            return u;
        }

        #region Local Cache

        public static void RemoveFromLocalCache(string cacheKey)
        {
            string locker = CACHE_LOCKER_PREFIX + cacheKey;
            lock (locker)
            {
                MemoryCache.Default.Remove(cacheKey);
            }
        }

        public static void SetLocalCache(string cacheKey, object data, bool absoluteExpiration = true, int cacheExpirationMinutes = 30)
        {
            InnerSetLocalCache(cacheKey, data, absoluteExpiration, (double)cacheExpirationMinutes);
        }

        private static void InnerSetLocalCache(string cacheKey, object data, bool absoluteExpiration = true, double cacheExpirationMinutes = 30)
        {
            CacheItemPolicy cp = new CacheItemPolicy();
            if (absoluteExpiration)
            {
                cp.AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(cacheExpirationMinutes));
            }
            else
            {
                cp.SlidingExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes);
            }
            MemoryCache.Default.Set(cacheKey, (data == null ? (object)DBNull.Value : data), cp);
        }

        public static object GetLocalCache(string cacheKey)
        {
            return MemoryCache.Default.Get(cacheKey);
        }

        public static T GetLocalCache<T>(string cacheKey)
        {
            var obj = MemoryCache.Default.Get(cacheKey);
            if (obj == null || obj is DBNull)
            {
                return default(T);
            }
            return (T)obj;
        }

        public static T GetLocalCache<T>(string cacheKey, out bool found)
        {
            var obj = MemoryCache.Default.Get(cacheKey);
            if (obj == null)
            {
                found = false;
                return default(T);
            }
            found = true;
            if (obj is DBNull)
            {
                return default(T);
            }
            return (T)obj;
        }

        public static T GetWithLocalCache<T>(string cacheKey, Func<T> getter, bool absoluteExpiration = true, int cacheExpirationMinutes = 30)
            where T : class
        {
            return InnerGetWithLocalCache(cacheKey, getter, absoluteExpiration, (double)cacheExpirationMinutes);
        }

        private static T InnerGetWithLocalCache<T>(string cacheKey, Func<T> getter, bool absoluteExpiration = true, double cacheExpirationMinutes = 30)
            where T : class
        {
            object t = MemoryCache.Default.Get(cacheKey);
            if (t is DBNull)
            {
                return null;
            }
            T rst = t as T;
            if (rst != null)
            {
                return rst;
            }
            string locker = CACHE_LOCKER_PREFIX + cacheKey;
            lock (locker)
            {
                rst = MemoryCache.Default.Get(cacheKey) as T;
                if (rst != null)
                {
                    return rst;
                }
                rst = getter();
                CacheItemPolicy cp = new CacheItemPolicy();
                if (absoluteExpiration)
                {
                    cp.AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(cacheExpirationMinutes));
                }
                else
                {
                    cp.SlidingExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes);
                }
                MemoryCache.Default.Set(cacheKey, (rst == null ? (object)DBNull.Value : (object)rst), cp);
                return rst;
            }
        }

        public static T GetWithLocalCache<T>(string cacheKey, Func<T> getter, params string[] filePathList)
            where T : class
        {
            object t = MemoryCache.Default.Get(cacheKey);
            if (t is DBNull)
            {
                return null;
            }
            T rst = t as T;
            if (rst != null)
            {
                return rst;
            }
            string locker = CACHE_LOCKER_PREFIX + cacheKey;
            lock (locker)
            {
                rst = MemoryCache.Default.Get(cacheKey) as T;
                if (rst != null)
                {
                    return rst;
                }
                rst = getter();
                List<string> list = new List<string>(filePathList.Length);
                foreach (var file in filePathList)
                {
                    if (File.Exists(file))
                    {
                        list.Add(file);
                    }
                }
                if (list.Count > 0)
                {
                    CacheItemPolicy cp = new CacheItemPolicy();
                    cp.ChangeMonitors.Add(new HostFileChangeMonitor(list));
                    MemoryCache.Default.Set(cacheKey, (rst == null ? (object)DBNull.Value : (object)rst), cp);
                }
                return rst;
            }
        }

        #endregion

        #region File Cache

        public static string ReadTextFileWithLocalCache(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            FileInfo f = new FileInfo(filePath);
            string key = f.FullName.ToUpper().GetHashCode().ToString();
            return GetWithLocalCache<string>(key, () => LoadRawString(filePath), filePath);
        }

        public static T ReadXmlFileWithLocalCache<T>(string filePath)
            where T : class
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            FileInfo f = new FileInfo(filePath);
            string key = "ReadXmlFileWithCache_" + f.FullName.ToUpper().GetHashCode().ToString();
            return GetWithLocalCache<T>(key, () => Serialization.LoadFromXml<T>(filePath), filePath);
        }

        public static T ReadJsonFileWithLocalCache<T>(string filePath)
            where T : class
        {
            if (!File.Exists(filePath))
            {
                return null;
            }
            FileInfo f = new FileInfo(filePath);
            string key = "ReadJsonFileWithCache_" + f.FullName.ToUpper().GetHashCode().ToString();
            return GetWithLocalCache<T>(key, () => Serialization.JsonDeserialize<T>(LoadRawString(filePath)), filePath);
        }

        private static string LoadRawString(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath, Encoding.GetEncoding("gb2312"), true))
            {
                return sr.ReadToEnd();
            }
        }

        #endregion

        #region HttpContext Cache

        public static T GetWithHttpContextCache<T>(string cacheKey, Func<T> getter)
        {
            if (HttpContext.Current == null || HttpContext.Current.Items == null)
            {
                return getter();
            }
            if (HttpContext.Current.Items.Contains(cacheKey))
            {
                return (T)HttpContext.Current.Items[cacheKey];
            }
            T rst = getter();
            SetIntoHttpContextCache(cacheKey, rst);
            return rst;
        }

        public static T GetFromHttpContextCache<T>(string cacheKey)
        {
            bool findInCache;
            return GetFromHttpContextCache<T>(cacheKey, out findInCache);
        }

        public static T GetFromHttpContextCache<T>(string cacheKey, out bool findInCache)
        {
            if (HttpContext.Current != null && HttpContext.Current.Items != null && HttpContext.Current.Items.Contains(cacheKey))
            {
                findInCache = true;
                return (T)HttpContext.Current.Items[cacheKey];
            }
            findInCache = false;
            return default(T);
        }

        public static void RemoveFromHttpContextCache(string cacheKey)
        {
            if (HttpContext.Current == null || HttpContext.Current.Items == null)
            {
                return;
            }
            HttpContext.Current.Items.Remove(cacheKey);
        }

        public static void SetIntoHttpContextCache(string cacheKey, object value)
        {
            if (HttpContext.Current == null || HttpContext.Current.Items == null)
            {
                return;
            }
            HttpContext.Current.Items[cacheKey] = value;
        }

        #endregion

        #region Distributed Cache

        public static void RemoveFromDistributedCache(string cacheKey)
        {
            MemcachedCacheHelper.Remove(cacheKey);
        }

        public static void RemoveAllFromDistributedCacheGroup(string groupName)
        {
            MemcachedCacheHelper.RemoveGroup(groupName, true);
        }

        public static void RemoveKeyFromDistributedCacheGroup(string groupName, string cacheKey)
        {
            MemcachedCacheHelper.RemoveKeyFromGroup(groupName, cacheKey);
        }

        public static void SetDistributedCache(string cacheKey, object data, bool absoluteExpiration = true, int cacheExpirationMinutes = 30, string groupName = null)
        {
            if (absoluteExpiration)
            {
                MemcachedCacheHelper.Set(cacheKey, data, DateTime.Now.AddMinutes(cacheExpirationMinutes), groupName);
            }
            else
            {
                MemcachedCacheHelper.Set(cacheKey, data, TimeSpan.FromMinutes(cacheExpirationMinutes), groupName);
            }
        }

        public static T GetDistributedCache<T>(string cacheKey)
        {
            return MemcachedCacheHelper.Get<T>(cacheKey);
        }

        public static T GetDistributedCache<T>(string cacheKey, out bool found)
        {
            return MemcachedCacheHelper.Get<T>(cacheKey, out found);
        }

        public static IDictionary<string, T> GetDistributedCache<T>(IEnumerable<string> cacheKeyList)
        {
            if (cacheKeyList == null || cacheKeyList.Count() <= 0)
            {
                return new Dictionary<string, T>(0);
            }
            return MemcachedCacheHelper.Get<T>(cacheKeyList.ToArray());
        }

        public static IDictionary<string, T> GetDistributedCacheGroup<T>(string groupName)
        {
            return MemcachedCacheHelper.GetByGroup<T>(groupName);
        }

        public static T GetWithDistributedCache<T>(string cacheKey, Func<T> getter, bool absoluteExpiration = true, int cacheExpirationMinutes = 30, string groupName = null)
        {
			bool found;
            object t = MemcachedCacheHelper.Get(cacheKey, out found);
            if (found)
            {
                return (T)t;
            }
            T data = getter();
            if (absoluteExpiration)
            {
                MemcachedCacheHelper.Set(cacheKey, data, DateTime.Now.AddMinutes(cacheExpirationMinutes), groupName);
            }
            else
            {
                MemcachedCacheHelper.Set(cacheKey, data, TimeSpan.FromMinutes(cacheExpirationMinutes), groupName);
            }
            return data;
        }

        public static IDictionary<string, T> GetWithDistributedCache<T>(IEnumerable<string> cacheKeyList,
            Func<IDictionary<string, T>, IDictionary<string, T>> getter, // 入参为缓存中已经找到的数据（key为缓存key，value为数据本身），需要返回剩下未在缓存中找到的数据，一般为去数据库读取（key为将存入缓存所用的key，value为要缓存的数据本身）
            bool absoluteExpiration = true, int cacheExpirationMinutes = 30, string groupName = null)
        {
            if (cacheKeyList == null || cacheKeyList.Count() <= 0)
            {
                return new Dictionary<string, T>(0);
            }
            cacheKeyList = cacheKeyList.Distinct();

            var cachedFoundDicts = GetDistributedCache<T>(cacheKeyList);
            var newGottenDicts = getter(cachedFoundDicts);
            if (newGottenDicts != null && newGottenDicts.Count > 0)
            {
                foreach (var entry in newGottenDicts)
                {
                    if (cachedFoundDicts.ContainsKey(entry.Key))
                    {
                        cachedFoundDicts[entry.Key] = entry.Value;
                    }
                    else
                    {
                        cachedFoundDicts.Add(entry.Key, entry.Value);
                    }
                    SetDistributedCache(entry.Key, entry.Value, absoluteExpiration, cacheExpirationMinutes, groupName);
                }
            }
            return cachedFoundDicts;
        }

        public static int ConcurrentIncreaseFromDistributedCache(string key, int change)
        {
            if (change == 0)
            {
                var t = MemcachedCacheHelper.GetClientInstance().Get(key);
                if (t == null || t.GetType() != typeof(int))
                {
                    return 0;
                }
                return (int)t;
            }
            int v;
            ConcurrentUpdateFromDistributedCache(key, (int d, out bool need) =>
            {
                need = true;
                return d + change;
            }, out v);
            return v;
        }

        public static bool ConcurrentIncreaseIfConditionFromDistributedCache(string key, int change, Func<int, bool> valueAfterChangeCondition, out int increasedValue)
        {
            if (change == 0)
            {
                var t = MemcachedCacheHelper.GetClientInstance().Get(key);
                if (t == null || t.GetType() != typeof(int))
                {
                    increasedValue = 0;
                }
                else
                {
                    increasedValue = (int)t;
                }
                return true;
            }
            if (valueAfterChangeCondition == null)
            {
                throw new ArgumentNullException("changedValueCondition");
            }
            return ConcurrentUpdateFromDistributedCache(key, (int d, out bool need) =>
            {
                need = true;
                d = d + change;
                if (valueAfterChangeCondition != null && valueAfterChangeCondition(d) == false)
                {
                    need = false;
                }
                return d;
            }, out increasedValue);
        }

        public static IEnumerable<T> ConcurrentAddItemIntoListFromDistributedCache<T>(string key, T item, Func<T, T, bool> comparer = null, Func<List<T>> dataInitializer = null)
        {
			List<T> list;
			bool success = ConcurrentUpdateFromDistributedCache(key, (List<T> d, out bool need) =>
            {
                need = false;
                if (d == null)
                {
                    need = true;
                    if (dataInitializer != null)
                    {
                        d = dataInitializer();
                    }
                    if (d == null) // 没有初始化，或者初始化返回的仍然为空
                    {
                        d = new List<T>();
                    }
                }
                if (comparer == null // 不需要排重
                    || d.Exists(x => comparer(x, item)) == false) // 根据comparer的委托来排重
                {
                    need = true;
                    d.Add(item);
                }
                return d;
            }, out list);

			if (success)
            {
                return list;
            }
            return null;
        }

        public static IEnumerable<T> ConcurrentRemoveItemFromListFromDistributedCache<T>(string key, T item, Func<T, T, bool> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

            List<T> list;
            bool success = ConcurrentUpdateFromDistributedCache(key, (List<T> d, out bool need) =>
            {
                need = false;
                if (d == null)
                {
                    return null;
                }
                int idx = d.FindIndex(x => comparer(x, item));
                if (idx >= 0)
                {
                    need = true;
                    d.RemoveAt(idx);
                }
                return d;
            }, out list);

			if (success)
            {
                return list;
            }
            return null;
        }

        public static bool ConcurrentUpdateFromDistributedCache<T>(string key, MemcachedConcurrentUpdater<T> updater, out T updatedData)
        {
            return MemcachedCacheHelper.ConcurrentUpdate(key, updater, out updatedData);
        }

        public static int GenerateNewSeedInDistributedCache(string seedKey, uint start = 1, uint step = 1)
        {
            return (int)MemcachedCacheHelper.GenerateNewSeed(seedKey, start, step);
        }

        #endregion

        #region Local And Distributed Cache

        public static void RemoveFromLocalAndDistributedCache(string cacheKey)
        {
            RemoveFromLocalCache(cacheKey);
            RemoveFromDistributedCache(cacheKey);
        }

        public static T GetWithLocalAndDistributedCache<T>(string cacheKey, Func<T> getter,
            bool localAbsoluteExpiration = true, double localCacheExpirationMinutes = 30,
            bool distributedAbsoluteExpiration = true, int distributedCacheExpirationMinutes = 30)
            where T : class
        {
            return InnerGetWithLocalCache(cacheKey, () =>
            {
                return GetWithDistributedCache<T>(cacheKey, getter, distributedAbsoluteExpiration, distributedCacheExpirationMinutes);
            }, localAbsoluteExpiration, localCacheExpirationMinutes);
        }

        public static IDictionary<string, T> GetWithLocalAndDistributedCache<T>(IEnumerable<string> cacheKeyList,
            Func<IDictionary<string, T>, IDictionary<string, T>> getter, // 入参为缓存中已经找到的数据（key为缓存key，value为数据本身），需要返回剩下未在缓存中找到的数据，一般为去数据库读取（key为将存入缓存所用的key，value为要缓存的数据本身）
            bool localAbsoluteExpiration = true, double localCacheExpirationMinutes = 30,
            bool distributedAbsoluteExpiration = true, int distributedCacheExpirationMinutes = 30)
        {
            var cachedFoundDicts = GetLocalAndDistributedCache<T>(cacheKeyList, localAbsoluteExpiration, localCacheExpirationMinutes);
            var newGottenDicts = getter(cachedFoundDicts);
            if (newGottenDicts != null && newGottenDicts.Count > 0)
            {
                foreach (var entry in newGottenDicts)
                {
                    if (cachedFoundDicts.ContainsKey(entry.Key))
                    {
                        cachedFoundDicts[entry.Key] = entry.Value;
                    }
                    else
                    {
                        cachedFoundDicts.Add(entry.Key, entry.Value);
                    }
                    SetLocalAndDistributedCache(entry.Key, entry.Value, localAbsoluteExpiration, localCacheExpirationMinutes, distributedAbsoluteExpiration, distributedCacheExpirationMinutes);
                }
            }
            return cachedFoundDicts;
        }

        public static T GetLocalAndDistributedCache<T>(string cacheKey, bool localAbsoluteExpiration = true, double localCacheExpirationMinutes = 30)
        {
			bool found;
            return GetLocalAndDistributedCache<T>(cacheKey, out found, localAbsoluteExpiration, localCacheExpirationMinutes);
        }

        public static T GetLocalAndDistributedCache<T>(string cacheKey, out bool found, bool localAbsoluteExpiration = true, double localCacheExpirationMinutes = 30)
        {
            var obj = GetLocalCache<T>(cacheKey, out found);
            if (found)
            {
                return obj;
            }
            obj = GetDistributedCache<T>(cacheKey, out found);
            if (found)
            {
                InnerSetLocalCache(cacheKey, obj, localAbsoluteExpiration, localCacheExpirationMinutes);
            }
            return obj == null ? default(T) : (T)obj;
        }

        public static IDictionary<string, T> GetLocalAndDistributedCache<T>(IEnumerable<string> cacheKeyList, bool localAbsoluteExpiration = true, double localCacheExpirationMinutes = 30)
        {
            if (cacheKeyList == null || cacheKeyList.Count() <= 0)
            {
                return new Dictionary<string, T>(0);
            }
            cacheKeyList = cacheKeyList.Distinct();
            Dictionary<string, T> localFoundObjList = new Dictionary<string, T>(cacheKeyList.Count() * 2);
            List<string> localNotFoundKeys = new List<string>(cacheKeyList.Count() * 2);
            foreach (var key in cacheKeyList)
            {
				bool found;
                var obj = GetLocalCache<T>(key, out found);
                if (found)
                {
                    localFoundObjList.Add(key, obj);
                }
                else
                {
                    localNotFoundKeys.Add(key);
                }
            }
            if (localNotFoundKeys.Count > 0)
            {
                var d = GetDistributedCache<T>(localNotFoundKeys);
                if (d != null && d.Count() > 0)
                {
                    foreach (var entry in d)
                    {
                        if (localFoundObjList.ContainsKey(entry.Key))
                        {
                            localFoundObjList[entry.Key] = entry.Value;
                        }
                        else
                        {
                            localFoundObjList.Add(entry.Key, entry.Value);
                        }
                        InnerSetLocalCache(entry.Key, entry.Value, localAbsoluteExpiration, localCacheExpirationMinutes);
                    }
                }
            }
            return localFoundObjList;
        }

        public static void SetLocalAndDistributedCache(string cacheKey, object data,
            bool localAbsoluteExpiration = true, double localCacheExpirationMinutes = 30,
            bool distributedAbsoluteExpiration = true, int distributedCacheExpirationMinutes = 30)
        {
            SetDistributedCache(cacheKey, data, distributedAbsoluteExpiration, distributedCacheExpirationMinutes);
            InnerSetLocalCache(cacheKey, data, localAbsoluteExpiration, localCacheExpirationMinutes);
        }

        #endregion
    }
}
