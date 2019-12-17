using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    [Serializable]
    public class MemcachedQueueItem
    {
        public object Data { get; internal set; }

        public int Generation { get; internal set; }
    }

    [Serializable]
    public class MemcachedQueueItemProcessResult
    {
        public object Result { get; internal set; }

        public bool HasException { get { return Exception != null; } }

        public Exception Exception { get; internal set; }
    }

    [Serializable]
    public class MemcachedQueueEnqueueResult
    {
        internal MemcachedQueueEnqueueResult(string queueName, bool success, ulong messageItemIndex)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            EnqueueSuccess = success;
            MessageItemIndex = messageItemIndex;
            m_QueueName = queueName.Trim();
        }

        private string m_QueueName;

        public bool EnqueueSuccess { get; private set; }

        public ulong MessageItemIndex { get; private set; }

        public MemcachedQueueItemProcessResult SyncGetMessageProcessResult(int timeoutMilliseconds = 10000)
        {
            if (EnqueueSuccess == false || MessageItemIndex <= 0)
            {
                return null;
            }
            var queueId = MemcachedQueue.GetQueueId(m_QueueName);
            if (string.IsNullOrWhiteSpace(queueId))
            {
                return null;
            }
            string itemKey = MemcachedQueue.BuildItemIndexKeyForProcessResult(queueId, (ulong)MessageItemIndex);
            var client = MemcachedCacheHelper.GetClientInstance();
            int wait = 0;
            while (wait < timeoutMilliseconds)
            {
                var r = client.Get(itemKey) as MemcachedQueueItemProcessResult;
                if (r != null)
                {
                    return r;
                }
                Thread.Sleep(100);
                wait = wait + 100;
            }
            return null;
        }
    }

    [Serializable]
    public enum MemcachedQueueIndexType
    {
        Enqueue = 0,

        Dequeue = 1,

        ClearGarbage = 2
    }

    public delegate T MemcachedQueueItemHandler<T>(long messageItemIndex, MemcachedQueueItem item);

    public delegate void MemcachedQueueItemHandler(long messageItemIndex, MemcachedQueueItem item);

    public static class MemcachedQueue
    {
        private const string M_QUEUE_REGISTRY_KEY = "q-idx:registry";
        private const int QUEUE_ID_LOCAL_CACHE_EXPIRY_MIN = 5;
        private const ulong DEFAULT_QUEUE_SIZE = 100000; // 10w
        private static Timer s_GarbageCleanerTimer = null;
        private static readonly object s_SyncForTimer = new object();

        private static Dictionary<string, ThreadTask> s_ProcessTimerDicts = new Dictionary<string, ThreadTask>();
        private static readonly object s_SyncProcessTimerDicts = new object();

        internal static bool CheckSerializable(Type type)
        {
            if (type == typeof(byte[])
                || type == typeof(string)
                || type == typeof(DateTime)
                || type == typeof(bool)
                || type == typeof(byte)
                || type == typeof(short)
                || type == typeof(ushort)
                || type == typeof(int)
                || type == typeof(uint)
                || type == typeof(long)
                || type == typeof(ulong)
                || type == typeof(float)
                || type == typeof(double))
            {
                return true;
            }
            else
            {
                var t = type.GetCustomAttributes(typeof(SerializableAttribute), false);
                return t != null && t.Length > 0;
            }
        }

        private static string BuildLocalCacheKeyOfQueueId(string queueName)
        {
            return string.Format("MemQ.Id|{0}", queueName);
        }

        private static string BuildItemIndexKey(string queueId, ulong index)
        {
            return string.Format("q-idx:{0}-{1}", queueId, index % DEFAULT_QUEUE_SIZE);
        }

        internal static string BuildItemIndexKeyForProcessResult(string queueId, ulong index)
        {
            return string.Format("q-idx:r-{0}-{1}", queueId, index % DEFAULT_QUEUE_SIZE);
        }

        private static string BuildIndexKey(string queueId, MemcachedQueueIndexType indexType)
        {
            if (indexType == MemcachedQueueIndexType.Enqueue)
            {
                return string.Format("q-idx:{0}-en", queueId);
            }
            else if (indexType == MemcachedQueueIndexType.Dequeue)
            {
                return string.Format("q-idx:{0}-de", queueId);
            }
            else  // if (indexType == MemcachedQueueIndexType.ClearGarbage)
            {
                return string.Format("q-idx:{0}-gc", queueId);
            }
        }

        private static ulong GetIndexById(string queueId, MemcachedQueueIndexType indexType)
        {
            if (string.IsNullOrWhiteSpace(queueId)) // 队列列表里未找到指定名称的队列
            {
                return 0;
            }
            string indexKey = BuildIndexKey(queueId, indexType);
            var client = MemcachedCacheHelper.GetClientInstance();
            string index_string = client.Get(indexKey) as string;
            ulong index;
            if (string.IsNullOrWhiteSpace(index_string)
                || index_string == "0"
                || ulong.TryParse(index_string, out index) == false
                || index <= 0)
            {
                return 0;
            }
            return index;
        }

        private static ulong[] GetIndexById(string queueId, params MemcachedQueueIndexType[] indexTypes)
        {
            if (string.IsNullOrWhiteSpace(queueId)) // 队列列表里未找到指定名称的队列
            {
                return new ulong[indexTypes.Length];
            }
            string[] indexKeys = new string[indexTypes.Length];
            for (int i = 0; i < indexTypes.Length; i++)
            {
                indexKeys[i] = BuildIndexKey(queueId, indexTypes[i]);
            }
            var client = MemcachedCacheHelper.GetClientInstance();
            object[] rstArray = client.Get(indexKeys);
            if (rstArray == null || rstArray.Length <= 0)
            {
                return new ulong[0];
            }
            ulong[] rstList = new ulong[rstArray.Length];
            for (int i = 0; i < rstArray.Length; i++)
            {
                string index_string = rstArray[i] as string;
                ulong index;
                if (string.IsNullOrWhiteSpace(index_string)
                    || index_string == "0"
                    || ulong.TryParse(index_string, out index) == false
                    || index <= 0)
                {
                    rstList[i] = 0;
                }
                else
                {
                    rstList[i] = index;
                }
            }
            return rstList;
        }

        // 此方法不用考虑线程安全
        private static void ClearGarbageItemInQueue(string queueId)
        {
            var ary = GetIndexById(queueId, MemcachedQueueIndexType.Dequeue, MemcachedQueueIndexType.ClearGarbage);
            ulong de_index = ary == null || ary.Length <= 0 ? 0 : ary[0];
            ulong gc_index = ary == null || ary.Length <= 1 ? 0 : ary[1];
            if (gc_index >= de_index - 1) // 垃圾回收到的地方，已经是最近一次读取的地方的前一个地方了，说明垃圾已经回收完了
            {
                return;
            }
            var client = MemcachedCacheHelper.GetClientInstance();
            for (var i = gc_index + 1; i < de_index; i++) // 不能等于de_index，这样确保当前最近处理的item数据还在
            {
                string itemKey = BuildItemIndexKey(queueId, i);
                //string resultKey = BuildItemIndexKeyForProcessResult(queueId, i);
                client.Delete(itemKey);
            }
            client.Set(BuildIndexKey(queueId, MemcachedQueueIndexType.ClearGarbage), de_index.ToString(CultureInfo.InvariantCulture));
        }

        public static void GarbageClean()
        {
            var allQueues = GetAllQueues();
            if (allQueues == null || allQueues.Count <= 0) // 还没有任何队列（队列列表为空）
            {
                return;
            }
            foreach (var entry in allQueues)
            {
                ClearGarbageItemInQueue(entry.Value);
            }
        }

        public static void StartAutoGarbageCleaner()
        {
            if (s_GarbageCleanerTimer == null)
            {
                lock (s_SyncForTimer)
                {
                    if (s_GarbageCleanerTimer == null)
                    {
                        s_GarbageCleanerTimer = new Timer((s) => GarbageClean(), null, 5000, 10 * 60 * 1000); // 每10分钟回收一次
                    }
                }
            }
        }

        public static Dictionary<string, string> GetAllQueues()
        {
            var client = MemcachedCacheHelper.GetClientInstance();
            var rst = client.Get(M_QUEUE_REGISTRY_KEY) as Dictionary<string, int>;
            if (rst == null)
            {
                return null;
            }
            Dictionary<string, string> dicts = new Dictionary<string, string>(rst.Count * 2);
            foreach (var entry in rst)
            {
                dicts.Add(entry.Key, entry.Value.ToString(CultureInfo.InvariantCulture));
            }
            return dicts;
        }

        public static ulong GetIndex(string queueName, MemcachedQueueIndexType indexType)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.Trim();
            return GetIndexById(GetQueueId(queueName), indexType);
        }

        public static ulong[] GetIndex(string queueName, params MemcachedQueueIndexType[] indexTypes)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            if (indexTypes == null || indexTypes.Length <= 0)
            {
                throw new ArgumentNullException("indexTypes");
            }
            queueName = queueName.Trim();
            return GetIndexById(GetQueueId(queueName), indexTypes);
        }

        public static bool IsEmpty(string queueName)
        {
            return GetCount(queueName) <= 0;
        }

        public static ulong GetCount(string queueName)
        {
            var ary = GetIndex(queueName, MemcachedQueueIndexType.Dequeue, MemcachedQueueIndexType.Enqueue);
            ulong de_index = ary == null || ary.Length <= 0 ? 0 : ary[0];
            ulong en_index = ary == null || ary.Length <= 1 ? 0 : ary[1];
            return en_index <= de_index ? 0 : en_index - de_index;
        }

        public static MemcachedQueueItem[] GetAllItems(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.Trim();
            string queueId = GetQueueId(queueName);
            if (string.IsNullOrWhiteSpace(queueId)) // 队列列表里未找到指定名称的队列
            {
                return new MemcachedQueueItem[0];
            }
            var ary = GetIndexById(queueId, MemcachedQueueIndexType.Dequeue, MemcachedQueueIndexType.Enqueue);
            ulong de_index = ary == null || ary.Length <= 0 ? 0 : ary[0];
            ulong en_index = ary == null || ary.Length <= 1 ? 0 : ary[1];
            if (en_index <= de_index) // 说明队列是空的
            {
                return new MemcachedQueueItem[0];
            }
            ulong len = en_index - de_index;
            MemcachedQueueItem[] rstList = new MemcachedQueueItem[len];
            const ulong maxBatchSize = 100;
            ulong itemIndex = de_index;
            int index = 0;
            var client = MemcachedCacheHelper.GetClientInstance();
            while (itemIndex < en_index)
            {
                ulong leftItemCount = en_index - itemIndex;
                uint batchSize = (uint)(leftItemCount < maxBatchSize ? leftItemCount : maxBatchSize);
                string[] keys = new string[batchSize];
                for (uint i = 0; i < batchSize; i++)
                {
                    itemIndex++;
                    keys[i] = BuildItemIndexKey(queueId, itemIndex);
                }
                var objs = client.Get(keys);
                for (uint i = 0; i < batchSize; i++)
                {
                    var obj = (objs == null || i >= objs.Length) ? null : objs[i];
                    rstList[index] = obj as MemcachedQueueItem;
                    index++;
                }
            }
            return rstList;
        }

        public static MemcachedQueueItem GetItem(string queueName, long index)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            if (index <= 0)
            {
                return null;
            }
            queueName = queueName.Trim();
            var queueId = GetQueueId(queueName);
            if (string.IsNullOrWhiteSpace(queueId))
            {
                return null;
            }
            string itemKey = BuildItemIndexKey(queueId, (ulong)index);
            var client = MemcachedCacheHelper.GetClientInstance();
            return client.Get(itemKey) as MemcachedQueueItem;
        }

        public static MemcachedQueueItem[] GetItem(string queueName, long[] indexs)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            if (indexs == null || indexs.Length <= 0)
            {
                return new MemcachedQueueItem[0];
            }
            queueName = queueName.Trim();
            var queueId = GetQueueId(queueName);
            if (string.IsNullOrWhiteSpace(queueId))
            {
                return new MemcachedQueueItem[indexs.Length];
            }
            string[] itemKeys = new string[indexs.Length];
            for (var i = 0; i < indexs.Length; i++)
            {
                itemKeys[i] = BuildItemIndexKey(queueId, indexs[i] <= 0 ? 0 : (ulong)indexs[i]);
            }
            var client = MemcachedCacheHelper.GetClientInstance();
            var list = client.Get(itemKeys);
            if (list == null || list.Length <= 0)
            {
                return new MemcachedQueueItem[indexs.Length];
            }

            MemcachedQueueItem[] rstList = new MemcachedQueueItem[indexs.Length];
            for (uint i = 0; i < indexs.Length; i++)
            {
                var obj = (list == null || i >= list.Length) ? null : list[i];
                rstList[i] = obj as MemcachedQueueItem;
            }
            return rstList;
        }

        public static MemcachedQueueItem GetItemWithProcessResult(string queueName, long index, out MemcachedQueueItemProcessResult processResult)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            if (index <= 0)
            {
                processResult = null;
                return null;
            }
            queueName = queueName.Trim();
            var queueId = GetQueueId(queueName);
            if (string.IsNullOrWhiteSpace(queueId))
            {
                processResult = null;
                return null;
            }
            string itemKey = BuildItemIndexKey(queueId, (ulong)index);
            string itemProcessdResultKey = BuildItemIndexKeyForProcessResult(queueId, (ulong)index);
            var client = MemcachedCacheHelper.GetClientInstance();
            var objs = client.Get(new string[] { itemKey, itemProcessdResultKey });
            MemcachedQueueItem item = null;
            processResult = null;
            if (objs != null)
            {
                if (objs.Length > 0)
                {
                    item = objs[0] as MemcachedQueueItem;
                }
                if (objs.Length > 1)
                {
                    processResult = objs[1] as MemcachedQueueItemProcessResult;
                }
            }
            return item;
        }

        public static string GetOrCreateQueueId(string queueName, bool notReadFromLocalCache = false)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.Trim();
            string localCacheKey = BuildLocalCacheKeyOfQueueId(queueName);
            Func<string> getter = () =>
            {
                var client = MemcachedCacheHelper.GetClientInstance();
                MemcachedClient.CasResult casRst;
                ulong unique;
                int queueId;
                string enKey = null;
                do
                {
                    Dictionary<string, int> registryDicts = client.Gets(M_QUEUE_REGISTRY_KEY, out unique) as Dictionary<string, int>; // 获取队列注册表
                    if (registryDicts != null && registryDicts.Count > 0
                        && registryDicts.TryGetValue(queueName, out queueId))
                    {
                        // 说明队列注册表里找到了此queueName的队列，获取到queue的Id（可能是之前注册过的，或者并发的其它线程先注册到了）
                        break;
                    }
                    // 注册表里没找到，则准备为此queueName的队列生成一个新的唯一Id，注册到注册表里
                    queueId = (registryDicts == null || registryDicts.Count <= 0) ? 1 : registryDicts.Values.Max() + 1;
                    if (registryDicts == null)
                    {
                        registryDicts = new Dictionary<string, int>
                        {
                            { queueName, queueId }
                        };
                    }
                    else
                    {
                        if (registryDicts.ContainsKey(queueName))
                        {
                            registryDicts[queueName] = queueId;
                        }
                        else
                        {
                            registryDicts.Add(queueName, queueId);
                        }
                    }
                    enKey = BuildIndexKey(queueId.ToString(CultureInfo.InvariantCulture), MemcachedQueueIndexType.Enqueue);
                    client.Add(enKey, "0");
                    if (unique == 0) // 说明还不存在，需要新增
                    {
                        casRst = client.Add(M_QUEUE_REGISTRY_KEY, registryDicts) ? MemcachedClient.CasResult.Stored : MemcachedClient.CasResult.NotStored;
                    }
                    else
                    {
                        casRst = client.CheckAndSet(M_QUEUE_REGISTRY_KEY, registryDicts, unique); // 满足条件则更新缓存
                    }
                } while (casRst != MemcachedClient.CasResult.Stored); // 这种写法防止并发冲突
                return queueId.ToString(CultureInfo.InvariantCulture);
            };
            if (notReadFromLocalCache)
            {
                string id = getter();
                if (string.IsNullOrWhiteSpace(id) == false)
                {
                    Cache.SetLocalCache(localCacheKey, id, true, QUEUE_ID_LOCAL_CACHE_EXPIRY_MIN);
                }
                return id;
            }
            return Cache.GetWithLocalCache(localCacheKey, getter, true, QUEUE_ID_LOCAL_CACHE_EXPIRY_MIN);
        }

        public static string GetQueueId(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.Trim();
            string localCacheKey = BuildLocalCacheKeyOfQueueId(queueName);
            string queueId = Cache.GetLocalCache(localCacheKey) as string;
            if (string.IsNullOrWhiteSpace(queueId) == false)
            {
                return queueId;
            }
            var allQueues = GetAllQueues();
            if (allQueues == null || allQueues.Count <= 0) // 还没有任何队列（队列列表为空）
            {
                return null;
            }
            if (allQueues.TryGetValue(queueName, out queueId) == false
                || string.IsNullOrWhiteSpace(queueId)) // 队列列表里未找到指定名称的队列
            {
                return null;
            }
            Cache.SetLocalCache(localCacheKey, queueId, true, QUEUE_ID_LOCAL_CACHE_EXPIRY_MIN);
            return queueId;
        }

        public static MemcachedQueueEnqueueResult Enqueue(string queueName, object data, int generation = 0, uint maxRetryTimes = 2)
        {
            if (maxRetryTimes < 0)
            {
                maxRetryTimes = 0;
            }
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.Trim();
            string localCacheKey = "MemQ.Id|" + queueName;
            bool rst = false;
            var client = MemcachedCacheHelper.GetClientInstance();
            ulong? index;
            string queueId;
            bool notReadFromLocalCache = false;
            do
            {
                queueId = GetOrCreateQueueId(queueName, notReadFromLocalCache);
                index = client.Increment(BuildIndexKey(queueId, MemcachedQueueIndexType.Enqueue), 1);
                if (index == null) // 有可能本地缓存读取的id已经过期试下了，需要在下次循环执行时，不走本地缓存重新获取
                {
                    notReadFromLocalCache = true;
                }
            }
            while (index == null);
            // 程序执行到这里，EnqueueIndex已经被增长上去了，但此index处的数据还未Set写入，那么此时并发的Dequeue操作则有可能读取到此index的，但获取数据是获取不到的
            // 所以在Dequeue方法里有针对这种极端并发的处理代码，结合这里的代码注释就容易理解了
            string itemKey = BuildItemIndexKey(queueId, index.Value);
            var item = new MemcachedQueueItem { Data = data, Generation = generation };
            rst = client.Set(itemKey, item); // 程序执行到这里才真正开始将此index的数据写入
            int retryTime = 0;
            while (rst == false) // 如果写入失败，则重试
            {
                if (retryTime >= maxRetryTimes) // 重试次数达到最大重试次数，则不再重试
                {
                    break;
                }
                Thread.Sleep(10); // 等待10ms再试
                rst = client.Set(itemKey, item);
                retryTime++;
            }
            return new MemcachedQueueEnqueueResult(queueName, rst, index.Value);
        }

        public static bool Dequeue(string queueName, MemcachedQueueItemHandler handler, uint maxRetryTimes = 2)
        {
            return Dequeue<object>(queueName, (idx, item) =>
            {
                handler(idx, item);
                return null;
            }, maxRetryTimes);
        }

        public static bool Dequeue<T>(string queueName, MemcachedQueueItemHandler<T> handler, uint maxRetryTimes = 2)
        {
            if (maxRetryTimes < 0)
            {
                maxRetryTimes = 0;
            }
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            if (CheckSerializable(typeof(T)) == false)
            {
                throw new NotSupportedException("不支持handler消息处理返回结果的类型\"" + typeof(T).FullName + "\"，因为此类型不支持二进制序列化，如果要使用请在此类型的定义处添加[Serializable]的Attribute");
            }

            queueName = queueName.Trim();
            string queueId = GetQueueId(queueName);
            if (string.IsNullOrWhiteSpace(queueId)) // 队列列表里未找到指定名称的队列
            {
                //handler(-1, null);
                return false;
            }
            ulong en_index = GetIndexById(queueId, MemcachedQueueIndexType.Enqueue);
            if (en_index <= 0) // 还从未写入过，那么肯定读取不到数据了
            {
                //handler(-1, null);
                return false;
            }
            var client = MemcachedCacheHelper.GetClientInstance();
            MemcachedClient.CasResult casRst;
            ulong unique;
            string k1 = BuildIndexKey(queueId, MemcachedQueueIndexType.Dequeue);
            ulong de_index;
            MemcachedQueueItem item;
            do
            {
                string de_index_string = client.Gets(k1, out unique) as string; // 读取上次出队列的index
                if (string.IsNullOrWhiteSpace(de_index_string)
                    || de_index_string == "0"
                    || ulong.TryParse(de_index_string, out de_index) == false
                    || de_index <= 0)
                {
                    // 说明之前还从未出过队列
                    de_index = 0;
                }
                de_index++; // 计算出本次应该出队列的item的index
                if (de_index > en_index) // 如果已经超过队列最近入的index了，说明队列已经读空了（所有item都读取过了）
                {
                    //handler(-1, null);
                    return false;
                }
                string itemKey = BuildItemIndexKey(queueId, de_index);
                item = client.Get(itemKey) as MemcachedQueueItem;
                // 这里可能遇到一种极端的读写并发情况，即在Enqueue方法的地方先将EnqueueIndex增长上去了，但此index处的元素数据还未来得及Set写
                // 然后本Dequeue方法就先拿到此index，然后Get数据，结果因为Set还未来得及写入所以获取数据未空
                // 所以需要等待一会儿再试（期望这段等待时间内写入线程能完成此index处的数据的写入）
                int reTryTimes = 0;
                while (item == null)
                {
                    Thread.Sleep(10); // 等待10ms再试着获取此index处的数据
                    item = client.Get(itemKey) as MemcachedQueueItem;
                    reTryTimes++;
                    if (reTryTimes >= maxRetryTimes) // 如果重试达到maxRetryTimes次（累计maxRetryTimes * 10ms了），那么基本上证明不是并发还未写入了，因为并发写入问题不可能等这么久还未写入进去，基本上确定是遇到网络问题或脏数据，或者memcached服务挂了
                    {
                        break;
                    }
                    // 基本上Dequeue操作都是单线程慢慢处理，所以对执行时间要求不是那么高，500ms的延迟等待可以接受（并且是要遇到item==null的很极端的读写并发，或者是遇到了脏数据，发生的几率都很小）
                }
                if (unique == 0) // 说明还不存在，需要新增
                {
                    casRst = client.Add(k1, de_index.ToString(CultureInfo.InvariantCulture)) ? MemcachedClient.CasResult.Stored : MemcachedClient.CasResult.NotStored;
                }
                else
                {
                    casRst = client.CheckAndSet(k1, de_index.ToString(CultureInfo.InvariantCulture), unique); // 乐观锁，满足条件（没有人优先改动过）则更新缓存，如果更新成功则锁定了de_index这个索引值，其它线程无法再获取到这个index的item了
                }
            } while
            (
                casRst != MemcachedClient.CasResult.Stored // 乐观锁，这种写法防止并发冲突
                || item == null // 发生item == null这种情况可能是脏数据了，则继续找下一个索引处的元素
            );
            if (item == null) // 说明没有找到有效数据
            {
                //handler(-1, null);
                return false;
            }

            string itemProcessdResultKey = BuildItemIndexKeyForProcessResult(queueId, de_index);
            T rst = default(T);
            Exception ex = null;
            try
            {
                rst = handler((long)de_index, item);
                ex = null;
            }
            catch (Exception ex1)
            {
                rst = default(T);
                ex = ex1;
                throw;
            }
            finally
            {
                MemcachedQueueItemProcessResult pr = new MemcachedQueueItemProcessResult { Exception = ex, Result = rst };
                client.Set(itemProcessdResultKey, pr, DateTime.Now.AddMinutes(120));
            }
            return true;
        }

        public static void StartToProcess(string queueName, MemcachedQueueItemHandler handler, int checkQueueIntervalMilliseconds = 5000)
        {
            StartToProcess<object>(queueName, (idx, item)=> 
            {
                handler(idx, item);
                return null;
            }, checkQueueIntervalMilliseconds);
        }

        public static void StartToProcess<T>(string queueName, MemcachedQueueItemHandler<T> handler, int checkQueueIntervalMilliseconds = 5000)
        {
			ThreadTask task;
            if (s_ProcessTimerDicts.TryGetValue(queueName, out task) == false)
            {
                lock (s_SyncProcessTimerDicts)
                {
                    if (s_ProcessTimerDicts.TryGetValue(queueName, out task) == false)
                    {
                        task = new ThreadTask((q, m) =>
                        {
                            while (true)
                            {
                                if (s_ProcessTimerDicts.ContainsKey(q) == false)
                                {
                                    return;
                                }
                                bool hasItem;
                                do
                                {
                                    try
                                    {
                                        hasItem = Dequeue(q, handler);
                                    }
                                    catch(Exception ex)
                                    {
                                        hasItem = true;
                                        Logger.Error(ex);
                                    }
                                }
                                while (hasItem); // 如果队列里有数据，则一直不断取数据出来处理，直到队列为空
                                // 队列取空后，休息m毫秒再尝试从队列取数据
                                Thread.Sleep(m);
                            }
                        });
                        s_ProcessTimerDicts.Add(queueName, task);
                        task.AsyncResult = task.Action.BeginInvoke(queueName, checkQueueIntervalMilliseconds, null, null);
                    }
                }
            }
        }

        public static void StopToProcess(string queueName)
        {
			ThreadTask task;
            if (s_ProcessTimerDicts.TryGetValue(queueName, out task))
            {
                lock (s_SyncProcessTimerDicts)
                {
                    if (s_ProcessTimerDicts.TryGetValue(queueName, out task))
                    {
                        s_ProcessTimerDicts.Remove(queueName);
                        task.Action.EndInvoke(task.AsyncResult);
                    }
                }
            }
        }
    }

    internal class ThreadTask
    {
        public ThreadTask(Action<string, int> action)
        {
            Action = action;
        }

        public Action<string, int> Action { get; private set; }

        public IAsyncResult AsyncResult { get; set; }
    }
}
