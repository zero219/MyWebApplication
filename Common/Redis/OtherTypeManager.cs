using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Redis
{
    public partial class RedisCacheManager : IRedisCacheManager
    {
        #region 公共方法
        /// <summary>
        /// key加上过期时间
        /// </summary>
        public void KeyAddExpire(string key, TimeSpan? cacheTime = null)
        {
            if (cacheTime.HasValue)
            {
                _db.KeyExpire(DataKey(key), cacheTime);
            }
        }
        /// <summary>
        /// key加上过期时间
        /// </summary>
        public async Task KeyAddExpireAsync(string key, TimeSpan? cacheTime = null)
        {
            if (cacheTime.HasValue)
            {
                await _db.KeyExpireAsync(DataKey(key), cacheTime);
            }

        }

        /// <summary>
        /// 判断是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exist(string key)
        {
            return _db.KeyExists(DataKey(key));
        }

        /// <summary>
        ///  判断是否存在(异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<bool> ExistAsync(string key)
        {
            return await _db.KeyExistsAsync(DataKey(key));
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Delete(string key)
        {
            return _db.KeyDelete(DataKey(key));
        }

        /// <summary>
        /// 删除值(异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(string key)
        {
            return await _db.KeyDeleteAsync(DataKey(key));
        }
        #endregion

        #region 缓存穿透、击穿

        public async Task<bool> BloomAddAsync(RedisKey key, RedisValue value)
            => (bool)await _db.ExecuteAsync("BF.ADD", key, value);

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
            string getValue = StrGet(key);
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
                this.StrSet(key, "", timeSpan);
                // 返回错误信息
                return default(T);
            }
            // 存在，写入缓存
            this.StrSet(key, JsonConvert.SerializeObject(t), timeSpan);
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
            string getValue = StrGet(key);
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
                var isLock = StrSet(lockKey, "lock", new TimeSpan(0, 10, 0));
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
                    this.StrSet(key, "", timeSpan);
                    // 返回错误信息
                    return default(T);
                }
                // 存在，写入缓存
                this.StrSet(key, JsonConvert.SerializeObject(t), timeSpan);

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
            string getValue = StrGet(key);
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
            var isLock = StrSet(lockKey, "lockTimeSpan", new TimeSpan(0, 10, 0));
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
                        StrSet(lockKey, JsonConvert.SerializeObject(data), timeSpan);
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
            long count = _db.StringIncrement(DataKey("icr:") + keyPrefix + ":" + date);
            return timeStamp << CountBits | count;
        }

        #endregion
    }
}
