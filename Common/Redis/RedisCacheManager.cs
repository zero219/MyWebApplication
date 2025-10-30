using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Redis
{
    /// <summary>
    /// Redis
    /// </summary>
    public class RedisCacheManager : IRedisCacheManager, IDisposable
    {
        /// <summary>
        /// 默认时间
        /// </summary>
        private readonly TimeSpan defaultTimeSpan = new TimeSpan(0, 0, 59);

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
        private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections;

        /// <summary>
        /// 释放标志
        /// </summary>
        private bool _disposed = false;

        public RedisCacheManager(string connectionString, string instanceName, int defaultDB = 0)
        {
            _connectionString = connectionString;
            _instanceName = instanceName;
            _defaultDB = defaultDB;
            _connections = new ConcurrentDictionary<string, ConnectionMultiplexer>();
        }

        /// <summary>
        /// 获取ConnectionMultiplexer
        /// </summary>
        /// <returns></returns>
        private ConnectionMultiplexer GetConnect()
        {
            return _connections.GetOrAdd(_instanceName, p => ConnectionMultiplexer.Connect(_connectionString));
        }

        /// <summary>
        /// 访问Redis数据库
        /// </summary>
        /// <returns></returns>
        public IDatabase GetRedisData()
        {
            return GetConnect().GetDatabase(_defaultDB);
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
        public string Get(string key)
        {
            return GetRedisData().StringGet(DataKey(key));
        }

        /// <summary>
        /// 获取缓存值(使用该异步方法时需要在获取结果后面.Result)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<string> GetAsync(string key)
        {
            return await GetRedisData().StringGetAsync(DataKey(key));
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        public bool Set(string key, string value, TimeSpan cacheTime = default(TimeSpan))
        {
            if (cacheTime == default(TimeSpan))
            {
                cacheTime = defaultTimeSpan;
            }
            return GetRedisData().StringSet(DataKey(key), value, cacheTime);
        }

        /// <summary>
        /// 保存(使用该异步方法时需要在后面使用.Wait())
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        public async Task<bool> SetAsync(string key, string value, TimeSpan cacheTime = default(TimeSpan))
        {
            if (cacheTime == default(TimeSpan))
            {
                cacheTime = defaultTimeSpan;
            }

            return await GetRedisData().StringSetAsync(DataKey(key), value, cacheTime);
        }

        /// <summary>
        /// 判断key是否存在，不存在就创建
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        public bool SetNx(string key, string value, TimeSpan cacheTime = default(TimeSpan))
        {
            if (cacheTime == default(TimeSpan))
            {
                cacheTime = defaultTimeSpan;
            }
            return GetRedisData().StringSet(DataKey(key), value, cacheTime, When.NotExists, CommandFlags.None);

        }

        /// <summary>
        /// 判断是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exist(string key)
        {
            return GetRedisData().KeyExists(DataKey(key));
        }

        /// <summary>
        ///  判断是否存在(异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<bool> ExistAsync(string key)
        {
            return await GetRedisData().KeyExistsAsync(DataKey(key));
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Delete(string key)
        {
            return GetRedisData().KeyDelete(DataKey(key));
        }

        /// <summary>
        /// 删除值(异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> DeleteAsync(string key)
        {
            return await GetRedisData().KeyDeleteAsync(DataKey(key));
        }

        #endregion

        #region Hash类型
        /// <summary>
        /// 设置单个字段
        /// </summary>
        public async Task HashSetAsync<T>(string key, string field, T value)
        {
            string json = JsonConvert.SerializeObject(value);
            await GetRedisData().HashSetAsync(DataKey(key), field, json);
        }

        /// <summary>
        /// 设置多个字段
        /// </summary>
        public async Task HashSetAsync<T>(string key, Dictionary<string, T> values)
        {
            var entries = values.Select(x => new HashEntry(x.Key, JsonConvert.SerializeObject(x.Value))).ToArray();
            await GetRedisData().HashSetAsync(DataKey(key), entries);
        }

        /// <summary>
        /// 获取单个字段
        /// </summary>
        public async Task<T> HashGetAsync<T>(string key, string field)
        {
            var value = await GetRedisData().HashGetAsync(DataKey(key), field);
            if (value.IsNullOrEmpty) return default;
            return JsonConvert.DeserializeObject<T>(value);
        }

        /// <summary>
        /// 获取整个哈希对象
        /// </summary>
        public async Task<Dictionary<string, T>> HashGetAllAsync<T>(string key)
        {
            var entries = await GetRedisData().HashGetAllAsync(DataKey(key));
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
            long count = await GetRedisData().HashDeleteAsync(DataKey(key), values);
            return count > 0;
        }

        /// <summary>
        /// 判断字段是否存在
        /// </summary>
        public async Task<bool> HashExistsAsync(string key, string field)
        {
            return await GetRedisData().HashExistsAsync(DataKey(key), field);
        }

        /// <summary>
        /// 获取字段数量
        /// </summary>
        public async Task<long> HashLengthAsync(string key)
        {
            return await GetRedisData().HashLengthAsync(DataKey(key));
        }

        /// <summary>
        /// 数值字段自增
        /// </summary>
        public async Task<double> HashIncrementAsync(string key, string field, double value = 1)
        {
            return await GetRedisData().HashIncrementAsync(DataKey(key), field, value);
        }

        /// <summary>
        /// 数值字段自减
        /// </summary>
        public async Task<double> HashDecrementAsync(string key, string field, double value = 1)
        {
            return await GetRedisData().HashDecrementAsync(DataKey(key), field, value);
        }

        /// <summary>
        /// 扫描哈希内容（可选匹配模式）
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> HashScan(string key, string pattern = "*")
        {
            foreach (var entry in GetRedisData().HashScan(DataKey(key), pattern))
            {
                yield return new KeyValuePair<string, string>(entry.Name, entry.Value);
            }
        }
        #endregion

        #region Set集合
        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> SetContainsAsync(string key, string value)
        {
            return await GetRedisData().SetContainsAsync(DataKey(key), value);
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> SetAddAsync(string key, string value)
        {
            return await GetRedisData().SetAddAsync(DataKey(key), value);
        }
        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> SetRemoveAsync(string key, string value)
        {
            return await GetRedisData().SetRemoveAsync(DataKey(key), value);
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
                    result = await GetRedisData().SetCombineAsync(SetOperation.Intersect, DataKey(first), DataKey(second));
                    break;
                case 2:
                    result = await GetRedisData().SetCombineAsync(SetOperation.Difference, DataKey(first), DataKey(second));
                    break;
                default:
                    result = await GetRedisData().SetCombineAsync(SetOperation.Union, DataKey(first), DataKey(second));
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
            return await GetRedisData().SortedSetScoreAsync(DataKey(key), member);
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
            return await GetRedisData().SortedSetRangeByRankWithScoresAsync(DataKey(key), start, stop);
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
            return await GetRedisData().SortedSetAddAsync(DataKey(key), member, score);
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public async Task<bool> SortedSetRemoveAsync(string key, string member)
        {
            return await GetRedisData().SortedSetRemoveAsync(DataKey(key), member);
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
            return await GetRedisData().GeoAddAsync(DataKey(key), entry);
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
            return await GetRedisData().GeoDistanceAsync(DataKey(key), num1, num2, GeoUnit.Kilometers);
        }

        /// <summary>
        /// 返回指定member的坐标
        /// </summary>
        /// <param name="key"></param>
        /// <param name="redisValues"></param>
        /// <returns></returns>
        public async Task<GeoPosition?[]> GeoHashAsync(string key, RedisValue[] redisValues)
        {
            return await GetRedisData().GeoPositionAsync(DataKey(key), redisValues);
        }

        #endregion

        #region BitMap
        public async Task<bool> StringSetBitAsync(string key, long offset, bool flag)
        {
            return await GetRedisData().StringSetBitAsync(DataKey(key), offset, flag);
        }

        #endregion

        #region HyperLogLog
        public async Task<bool> HyperLogLogAddAsync(string key, string value)
        {
            return await GetRedisData().HyperLogLogAddAsync(DataKey(key), value);
        }

        public async Task<long> HyperLogLogLengthAsync(string key)
        {
            return await GetRedisData().HyperLogLogLengthAsync(DataKey(key));
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
            await GetRedisData().HyperLogLogMergeAsync(DataKey(key), first, second);
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
            var result = GetRedisData().StreamAdd(DataKey(key), filed, value);
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
            var result = GetRedisData().StreamRead(DataKey(key), position);
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
            var result = GetRedisData().StreamCreateConsumerGroup(DataKey(key), nameGroup, position);
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
            var result = GetRedisData().StreamGroupInfo(DataKey(key));
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
            var result = GetRedisData().StreamReadGroup(DataKey(key), nameGroup, consumer, position, count);
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
            var result = GetRedisData().StreamAcknowledge(DataKey(key), nameGroup, messageId);
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
            var result = GetRedisData().StreamPending(DataKey(key), nameGroup);
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
            var result = GetRedisData().StreamPendingMessages(DataKey(key), nameGroup, count, consumer, minId: minId);
            return result;
        }
        #endregion

        #region 缓存穿透、击穿

        public async Task<bool> BloomAddAsync(RedisKey key, RedisValue value)
            => (bool)await GetRedisData().ExecuteAsync("BF.ADD", key, value);

        /// <summary>
        /// 缓存穿透-缓存空值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="ID"></typeparam>
        /// <param name="keyPrefix"></param>
        /// <param name="t"></param>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public T CachePenetration<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan)
        {
            string key = string.Format("{0}:{1}", keyPrefix, id);
            // 获取缓存
            string getValue = GetAsync(key).Result;
            // 判断缓存是否存在
            if (!string.IsNullOrWhiteSpace(getValue))
            {
                //存在直接返回
                return JsonConvert.DeserializeObject<T>(getValue);
            }
            // 判断命中的是空值
            if (getValue != null)
            {
                // 返回一个错误信息
                return default(T);
            }
            // 不存在执行查询方法
            T t = func(id);
            if (t == null)
            {
                // 存入空值
                this.SetAsync(key, "", timeSpan).Wait();
                // 返回错误信息
                return default(T);
            }
            // 存在，写入缓存
            this.Set(key, JsonConvert.SerializeObject(t), timeSpan);
            return t;
        }

        /// <summary>
        /// 缓存击穿-互斥锁
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="ID"></typeparam>
        /// <param name="keyPrefix"></param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public T CacheBreakdownLock<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan)
        {
            string key = string.Format("{0}:{1}", keyPrefix, id);
            // 获取缓存
            string getValue = GetAsync(key).Result;
            // 判断缓存是否存在
            if (!string.IsNullOrWhiteSpace(getValue))
            {
                //存在直接返回
                return JsonConvert.DeserializeObject<T>(getValue);
            }
            // 判断命中的是空值
            if (getValue != null)
            {
                // 返回一个错误信息
                return default(T);
            }
            string lockKey = string.Format("{0}:{1}", CacheKeys.LOCK_KEY, id);
            T t = default(T);
            try
            {
                // 获取互斥锁
                var isLock = Set(lockKey, "lock", new TimeSpan(0, 10, 0));
                // 判断是否获取成功
                if (!isLock)
                {
                    // 获取锁失败，休眠并重试
                    Thread.Sleep(10000);
                    return CacheBreakdownLock(keyPrefix, type, id, func, timeSpan);
                }
                // 获取锁成功，执行查询
                t = func(id);
                if (t == null)
                {
                    // 存入空值
                    this.SetAsync(key, "", timeSpan).Wait();
                    // 返回错误信息
                    return default(T);
                }
                // 存在，写入缓存
                this.SetAsync(key, JsonConvert.SerializeObject(t), timeSpan).Wait();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                //释放锁
                Delete(lockKey);
            }
            return t;
        }

        /// <summary>
        /// 缓存穿透-逻辑过期
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="ID"></typeparam>
        /// <param name="keyPrefix"></param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="func"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public T CacheBreakdownTimeSpan<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan)
        {
            string key = string.Format("{0}:{1}", keyPrefix, id);
            // 获取缓存
            string getValue = GetAsync(key).Result;
            // 判断命中的是空值
            if (string.IsNullOrWhiteSpace(getValue))
            {
                // 返回一个错误信息
                return default(T);
            }
            // 反序列化json
            RedisData redisData = JsonConvert.DeserializeObject<RedisData>(getValue);
            long time = redisData.expireTime.Value;
            // 转泛型
            //T t = (T)Convert.ChangeType(, typeof(T));
            T t = JsonConvert.DeserializeObject<T>(redisData.obj.ToString());
            // 获取当前时间戳
            long nowTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            // 判断时间戳是否过期
            if (time > nowTime)
            {
                // 未过期，直接返回店铺信息
                return t;
            }
            // 已经过期，缓存重建
            // 获取互斥锁
            string lockKey = string.Format("{0}:{1}", CacheKeys.LOCKTIMESPAN_KEY, id);
            var isLock = Set(lockKey, "lockTimeSpan", new TimeSpan(0, 10, 0));
            // 判断是否获取成功
            if (isLock)
            {
                // 成功，开启独立线程，实现缓存重建
                Task.Run(() =>
                {
                    try
                    {
                        // 执行查询
                        T tt = func(id);
                        // 重建缓存
                        RedisData data = new RedisData();
                        data.obj = tt;
                        data.expireTime = time;
                        SetAsync(lockKey, JsonConvert.SerializeObject(data), timeSpan).Wait();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                    finally
                    {
                        //释放锁
                        Delete(lockKey);
                    }
                });
            }
            return t;
        }

        #endregion

        #region 自增唯一id
        /// <summary>
        /// 开始时间戳
        /// </summary>
        private readonly long BeginTimestamp = 1672502400L;
        /// <summary>
        /// 序列号位数
        /// </summary>
        private readonly int CountBits = 32;

        /// <summary>
        /// 生成唯一id
        /// </summary>
        /// <param name="keyPrefix"></param>
        /// <returns></returns>
        public long NextId(string keyPrefix)
        {
            //生成时间戳
            long nowTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            long timeStamp = nowTime - BeginTimestamp;
            // 获取日期
            string date = DateTime.Now.ToString("yyyy:MM:dd");
            // 自增长
            long count = GetRedisData().StringIncrement(DataKey("icr:") + keyPrefix + ":" + date);
            return timeStamp << CountBits | count;
        }

        #endregion

        #region lua脚本

        public RedisResult LuaScripts(string str, object obj)
        {
            return GetRedisData().ScriptEvaluate(LuaScript.Prepare(str), obj);
        }

        public async Task<RedisResult> ExecuteAsync(string str, params object[] obj)
        {
            return await GetRedisData().ExecuteAsync(str, obj);
        }

        #endregion

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
    }
}
