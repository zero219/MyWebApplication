using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Common.Redis
{
    /// <summary>
    /// Redis
    /// </summary>
    public class RedisCacheManager : IRedisCacheManager
    {
        /// <summary>
        /// 默认时间
        /// </summary>
        private readonly TimeSpan defaultTimeSpan = new TimeSpan(0, 0, 59);

        /// <summary>
        /// 默认key
        /// </summary>
        private const string RedisDataKey = "MyRedis";

        //连接字符串
        private readonly string _connectionString;
        //实例名称
        private readonly string _instanceName;
        //默认数据库
        private readonly int _defaultDB;

        private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections;
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
            if (value != null)
            {
                return GetRedisData().StringSet(DataKey(key), value, cacheTime);
            }
            return false;
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
            if (value != null)
            {
                return await GetRedisData().StringSetAsync(DataKey(key), value, cacheTime);
            }
            return false;
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
            if (value != null)
            {
                return GetRedisData().StringSet(DataKey(key), value, cacheTime, When.NotExists, CommandFlags.None);
            }
            return false;
        }

        /// <summary>
        /// 判断是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exist(string key)
        {
            return GetRedisData().KeyExists(key);
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

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_connections != null && _connections.Count > 0)
            {
                foreach (var item in _connections.Values)
                {
                    item.Close();
                }
            }
        }

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
            var result = GetRedisData().ScriptEvaluate(LuaScript.Prepare(str), obj);
            return result;
        }

        public RedisResult Execute(string str, object[] obj)
        {
            var result = GetRedisData().Execute(str, obj);
            return result;
        }
        
        #endregion
    }
    public class RedisData
    {
        public object obj { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        public long? expireTime { get; set; }
    }
}
