using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;

namespace Fly.Framework.Common
{
    internal class MemcachedClientWrapper
    {
        private MemcachedClient m_Client;

        public MemcachedClientWrapper(MemcachedClient client)
        {
            m_Client = client;
        }

        public virtual bool Replace(string key, object value, TimeSpan expiry)
        {
            return m_Client.Replace(key, value, expiry);
        }

        public virtual object Gets(string key, out ulong unique)
        {
            return m_Client.Gets(key, out unique);
        }

        public virtual MemcachedClient.CasResult CheckAndSet(string key, object value, ulong unique)
        {
            return m_Client.CheckAndSet(key, value, unique);
        }

        public virtual bool Delete(string key)
        {
            return m_Client.Delete(key);
        }

        public virtual bool Delete(string key, uint delaySeconds)
        {
            return m_Client.Delete(key, TimeSpan.FromSeconds(delaySeconds));
        }

        public virtual object Get(string key)
        {
            return m_Client.Get(key);
        }

        public virtual object[] Get(string[] keys)
        {
            return m_Client.Get(keys);
        }

        public virtual bool Set(string key, object value)
        {
            return m_Client.Set(key, value);
        }

        public virtual bool Set(string key, object value, DateTime expiry)
        {
            return m_Client.Set(key, value, expiry);
        }

        public virtual bool Set(string key, object value, TimeSpan expiry)
        {
            return m_Client.Set(key, value, expiry);
        }

        public virtual ulong? Increment(string key, ulong value)
        {
            return m_Client.Increment(key, value);
        }

        public virtual ulong? Decrement(string key, ulong value)
        {
            return m_Client.Decrement(key, value);
        }

        public virtual bool Add(string key, object value)
        {
            return m_Client.Add(key, value);
        }
    }

    internal class DisabledMemcachedClientWrapper : MemcachedClientWrapper
    {
        public DisabledMemcachedClientWrapper() : base(null)
        {

        }

        public override bool Replace(string key, object value, TimeSpan expiry)
        {
            return true;
        }

        public override object Gets(string key, out ulong unique)
        {
            unique = 0;
            return MemoryCache.Default.Get(key);
        }

        public override MemcachedClient.CasResult CheckAndSet(string key, object value, ulong unique)
        {
            return MemcachedClient.CasResult.Stored;
        }

        public override bool Delete(string key)
        {
            MemoryCache.Default.Remove(key);
            return true;
        }

        public override bool Delete(string key, uint delaySeconds)
        {
            if (delaySeconds <= 0)
            {
                MemoryCache.Default.Remove(key);
            }
            else
            {
                new Timer((s) => MemoryCache.Default.Remove(key), null, 1000 * delaySeconds, Timeout.Infinite);
            }
            return true;
        }

        public override object Get(string key)
        {
            return MemoryCache.Default.Get(key);
        }

        public override object[] Get(string[] keys)
        {
            if (keys == null || keys.Count() <= 0)
            {
                return new object[0];
            }
            List<object> rstList = new List<object>(keys.Length);
            foreach (var key in keys)
            {
                rstList.Add(MemoryCache.Default.Get(key));
            }
            return rstList.ToArray();
        }

        public override bool Set(string key, object value)
        {
            CacheItemPolicy cp = new CacheItemPolicy();
            cp.SlidingExpiration = TimeSpan.FromDays(30);
            MemoryCache.Default.Set(key, value, cp);
            return true;
        }

        public override bool Set(string key, object value, DateTime expiry)
        {
            CacheItemPolicy cp = new CacheItemPolicy();
            cp.AbsoluteExpiration = new DateTimeOffset(expiry);
            MemoryCache.Default.Set(key, value, cp);
            return true;
        }

        public override bool Set(string key, object value, TimeSpan expiry)
        {
            CacheItemPolicy cp = new CacheItemPolicy();
            cp.SlidingExpiration = expiry;
            MemoryCache.Default.Set(key, value, cp);
            return true;
        }

        private static readonly object s_SyncObjForCounter = new object();
        public override ulong? Increment(string key, ulong value)
        {
            lock (s_SyncObjForCounter)
            {
                var obj = MemoryCache.Default.Get(key);
                if (obj == null || obj.GetType() != typeof(ulong))
                {
                    return null;
                }
                ulong t = (ulong)obj + value;
                CacheItemPolicy cp = new CacheItemPolicy();
                cp.SlidingExpiration = TimeSpan.FromDays(30);
                MemoryCache.Default.Set(key, t, cp);
                return t;
            }
        }

        public override ulong? Decrement(string key, ulong value)
        {
            lock (s_SyncObjForCounter)
            {
                var obj = MemoryCache.Default.Get(key);
                if (obj == null || obj.GetType() != typeof(ulong))
                {
                    return null;
                }
                ulong t = (ulong)obj - value;
                CacheItemPolicy cp = new CacheItemPolicy();
                cp.SlidingExpiration = TimeSpan.FromDays(30);
                MemoryCache.Default.Set(key, t, cp);
                return t;
            }
        }

        public override bool Add(string key, object value)
        {
            var obj = MemoryCache.Default.Get(key);
            if (obj != null)
            {
                return false;
            }
            CacheItemPolicy cp = new CacheItemPolicy();
            cp.SlidingExpiration = TimeSpan.FromDays(30);
            MemoryCache.Default.Set(key, value, cp);
            return true;
        }
    }

    internal static class MemcachedCacheHelper
    {
        #region CacheItem

        [Serializable]
        public abstract class CacheItem
        {
            public string Key { get; private set; }

            public object Data { get; private set; }

            public string GroupName { get; private set; }

            public CacheItem(string key, object data, string groupName)
            {
                Key = key;
                Data = data;
                GroupName = groupName;
            }

            public virtual DateTime GetLocalCacheExpiry(TimeSpan defaultLocalCacheExpiry)
            {
                return DateTime.Now.Add(defaultLocalCacheExpiry);
            }
        }

        [Serializable]
        public class NeverExpiredCacheItem : CacheItem
        {
            public NeverExpiredCacheItem(string key, object data, string groupName)
                : base(key, data, groupName)
            {

            }
        }

        [Serializable]
        public class SlidingCacheItem : CacheItem
        {
            public TimeSpan SlidingExpiration { get; private set; }

            public SlidingCacheItem(string key, object data, string groupName, TimeSpan slidingExpiration)
                : base(key, data, groupName)
            {
                SlidingExpiration = slidingExpiration;
            }

            public override DateTime GetLocalCacheExpiry(TimeSpan defaultLocalCacheExpiry)
            {
                TimeSpan x = defaultLocalCacheExpiry < SlidingExpiration ? defaultLocalCacheExpiry : SlidingExpiration;
                return DateTime.Now.Add(x);
            }
        }

        [Serializable]
        public class AbsoluteCacheItem : CacheItem
        {
            public DateTime AbsoluteExpiration { get; private set; }

            public AbsoluteCacheItem(string key, object data, string groupName, DateTime absoluteExpiration)
                : base(key, data, groupName)
            {
                AbsoluteExpiration = absoluteExpiration;
            }

            public override DateTime GetLocalCacheExpiry(TimeSpan defaultLocalCacheExpiry)
            {
                DateTime x = DateTime.Now.Add(defaultLocalCacheExpiry);
                x = x < AbsoluteExpiration ? x : AbsoluteExpiration;
                return x;
            }
        }

        #endregion

        private static MemcachedClientWrapper s_Client = null;
        private static Timer s_Timer = null;
        private static readonly object s_SyncObj = new object();
        private static readonly object s_SyncObjForList = new object();
        private static readonly List<SlidingCacheItem> s_ReActiveCacheTask = new List<SlidingCacheItem>();

        public static MemcachedClientWrapper GetClientInstance()
        {
            if (s_Client == null)
            {
                lock (s_SyncObj)
                {
                    if (s_Client == null)
                    {
                        if (ConfigurationManager.AppSettings["memcached_disabled"] == "1")
                        {
                            s_Client = new DisabledMemcachedClientWrapper();
                        }
                        else
                        {
                            string serverList = TryGetConfigValueOrDefault("memcached_serverList", null);
                            if (string.IsNullOrWhiteSpace(serverList))
                            {
                                throw new ApplicationException("app.config或web.config文件中节点'memcached_serverList'不能为空，请设置缓存服务器的地址");
                            }
                            string partitionName = TryGetConfigValueOrDefault("memcached_partitionName", "ibb");
                            if (!MemcachedClient.Exists(partitionName))
                            {
                                MemcachedClient.Setup(partitionName, serverList.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                            var client = MemcachedClient.GetInstance(partitionName);
                            client.MaxPoolSize = TryGetConfigValueOrDefault("", 10);
                            client.MinPoolSize = TryGetConfigValueOrDefault("", 10);
                            client.SendReceiveTimeout = (int)TryGetConfigValueOrDefault("memcached_sendReceiveTimeout", 2000);
                            client.SocketRecycleAge = TryGetConfigValueOrDefault("memcached_socketRecycleAge", TimeSpan.Parse("00:30:00"));
                            client.KeyPrefix = TryGetConfigValueOrDefault("memcached_keyPrefix", string.Empty);
                            client.ConnectTimeout = (int)TryGetConfigValueOrDefault("memcached_connectTimeout", 2000);
                            client.CompressionThreshold = TryGetConfigValueOrDefault("memcached_compressionThreshold", 131072);
                            s_Client = new MemcachedClientWrapper(client);
                            s_Timer = new Timer(new TimerCallback(RealHitSlidingCacheItemByTimer), null, 1000, 1000);
                        }
                    }
                }
            }
            return s_Client;
        }

        private static uint TryGetConfigValueOrDefault(string nodeName, uint defaultValue)
        {
            uint s1;
            string tmp1 = ConfigurationManager.AppSettings[nodeName];
            if (string.IsNullOrWhiteSpace(tmp1) == false
                && uint.TryParse(tmp1, out s1)
                && s1 > 0)
            {
                return s1;
            }
            return defaultValue;
        }

        private static string TryGetConfigValueOrDefault(string nodeName, string defaultValue)
        {
            string tmp1 = ConfigurationManager.AppSettings[nodeName];
            if (string.IsNullOrWhiteSpace(tmp1) == false)
            {
                return tmp1.Trim();
            }
            return defaultValue;
        }

        private static TimeSpan TryGetConfigValueOrDefault(string nodeName, TimeSpan defaultValue)
        {
            TimeSpan s1;
            string tmp1 = ConfigurationManager.AppSettings[nodeName];
            if (string.IsNullOrWhiteSpace(tmp1) == false
                && TimeSpan.TryParse(tmp1, out s1)
                && s1.Ticks > 0)
            {
                return s1;
            }
            return defaultValue;
        }

        private static void HitSlidingCacheItem(SlidingCacheItem item)
        {
            if (item == null)
            {
                return;
            }
            HitSlidingCacheItem(new SlidingCacheItem[] { item });
        }

        private static void HitSlidingCacheItem(IEnumerable<SlidingCacheItem> itemList)
        {
            if (itemList == null || itemList.Count() <= 0)
            {
                return;
            }
            itemList = itemList.Where(x => x != null);
            if (itemList.Count() <= 0)
            {
                return;
            }
            lock (s_SyncObjForList)
            {
                foreach (var item in itemList)
                {
                    if (!s_ReActiveCacheTask.Exists(t => t.Key == item.Key))
                    {
                        s_ReActiveCacheTask.Add(item);
                    }
                }
            }
        }

        private static void RealHitSlidingCacheItemByTimer(object state)
        {
            List<SlidingCacheItem> taskList = null;
            if (s_ReActiveCacheTask.Count > 0)
            {
                lock (s_SyncObjForList)
                {
                    if (s_ReActiveCacheTask.Count > 0)
                    {
                        taskList = new List<SlidingCacheItem>(s_ReActiveCacheTask);
                        s_ReActiveCacheTask.Clear();
                    }
                }
            }
            if (taskList != null && taskList.Count > 0)
            {
                var client = GetClientInstance();
                foreach (var task in taskList)
                {
                    if (task != null)
                    {
                        client.Replace(task.Key.Trim(), task.Data, task.SlidingExpiration);
                    }
                }
            }
        }

        private static void AddKeyIntoGroup(string groupName, string key)
        {
            string gkey = "gk:" + groupName;
            var client = GetClientInstance();
            MemcachedClient.CasResult casRst;
            ulong unique;
            do
            {
                List<string> vlist = client.Gets(gkey, out unique) as List<string>;
                if (vlist == null)
                {
                    vlist = new List<string>(1);
                }
                if (vlist.Contains(key))
                {
                    return;
                }
                vlist.Add(key);
                if (unique == 0) // 说明还不存在，需要新增
                {
                    casRst = client.Add(gkey, vlist) ? MemcachedClient.CasResult.Stored : MemcachedClient.CasResult.NotStored;
                }
                else
                {
                    casRst = client.CheckAndSet(gkey, vlist, unique);
                }
            } while (casRst != MemcachedClient.CasResult.Stored); // 这种写法防止并发冲突
        }

        public static void RemoveKeyFromGroup(string groupName, string key)
        {
            string gkey = "gk:" + groupName;
            var client = GetClientInstance();
            MemcachedClient.CasResult casRst;
            ulong unique;
            do
            {
                List<string> vlist = client.Gets(gkey, out unique) as List<string>;
                if (vlist == null || vlist.Count <= 0)
                {
                    return;
                }
                int idx = vlist.FindIndex(x => x == key);
                if (idx <= -1)
                {
                    return;
                }
                vlist.RemoveAt(idx);
                casRst = client.CheckAndSet(gkey, vlist, unique);
            } while (casRst != MemcachedClient.CasResult.Stored); // 这种写法防止并发冲突
        }

        public static bool RemoveGroup(string groupName, bool removeKeyAndDataInGroup = false)
        {
            string gkey = "gk:" + groupName;
            var client = GetClientInstance();
            if (removeKeyAndDataInGroup)
            {
                List<string> keys = GetKeyFromGroup(groupName);
                if (keys != null && keys.Count > 0)
                {
                    foreach (var key in keys)
                    {
                        Remove(key);
                    }
                }
            }
            return client.Delete(gkey);
        }

        public static List<string> GetKeyFromGroup(string groupName)
        {
            string gkey = "gk:" + groupName;
            var client = GetClientInstance();
            List<string> vlist = client.Get(gkey) as List<string>;
            if (vlist == null)
            {
                return new List<string>(0);
            }
            return vlist;
        }

        private static bool Set(CacheItem item)
        {
            if (item == null || item.Data == null)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                throw new ApplicationException("缓存项的key不能为空");
            }
            string k = item.Key.Trim();
            if (k.Trim().StartsWith("gk:"))
            {
                throw new ApplicationException("缓存项的key不能以'gk:'开头，此开头的key为系统保留");
            }
            if (k.Trim().StartsWith("num:"))
            {
                throw new ApplicationException("缓存项的key不能以'num:'开头，此开头的key为系统保留");
            }
            if (k.Trim().StartsWith("sd:"))
            {
                throw new ApplicationException("缓存项的key不能以'sd:'开头，此开头的key为系统保留");
            }
            if (k.Trim().StartsWith("q-idx:"))
            {
                throw new ApplicationException("缓存项的key不能以'q-idx:'开头，此开头的key为系统保留");
            }
            var client = GetClientInstance();
            bool rst = false;
            int tryTimes = 0;
            do
            {
                if (tryTimes >= 3) // 最多总共执行3次
                {
                    break;
                }
                if (tryTimes > 0) // 说明是重试了
                {
                    Thread.Sleep(20);
                }
                if (item is NeverExpiredCacheItem)
                {
                    rst = client.Set(k, (NeverExpiredCacheItem)item);
                }
                else if (item is AbsoluteCacheItem)
                {
                    rst = client.Set(k, (AbsoluteCacheItem)item, ((AbsoluteCacheItem)item).AbsoluteExpiration);
                }
                else if (item is SlidingCacheItem)
                {
                    rst = client.Set(k, (SlidingCacheItem)item, ((SlidingCacheItem)item).SlidingExpiration);
                }
                tryTimes++;
            }
            while (rst == false);
            if (item is SlidingCacheItem && rst && s_ReActiveCacheTask.Exists(t => t.Key == item.Key)) // 无需背后线程再去更新了
            {
                lock (s_SyncObjForList)
                {
                    int index = s_ReActiveCacheTask.FindIndex(t => t.Key == item.Key);
                    if (index >= 0 && index < s_ReActiveCacheTask.Count)
                    {
                        s_ReActiveCacheTask.RemoveAt(index);
                    }
                }
            }
            if (rst && string.IsNullOrWhiteSpace(item.GroupName) == false)
            {
                AddKeyIntoGroup(item.GroupName, k);
            }
            return rst;
        }

        public static bool Set(string key, object value, string groupName = null)
        {
            return Set(new NeverExpiredCacheItem(key, value, groupName));
        }

        public static bool Set(string key, object value, TimeSpan slidingExpiration, string groupName = null)
        {
            return Set(new SlidingCacheItem(key, value, groupName, slidingExpiration));
        }

        public static bool Set(string key, object value, DateTime absoluteExpiration, string groupName = null)
        {
            return Set(new AbsoluteCacheItem(key, value, groupName, absoluteExpiration));
        }

        public static object Get(string key)
        {
			bool hasHit;
            return Get(key, out hasHit);
        }

        public static object Get(string key, out bool hasFound)
        {
            var client = GetClientInstance();
            var item = client.Get(key) as CacheItem;
            if (item == null)
            {
                hasFound = false;
                return null;
            }
            hasFound = true;
            HitSlidingCacheItem(item as SlidingCacheItem);
            return item.Data;
        }

        public static T Get<T>(string key)
        {
            object cache = Get(key);
            if (cache != null)
            {
                return (T)cache;
            }
            else
            {
                return default(T);
            }
        }

        public static T Get<T>(string key, out bool hasFound)
        {
            object cache = Get(key, out hasFound);
            if (cache != null)
            {
                return (T)cache;
            }
            else
            {
                return default(T);
            }
        }

        public static Dictionary<string, object> Get(string[] keys)
        {
            if (keys == null || keys.Length <= 0)
            {
                return new Dictionary<string, object>(0);
            }
            var client = GetClientInstance();
            keys = keys.Distinct().ToArray();
            object[] cachedData = client.Get(keys);
            List<CacheItem> clist = (cachedData == null || cachedData.Length <= 0) ? new List<CacheItem>(0)
                : cachedData.Select(x => x as CacheItem).Where(x => x != null).ToList();
            HitSlidingCacheItem(clist.Select(x => x as SlidingCacheItem).Where(x => x != null));
            var rstDict = new Dictionary<string, object>(clist.Count * 2);
            foreach (var item in clist)
            {
                if (item != null && string.IsNullOrWhiteSpace(item.Key) == false)
                {
                    rstDict.Add(item.Key.Trim(), item.Data);
                }
            }
            return rstDict;
        }

        public static Dictionary<string, T> Get<T>(string[] keys)
        {
            if (keys == null || keys.Length <= 0)
            {
                return new Dictionary<string, T>(0);
            }
            var client = GetClientInstance();
            keys = keys.Distinct().ToArray();
            object[] cachedData = client.Get(keys);
            List<CacheItem> clist = (cachedData == null || cachedData.Length <= 0) ? new List<CacheItem>(0)
                : cachedData.Select(x => x as CacheItem).Where(x => x != null).ToList();
            HitSlidingCacheItem(clist.Select(x => x as SlidingCacheItem).Where(x => x != null));

            var rstDict = new Dictionary<string, T>(clist.Count * 2);
            foreach (var item in clist)
            {
                if (item != null && string.IsNullOrWhiteSpace(item.Key) == false)
                {
                    rstDict.Add(item.Key.Trim(), item.Data == null ? default(T) : (T)item.Data);
                }
            }
            return rstDict;
        }

        public static Dictionary<string, object> GetByGroup(string groupName)
        {
            List<string> keys = GetKeyFromGroup(groupName);
            return Get(keys.ToArray());
        }

        public static Dictionary<string, T> GetByGroup<T>(string groupName)
        {
            List<string> keys = GetKeyFromGroup(groupName);
            return Get<T>(keys.ToArray());
        }

        public static bool Remove(string key)
        {
            var client = GetClientInstance();
            return client.Delete(key);
        }

        public static ulong ConcurrentIncrease(string gkey, long change)
        {
            var client = GetClientInstance();
            if (change == 0)
            {
                string t = client.Get(gkey) as string;
                ulong rst;
                if (string.IsNullOrWhiteSpace(t) == false && ulong.TryParse(t, out rst) && rst > 0)
                {
                    return rst;
                }
                return 0;
            }
            ulong? val;
            do
            {
                if (change > 0)
                {
                    val = client.Increment(gkey, (ulong)change);
                }
                else
                {
                    val = client.Decrement(gkey, (ulong)(0 - change));
                }
                if (val == null) // 说明还未初始化
                {
                    if (change > 0) // 这里需要增长，那么就需要进行初始化
                    {
                        if (client.Add(gkey, change.ToString(CultureInfo.InvariantCulture)))
                        {
                            val = (ulong)change;
                        }
                    }
                    else // 这里需要递减，那么未初始化的也不能减为负数，所以就不用初始化了，相当于保持为0
                    {
                        val = 0;
                    }
                }
            }
            while (val == null);
            return val.Value;
        }

        public static bool ConcurrentUpdate<T>(string gkey, MemcachedConcurrentUpdater<T> updater, out T updatedData)
        {
            if (updater == null)
            {
                throw new ArgumentNullException("updater");
            }
            var client = GetClientInstance();
            updatedData = default(T);
            int tryTimes = 0;
            MemcachedClient.CasResult casRst;
            ulong unique;
            do
            {
                if (tryTimes >= 5)
                {
                    throw new ApplicationException("已经尝试了5次，都无法访问memcached服务");
                }
                if (tryTimes >= 2)
                {
                    Thread.Sleep(50);
                }
                T originalDate;
                CacheItem tmp = client.Gets(gkey, out unique) as CacheItem;
                if (tmp == null || tmp.Data == null)
                {
                    originalDate = default(T);
                }
                else
                {
                    originalDate = (T)tmp.Data;
                }
				bool needUpdate;
                updatedData = updater(originalDate, out needUpdate);
                if (needUpdate == false)
                {
                    return false;
                }
                var itemData = new NeverExpiredCacheItem(gkey, updatedData, null);
                if (unique == 0) // 说明还不存在，需要新增
                {
                    casRst = client.Add(gkey, itemData) ? MemcachedClient.CasResult.Stored : MemcachedClient.CasResult.NotStored;
                }
                else
                {
                    casRst = client.CheckAndSet(gkey, itemData, unique); // 满足条件则更新缓存
                }
                tryTimes++;
            } while (casRst != MemcachedClient.CasResult.Stored); // 这种写法防止并发冲突
            return true;
        }

        public static ulong GenerateNewSeed(string seedKey, ulong start = 1, ulong step = 1)
        {
            if (step <= 0)
            {
                throw new ApplicationException("种子增长的步长必须大于0");
            }
            string k = "sd:" + seedKey;

            var client = GetClientInstance();
            ulong? r = null;
            do
            {
                r = client.Increment(k, step);
                if (r == null // 说明种子key还未初始化
                    && client.Add(k, start.ToString(CultureInfo.InvariantCulture))) // 进行初始为start
                {
                    r = start; // 初始化成功了
                }
            }
            while (r == null);
            return r.Value;
        }
    }

    public delegate T MemcachedConcurrentUpdater<T>(T data, out bool needUpdateToCache);
}
