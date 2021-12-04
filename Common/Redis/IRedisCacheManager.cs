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
        Task<string> GetValue(string key);
        Task Set(string key, object value, TimeSpan cacheTime);

        Task<bool> Exist(string key);

        Task Remove(string key);

        void Dispose();
    }
}
