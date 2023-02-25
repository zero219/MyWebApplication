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

        bool Set(string key, string value,TimeSpan cacheTime);

        Task<bool> SetAsync(string key, string value, TimeSpan cacheTime);

        bool Exist(string key);

        Task<bool> ExistAsync(string key);

        bool Delete(string key);    

        Task<bool> DeleteAsync(string key);

        void Dispose();

        T CachePenetration<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);

        T CacheBreakdownLock<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);

        T CacheBreakdownTimeSpan<T, ID>(string keyPrefix, T type, ID id, Func<ID, T> func, TimeSpan timeSpan);
    }
}
