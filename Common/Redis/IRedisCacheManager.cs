using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Redis
{
    public interface IRedisCacheManager
    {
        #region String类型

        bool StrSet<T>(string key, T value, TimeSpan? cacheTime = null);

        Task<bool> StrSetAsync<T>(string key, T value, TimeSpan? cacheTime = null);

        string StrGet(string key);

        Task<string> StrGetAsync(string key);

        bool StrSetNx<T>(string key, T value, TimeSpan? cacheTime = null);

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
        bool StrSetBatch(Dictionary<string, string> keyValues);

        /// <summary>
        /// 批量获取值
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public Dictionary<string, string> StrGetBatch(IEnumerable<string> keys);

        #endregion

        #region Hash类型
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

        #endregion

        #region List集合
        /// <summary>
        /// 左入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        long ListLeftPush<T>(string key, T item);

        /// <summary>
        /// 右入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        long ListRightPush<T>(string key, T item);

        /// <summary>
        /// 左批量入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        long ListLeftPushBatch<T>(string key, IEnumerable<T> items);

        /// <summary>
        /// 右批量入队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        long ListRightPushBatch<T>(string key, IEnumerable<T> items);

        /// <summary>
        /// 左出队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        T ListLeftPop<T>(string key);

        /// <summary>
        /// 右出队
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        T ListRightPop<T>(string key);

        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        long ListLength(string key);

        /// <summary>
        /// 获取范围
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        List<T> ListRange<T>(string key, long start = 0, long stop = -1);


        /// <summary>
        /// 删除指定值，count=0 删除所有
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        long ListRemove<T>(string key, T value, long count = 0);
        #endregion

        #region Set类型

        /// <summary>
        /// 判断元素是否存在
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> SetContainsAsync<T>(string key, T value);

        /// <summary>
        /// 获取所有成员（泛型）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<HashSet<T>> SetGetAllAsync<T>(string key);

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        Task<bool> SetAddAsync<T>(string key, T value, TimeSpan? cacheTime = null);

        /// <summary>
        /// 批量添加元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        Task<long> SetAddBatchAsync<T>(string key, IEnumerable<T> values, TimeSpan? cacheTime = null);

        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<bool> SetRemoveAsync<T>(string key, T value);

        /// <summary>
        /// 求并集、交集、差集
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        Task<RedisValue[]> SetCombineAsync(int num, string first, string second);
        #endregion

        #region SortedSet类型
        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <param name="score"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        Task<bool> SortedSetAddAsync<T>(string key, T member, double score, TimeSpan? cacheTime = null);

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        Task<long> SortedSetAddBatchAsync<T>(string key, IEnumerable<(T value, double score)> values, TimeSpan? cacheTime = null);

        /// <summary>
        /// 高性能批量添加 SortedSet 元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="entries"></param>
        /// <returns></returns>
        Task SortedSetAddBatchAsync(string key, IEnumerable<SortedSetEntry> entries);

        /// <summary>
        /// SortedSet是否存在
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        Task<double?> SortedSetScoreAsync<T>(string key, T member);

        /// <summary>
        /// 获取范围数据，索引0开始
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(string key, long start, long stop);

        /// <summary>
        /// 删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        Task<bool> SortedSetRemoveAsync<T>(string key, T member);

        #endregion

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
