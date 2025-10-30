using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common.Redis
{
    public interface IRedisCacheManager
    {
        string StrGet(string key);

        Task<string> StrGetAsync(string key);

        bool StrSet(string key, string value, TimeSpan? cacheTime = null);

        Task<bool> StrSetAsync(string key, string value, TimeSpan? cacheTime = null);

        bool StrSetNx(string key, string value, TimeSpan? cacheTime = null);

        /// <summary>
        /// 自增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        long StrIncr(string key, long value = 1);

        /// <summary>
        /// 自减
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        long StrDecr(string key, long value = 1);

        /// <summary>
        /// 批量设置键值对
        /// </summary>
        /// <param name="keyValues"></param>
        /// <returns></returns>
        bool StrBatch(Dictionary<string, string> keyValues);

        /// <summary>
        /// 批量获取值
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetBatch(IEnumerable<string> keys);

        /// <summary>
        /// 设置单个字段
        /// </summary>
        Task HashSetAsync<T>(string key, string field, T value);

        /// <summary>
        /// 设置多个字段
        /// </summary>
        Task HashSetAsync<T>(string key, Dictionary<string, T> values);

        /// <summary>
        /// 获取单个字段
        /// </summary>
        Task<T> HashGetAsync<T>(string key, string field);

        /// <summary>
        /// 获取整个哈希对象
        /// </summary>
        Task<Dictionary<string, T>> HashGetAllAsync<T>(string key);

        /// <summary>
        /// 删除一个或多个字段
        /// </summary>
        Task<bool> HashDeleteAsync(string key, params string[] fields);

        /// <summary>
        /// 判断字段是否存在
        /// </summary>
        Task<bool> HashExistsAsync(string key, string field);

        /// <summary>
        /// 获取字段数量
        /// </summary>
        Task<long> HashLengthAsync(string key);

        /// <summary>
        /// 数值字段自增
        /// </summary>
        Task<double> HashIncrementAsync(string key, string field, double value = 1);

        /// <summary>
        /// 数值字段自减
        /// </summary>
        Task<double> HashDecrementAsync(string key, string field, double value = 1);

        /// <summary>
        /// 扫描哈希内容（可选匹配模式）
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> HashScan(string key, string pattern = "*");

        Task<bool> SetContainsAsync(string key, string value);

        /// <summary>
        /// 获取所有成员
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        HashSet<string> SetGetAll(string key);

        Task<bool> SetAddAsync(string key, string value, TimeSpan? cacheTime = null);

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        Task<long> SetAddBatchAsync(string key, IEnumerable<string> values, TimeSpan? cacheTime = null);

        Task<bool> SetRemoveAsync(string key, string value);

        Task<RedisValue[]> SetCombineAsync(int num, string first, string second);

        Task<double?> SortedSetScoreAsync(string key, string member);

        Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(string key, long start, long stop);

        Task<bool> SortedSetAddAsync(string key, string member, double score);

        Task SortedSetAddBatchAsync(string key, IEnumerable<SortedSetEntry> entries);

        Task<bool> SortedSetRemoveAsync(string key, string member);

        Task<long> GeoAddAsync(string key, GeoEntry[] entry);

        Task<double?> GeoDistanceAsync(string key, string num1, string num2);

        Task<GeoPosition?[]> GeoHashAsync(string key, RedisValue[] redisValues);

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="offset">索引0开始</param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Task<bool> StringSetBitAsync(string key, long offset, bool flag);

        string StreamAdd(string key, string filed, string value);

        StreamEntry[] StreamRead(string key, string position);

        bool StreamCreateConsumerGroup(string key, string nameGroup, string position);

        StreamGroupInfo[] StreamGroupInfo(string key);

        StreamEntry[] StreamReadGroup(string key, string nameGroup, string consumer, string position, int count = 1);

        long StreamAcknowledge(string key, string nameGroup, string messageId);

        StreamPendingInfo StreamPending(string key, string nameGroup);

        StreamPendingMessageInfo[] StreamPendingMessages(string key, string nameGroup, string consumer, string minId, int count = 1);
        /// <summary>
        /// key加上过期时间
        /// </summary>
        void KeyAddExpire(string key, TimeSpan? cacheTime = null);
        /// <summary>
        /// key加上过期时间
        /// </summary>
        Task KeyAddExpireAsync(string key, TimeSpan? cacheTime = null);

        bool Exist(string key);

        Task<bool> ExistAsync(string key);

        bool Delete(string key);

        Task<bool> DeleteAsync(string key);

        void Dispose();

        T CachePenetration<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);

        T CacheBreakdownLock<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);

        T CacheBreakdownTimeSpan<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);

        long NextId(string keyPrefix);

        RedisResult LuaScripts(string str, object obj);

        Task<RedisResult> ExecuteAsync(string str, params object[] obj);
    }
}
