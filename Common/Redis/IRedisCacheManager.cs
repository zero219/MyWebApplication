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

        RedisResult Execute(string str, object[] obj);
    }
}
