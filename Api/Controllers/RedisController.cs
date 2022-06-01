using Common.Redis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace Api.Controllers
{
    [Route("api/Redis")]
    [ApiController]
    public class RedisController : CustomBase<RedisController>
    {
        public RedisController(ILogger<RedisController> logger,
            IRedisCacheManager redisCacheManager) : base(logger, redisCacheManager)
        {

        }
        [HttpGet("Test")]
        public IActionResult Test()
        {
            var getKey = _redisCacheManager.GetAsync("myKey").Result;
            var setKey = _redisCacheManager.SetAsync("20220224_Key","123",new TimeSpan(0,0,59));
            return Ok();
        }
    }
}
