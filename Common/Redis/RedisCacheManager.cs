using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        public void Set(string key, string value, TimeSpan cacheTime = default(TimeSpan))
        {
            if (cacheTime == default(TimeSpan))
            {
                cacheTime = defaultTimeSpan;
            }
            if (value != null)
            {
                GetRedisData().StringSet(DataKey(key), JsonConvert.SerializeObject(value), cacheTime);
            }
        }

        /// <summary>
        /// 保存(使用该异步方法时需要在后面使用.Wait())
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        public async Task SetAsync(string key, string value, TimeSpan cacheTime = default(TimeSpan))
        {
            if (cacheTime == default(TimeSpan))
            {
                cacheTime = defaultTimeSpan;
            }
            if (value != null)
            {
                await GetRedisData().StringSetAsync(DataKey(key), JsonConvert.SerializeObject(value), cacheTime);
            }
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
    }
}
