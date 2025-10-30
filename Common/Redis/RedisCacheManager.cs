using Common.Models;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.Redis
{
    /// <summary>
    /// Redis
    /// </summary>
    public partial class RedisCacheManager : IRedisCacheManager, IDisposable
    {
        /// <summary>
        /// 默认key
        /// </summary>
        private const string RedisDataKey = "MyRedis";

        /// <summary>
        /// 连接字符串 
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// 实例名称
        /// </summary>
        private readonly string _instanceName;

        /// <summary>
        ///  默认数据库
        /// </summary>
        private readonly int _defaultDB;

        /// <summary>
        /// Redis连接复用器缓存（线程安全）
        /// </summary>
        private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections = new();

        /// <summary>
        /// 直接获取 Redis 数据库操作对象
        /// </summary>
        private readonly IDatabase _db;

        /// <summary>
        /// 释放标志
        /// </summary>
        private bool _disposed = false;

        public RedisCacheManager(string connectionString, string instanceName, int defaultDB = 0)
        {
            _connectionString = connectionString;
            _instanceName = instanceName;
            _defaultDB = defaultDB;

            // 初始化数据库,访问Redis数据库
            _db = GetConnect().GetDatabase(_defaultDB);
        }

        /// <summary>
        /// 获取ConnectionMultiplexer
        /// </summary>
        /// <returns></returns>
        private ConnectionMultiplexer GetConnect()
        {
            return _connections.GetOrAdd(_instanceName, p => ConnectionMultiplexer.Connect(_connectionString));
        }

        private string DataKey(string key)
        {
            return string.Format("{0}:{1}", RedisDataKey, key);
        }

        #region String类型
        /// <summary>
        /// 获取缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string StrGet(string key)
        {
            return _db.StringGet(DataKey(key));
        }

        /// <summary>
        /// 获取缓存值(使用该异步方法时需要在获取结果后面.Result)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<string> StrGetAsync(string key)
        {
            return await _db.StringGetAsync(DataKey(key));
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        public bool StrSet(string key, string value, TimeSpan? cacheTime = null)
        {
            var result = _db.StringSet(DataKey(key), value);
            this.KeyAddExpire(key, cacheTime);
            return result;
        }

        /// <summary>
        /// 保存(使用该异步方法时需要在后面使用.Wait())
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        public async Task<bool> StrSetAsync(string key, string value, TimeSpan? cacheTime = null)
        {
            var result = await _db.StringSetAsync(DataKey(key), value);
            await this.KeyAddExpireAsync(key, cacheTime);
            return result;
        }

        /// <summary>
        /// 判断key是否存在，不存在就创建
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        public bool StrSetNx(string key, string value, TimeSpan? cacheTime = null)
        {
            return _db.StringSet(DataKey(key), value, cacheTime, When.NotExists, CommandFlags.None); ;
        }

        /// <summary>
        /// 自增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long StrIncr(string key, long value = 1)
        {
            return _db.StringIncrement(key, value);
        }

        /// <summary>
        /// 自减
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public long StrDecr(string key, long value = 1)
        {
            return _db.StringDecrement(key, value);
        }

        /// <summary>
        /// 批量设置键值对
        /// </summary>
        /// <param name="keyValues"></param>
        /// <returns></returns>
        public bool StrBatch(Dictionary<string, string> keyValues)
        {
            var redisKeyValues = keyValues.Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value)).ToArray();
            return _db.StringSet(redisKeyValues);
        }

        /// <summary>
        /// 批量获取值
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetBatch(IEnumerable<string> keys)
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            RedisValue[] values = _db.StringGet(redisKeys);

            var result = new Dictionary<string, string>();
            int i = 0;
            foreach (var key in keys)
            {
                result[key] = values[i];
                i++;
            }
            return result;
        }
        #endregion

        #region Hash类型
        /// <summary>
        /// 设置单个字段
        /// </summary>
        public async Task HashSetAsync<T>(string key, string field, T value)
        {
            string json = JsonConvert.SerializeObject(value);
            await _db.HashSetAsync(DataKey(key), field, json);
        }

        /// <summary>
        /// 设置多个字段
        /// </summary>
        public async Task HashSetAsync<T>(string key, Dictionary<string, T> values)
        {
            var entries = values.Select(x => new HashEntry(x.Key, JsonConvert.SerializeObject(x.Value))).ToArray();
            await _db.HashSetAsync(DataKey(key), entries);
        }

        /// <summary>
        /// 获取单个字段
        /// </summary>
        public async Task<T> HashGetAsync<T>(string key, string field)
        {
            var value = await _db.HashGetAsync(DataKey(key), field);
            if (value.IsNullOrEmpty) return default;
            return JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// 获取整个哈希对象
        /// </summary>
        public async Task<Dictionary<string, T>> HashGetAllAsync<T>(string key)
        {
            var entries = await _db.HashGetAllAsync(DataKey(key));
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => JsonConvert.DeserializeObject<T>(e.Value) ?? throw new Exception($"反序列化字段 {e.Name} 失败")
            );
        }

        /// <summary>
        /// 删除一个或多个字段
        /// </summary>
        public async Task<bool> HashDeleteAsync(string key, params string[] fields)
        {
            RedisValue[] values = fields.Select(f => (RedisValue)f).ToArray();
            long count = await _db.HashDeleteAsync(DataKey(key), values);
            return count > 0;
        }

        /// <summary>
        /// 判断字段是否存在
        /// </summary>
        public async Task<bool> HashExistsAsync(string key, string field)
        {
            return await _db.HashExistsAsync(DataKey(key), field);
        }

        /// <summary>
        /// 获取字段数量
        /// </summary>
        public async Task<long> HashLengthAsync(string key)
        {
            return await _db.HashLengthAsync(DataKey(key));
        }

        /// <summary>
        /// 数值字段自增
        /// </summary>
        public async Task<double> HashIncrementAsync(string key, string field, double value = 1)
        {
            return await _db.HashIncrementAsync(DataKey(key), field, value);
        }

        /// <summary>
        /// 数值字段自减
        /// </summary>
        public async Task<double> HashDecrementAsync(string key, string field, double value = 1)
        {
            return await _db.HashDecrementAsync(DataKey(key), field, value);
        }

        /// <summary>
        /// 扫描哈希内容（可选匹配模式）
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> HashScan(string key, string pattern = "*")
        {
            foreach (var entry in _db.HashScan(DataKey(key), pattern))
            {
                yield return new KeyValuePair<string, string>(entry.Name, entry.Value);
            }
        }
        #endregion

        #region List集合
        /// <summary>
        /// 左入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public long ListLeftPush<T>(string key, T item)
        {
            return _db.ListLeftPush(DataKey(key), JsonConvert.SerializeObject(item));
        }

        /// <summary>
        /// 右入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public long ListRightPush<T>(string key, T item)
        {
            return _db.ListRightPush(DataKey(key), JsonConvert.SerializeObject(item));
        }

        /// <summary>
        /// 左批量入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public long ListLeftPushBatch<T>(string key, IEnumerable<T> items)
        {
            var redisValues = new List<RedisValue>();
            foreach (var item in items)
                redisValues.Add(JsonConvert.SerializeObject(item));

            return _db.ListLeftPush(DataKey(key), redisValues.ToArray());
        }

        /// <summary>
        /// 右批量入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public long ListRightPushBatch<T>(string key, IEnumerable<T> items)
        {
            var redisValues = new List<RedisValue>();
            foreach (var item in items)
                redisValues.Add(JsonConvert.SerializeObject(item));

            return _db.ListRightPush(DataKey(key), redisValues.ToArray());
        }

        /// <summary>
        /// 左出队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T ListLeftPop<T>(string key)
        {
            var value = _db.ListLeftPop(DataKey(key));
            return JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// 右出队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T ListRightPop<T>(string key)
        {
            var value = _db.ListRightPop(DataKey(key));
            return JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long ListLength(string key)
        {
            return _db.ListLength(DataKey(key));
        }

        /// <summary>
        /// 获取范围
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public List<T> ListRange<T>(string key, long start = 0, long stop = -1)
        {
            var values = _db.ListRange(DataKey(key), start, stop);
            var result = new List<T>();
            foreach (var value in values)
                result.Add(JsonConvert.DeserializeObject<T>(value));

            return result;
        }

        /// <summary>
        /// 删除指定值，count=0 删除所有
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public long ListRemove<T>(string key, T value, long count = 0)
        {
            return _db.ListRemove(DataKey(key), JsonConvert.SerializeObject(value), count);
        }

        #endregion

        #region Set集合

        /// <summary>
        /// 判断元素是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> SetContainsAsync(string key, string value)
        {
            return await _db.SetContainsAsync(DataKey(key), value);
        }

        /// <summary>
        /// 获取所有成员
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public HashSet<string> SetGetAll(string key)
        {
            var values = _db.SetMembers(key);
            return new HashSet<string>(values.Select(v => (string)v));
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        public async Task<bool> SetAddAsync(string key, string value, TimeSpan? cacheTime = null)
        {
            var result = await _db.SetAddAsync(DataKey(key), value);
            await this.KeyAddExpireAsync(key, cacheTime);
            return result;
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <param name="expiry"></param>
        public async Task<long> SetAddBatchAsync(string key, IEnumerable<string> values, TimeSpan? cacheTime = null)
        {
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            var result = await _db.SetAddAsync(DataKey(key), redisValues);
            await this.KeyAddExpireAsync(key, cacheTime);
            return result;

        }
        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> SetRemoveAsync(string key, string value)
        {
            return await _db.SetRemoveAsync(DataKey(key), value);
        }

        /// <summary>
        /// 求并集、交集、差集
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public async Task<RedisValue[]> SetCombineAsync(int num, string first, string second)
        {
            // SetOperation.Union 并集
            // SetOperation.Intersect 交集
            // SetOperation.Difference 差集
            RedisValue[] result;
            switch (num)
            {
                case 1:
                    result = await _db.SetCombineAsync(SetOperation.Intersect, DataKey(first), DataKey(second));
                    break;
                case 2:
                    result = await _db.SetCombineAsync(SetOperation.Difference, DataKey(first), DataKey(second));
                    break;
                default:
                    result = await _db.SetCombineAsync(SetOperation.Union, DataKey(first), DataKey(second));
                    break;
            }
            return result;
        }
        #endregion

        #region SortedSet
        /// <summary>
        /// SortedSet是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public async Task<double?> SortedSetScoreAsync(string key, string member)
        {
            return await _db.SortedSetScoreAsync(DataKey(key), member);
        }
        /// <summary>
        /// 获取范围数据，索引0开始
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public async Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(string key, long start, long stop)
        {
            return await _db.SortedSetRangeByRankWithScoresAsync(DataKey(key), start, stop);
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetAddAsync(string key, string member, double score)
        {
            return await _db.SortedSetAddAsync(DataKey(key), member, score);
        }

        /// <summary>
        /// 高性能批量添加 SortedSet 元素
        /// </summary>
        /// <param name="key">SortedSet key</param>
        /// <param name="entries">要添加的元素集合</param>
        public async Task SortedSetAddBatchAsync(string key, IEnumerable<SortedSetEntry> entries)
        {
            // 每批处理的元素数量
            var batchSize = 1000; 
            var batchList = new List<SortedSetEntry>(batchSize);
            foreach (var entry in entries)
            {
                batchList.Add(entry);

                if (batchList.Count >= batchSize)
                {
                    await _db.SortedSetAddAsync(DataKey(key), batchList.ToArray());
                    batchList.Clear();
                }
            }

            // 添加剩余不足一批的数据
            if (batchList.Count > 0)
            {
                await _db.SortedSetAddAsync(DataKey(key), batchList.ToArray());
            }
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetRemoveAsync(string key, string member)
        {
            return await _db.SortedSetRemoveAsync(DataKey(key), member);
        }
        #endregion

        #region GEO
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entry">GPS经度、纬度、值</param>
        /// <returns></returns>
        public async Task<long> GeoAddAsync(string key, GeoEntry[] entry)
        {
            return await _db.GeoAddAsync(DataKey(key), entry);
        }
        /// <summary>
        ///  计算两个点的距离
        /// </summary>
        /// <param name="key"></param>
        /// <param name="num1"></param>
        /// <param name="num2"></param>
        /// <returns></returns>
        public async Task<double?> GeoDistanceAsync(string key, string num1, string num2)
        {
            return await _db.GeoDistanceAsync(DataKey(key), num1, num2, GeoUnit.Kilometers);
        }

        /// <summary>
        /// 返回指定member的坐标
        /// </summary>
        /// <param name="key"></param>
        /// <param name="redisValues"></param>
        /// <returns></returns>
        public async Task<GeoPosition?[]> GeoHashAsync(string key, RedisValue[] redisValues)
        {
            return await _db.GeoPositionAsync(DataKey(key), redisValues);
        }

        #endregion

        #region BitMap
        public async Task<bool> StringSetBitAsync(string key, long offset, bool flag)
        {
            return await _db.StringSetBitAsync(DataKey(key), offset, flag);
        }

        #endregion

        #region HyperLogLog
        public async Task<bool> HyperLogLogAddAsync(string key, string value)
        {
            return await _db.HyperLogLogAddAsync(DataKey(key), value);
        }

        public async Task<long> HyperLogLogLengthAsync(string key)
        {
            return await _db.HyperLogLogLengthAsync(DataKey(key));
        }
        /// <summary>
        /// 合并多个HyperLogLog成一个
        /// </summary>
        /// <param name="key"></param>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public async Task HyperLogLogMergeAsync(string key, string first, string second)
        {
            await _db.HyperLogLogMergeAsync(DataKey(key), first, second);
        }
        #endregion

        #region redis5.0 Stream队列

        /* position:
         *          '0-0'表示从头开始读取;
         *          '>'读取最新，未被消费的消息;
         *          '$' 表示使用者组将只读取在创建使用者组之后创建的消息
         * 
         * 
         */


        /// <summary>
        /// 创建单消费队列
        /// </summary>
        /// <param name="key"></param>
        /// <param name="filed"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string StreamAdd(string key, string filed, string value)
        {
            var result = _db.StreamAdd(DataKey(key), filed, value);
            return result;
        }

        /// <summary>
        /// 单个消息读取
        /// </summary>
        /// <param name="key"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public StreamEntry[] StreamRead(string key, string position)
        {
            var result = _db.StreamRead(DataKey(key), position);
            return result;
        }

        /// <summary>
        /// 创建消费组
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nameGroup"></param>
        /// <param name="position">0-0'表示从头开始读取;'>'读取最新，未被消费的消息;'$' 表示使用者组将只读取在创建使用者组之后创建的消息</param>
        /// <returns></returns>
        public bool StreamCreateConsumerGroup(string key, string nameGroup, string position)
        {
            var result = _db.StreamCreateConsumerGroup(DataKey(key), nameGroup, position);
            return result;
        }

        /// <summary>
        /// 判断组是否存在
        /// </summary>
        /// <param name="nameGroup"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public StreamGroupInfo[] StreamGroupInfo(string key)
        {
            var result = _db.StreamGroupInfo(DataKey(key));
            return result;
        }
        /// <summary>
        /// 通过组取里面的消息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nameGroup"></param>
        /// <param name="consumer"></param>
        /// <param name="position"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public StreamEntry[] StreamReadGroup(string key, string nameGroup, string consumer, string position, int count = 1)
        {
            var result = _db.StreamReadGroup(DataKey(key), nameGroup, consumer, position, count);
            return result;
        }

        /// <summary>
        /// 消息确认
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nameGroup"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public long StreamAcknowledge(string key, string nameGroup, string messageId)
        {
            var result = _db.StreamAcknowledge(DataKey(key), nameGroup, messageId);
            return result;
        }

        /// <summary>
        /// 返回有关待处理消息数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nameGroup"></param>
        /// <returns></returns>
        public StreamPendingInfo StreamPending(string key, string nameGroup)
        {
            var result = _db.StreamPending(DataKey(key), nameGroup);
            return result;
        }

        /// <summary>
        /// 待处理消息的详细信息
        /// </summary>
        /// <param name="key"></param>
        /// <param name="nameGroup"></param>
        /// <param name="consumer"></param>
        /// <param name="minId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public StreamPendingMessageInfo[] StreamPendingMessages(string key, string nameGroup, string consumer, string minId, int count = 100)
        {
            var result = _db.StreamPendingMessages(DataKey(key), nameGroup, count, consumer, minId: minId);
            return result;
        }
        #endregion

        #region lua脚本

        public RedisResult LuaScripts(string str, object obj)
        {
            return _db.ScriptEvaluate(LuaScript.Prepare(str), obj);
        }

        public async Task<RedisResult> ExecuteAsync(string str, params object[] obj)
        {
            return await _db.ExecuteAsync(str, obj);
        }

        #endregion

        #region 释放资源
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // 告诉GC不必再调用终结器
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 释放托管资源
                foreach (var conn in _connections.Values)
                {
                    conn?.Dispose();
                }
                _connections.Clear();
            }

            // 如果有非托管资源，也在这里释放

            _disposed = true;
        }

        ~RedisCacheManager()
        {
            Dispose(false); // 防御性调用，确保即使用户忘了调用 Dispose() 也能释放
        }

        #endregion
    }
}
