using Common.Redis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Api.Controllers
{
    public class CustomBase<T> : ControllerBase
    {
        public readonly ILogger<T> _logger;

        #region Redis缓存
        public readonly IDatabase _database;
        public readonly IRedisCacheManager _redisCacheManager;
        #endregion

        public CustomBase(
            ILogger<T> logger,
            IRedisCacheManager redisCacheManager)
        {
            _logger = logger;
            _database = redisCacheManager.GetRedisData();
            _redisCacheManager = redisCacheManager;
        }
    }
}
