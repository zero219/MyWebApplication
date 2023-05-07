using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Common.Redis
{
    public interface IRedisCacheManager
    {
        IDatabase GetRedisData();
        string Get(string key);

        Task<string> GetAsync(string key);

        bool Set(string key, string value, TimeSpan cacheTime);

        Task<bool> SetAsync(string key, string value, TimeSpan cacheTime);

        bool SetNx(string key, string value, TimeSpan cacheTime = default(TimeSpan));

        bool Exist(string key);

        Task<bool> ExistAsync(string key);

        bool Delete(string key);

        Task<bool> DeleteAsync(string key);

        Task<bool> SetContainsAsync(string key, string value);

        Task<bool> SetAddAsync(string key, string value);

        Task<bool> SetRemoveAsync(string key, string value);

        Task<RedisValue[]> SetCombineAsync(int num, string first, string second);

        Task<double?> SortedSetScoreAsync(string key, string member);

        Task<SortedSetEntry[]> SortedSetRangeByRankWithScoresAsync(string key, long start, long stop);

        Task<bool> SortedSetAddAsync(string key, string member, double score);

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

        void Dispose();

        T CachePenetration<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);

        T CacheBreakdownLock<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);

        T CacheBreakdownTimeSpan<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);

        long NextId(string keyPrefix);

        RedisResult LuaScripts(string str, object obj);

        Task<RedisResult> ExecuteAsync(string str, params object[] obj);
    }
}
