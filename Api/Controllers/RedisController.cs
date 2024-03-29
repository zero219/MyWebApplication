﻿using Common.Redis;
using IBll;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Entity.Models;
using System.Linq;
using Entity.Dtos;
using AutoMapper;
using Entity.Dtos.SeckillVoucherDto;
using System.Threading;
using StackExchange.Redis;

namespace Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class RedisController : ControllerBase
    {
        private readonly IRedisCacheManager _redisCacheManager;
        private readonly ILogger<RedisController> _logger;
        private readonly ICompanyService _companyService;
        private readonly ISeckillVoucherService _seckillVoucherService;
        private readonly ISignService _signService;
        private readonly IFollowService _followService;
        private readonly IMapper _mapper;
        public RedisController(ILogger<RedisController> logger,
            IRedisCacheManager redisCacheManager,
            ICompanyService companyService,
            ISignService signService,
            ISeckillVoucherService seckillVoucherService,
            IFollowService followService,
            IMapper mapper)
        {
            _logger = logger;
            _redisCacheManager = redisCacheManager;
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _seckillVoucherService = seckillVoucherService ?? throw new ArgumentNullException(nameof(seckillVoucherService));
            _signService = signService ?? throw new ArgumentNullException(nameof(signService));
            _followService = followService ?? throw new ArgumentNullException(nameof(followService));
            _mapper = mapper;
        }
        /// <summary>
        /// 基本缓存模型和思路
        /// </summary>
        /// <returns></returns>
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            string key = string.Format("{0}:{1}", CacheKeys.MY_KEY, "bbdee09c-089b-4d30-bece-44df59237100");
            var getKey = _redisCacheManager.GetAsync(key).Result;
            if (!string.IsNullOrEmpty(getKey))
            {
                return Ok(getKey);
            }
            var company = await _companyService.QueryWhere(p => p.Id == Guid.Parse("bbdee09c-089b-4d30-bece-44df59237100")).FirstOrDefaultAsync();
            var companyStr = JsonConvert.SerializeObject(company);
            var setKey = _redisCacheManager.SetAsync(key, companyStr, new TimeSpan(0, 0, 59));
            return Ok(company);
        }


        /// <summary>
        /// 测试缓存穿透
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("cachePenetration")]
        public IActionResult CachePenetration(string id)
        {
            CompanyDto companyDto = new CompanyDto();
            var result = _redisCacheManager.CachePenetration("cachePenetration", companyDto, id, (id) =>
             {
                 var company = _companyService.QueryWhere(p => p.Id == Guid.Parse(id)).FirstOrDefault();
                 var companyDtos = _mapper.Map<CompanyDto>(company);
                 return companyDtos;
             }, new TimeSpan(0, 0, 30, 0));
            if (result == null)
            {
                return Ok("您输入的信息有误");
            }
            return Ok(result);
        }

        /// <summary>
        /// 测试缓存击穿-互斥锁
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("cacheBreakdownLock")]
        public IActionResult CacheBreakdownLock(string id)
        {
            CompanyDto companyDto = new CompanyDto();
            var result = _redisCacheManager.CacheBreakdownLock("cacheBreakdownLock", companyDto, id, (id) =>
            {
                var company = _companyService.QueryWhere(p => p.Id == Guid.Parse(id)).FirstOrDefault();
                var companyDtos = _mapper.Map<CompanyDto>(company);
                return companyDtos;
            }, new TimeSpan(0, 0, 30, 0));
            if (result == null)
            {
                return Ok("您输入的信息有误");
            }
            return Ok(result);
        }

        /// <summary>
        /// 测试缓存击穿-逻辑过期
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("cacheBreakdownTimeSpan")]
        public IActionResult CacheBreakdownTimeSpan(string id)
        {
            var company = _companyService.QueryWhere(p => p.Id == Guid.Parse(id)).FirstOrDefault();
            if (company == null)
            {
                return NotFound();
            }
            var companyDtos = _mapper.Map<CompanyDto>(company);
            //RedisData redisData = new RedisData();
            //redisData.obj = companyDtos;
            //redisData.expireTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            //var a = _redisCacheManager.Set(string.Format("{0}:{1}", "cacheBreakdownTimeSpan", id), JsonConvert.SerializeObject(redisData), new TimeSpan(0, 0, 30, 0));
            CompanyDto companyDto = new CompanyDto();
            var result = _redisCacheManager.CacheBreakdownTimeSpan("cacheBreakdownTimeSpan", companyDto, id, (id) =>
            {
                return companyDtos;
            }, new TimeSpan(0, 0, 30, 0));
            if (result == null)
            {
                return Ok("您输入的信息有误");
            }
            return Ok(result);
        }

        /// <summary>
        ///  秒杀优惠券
        /// </summary>
        /// <param name="seckillVoucherDto"></param>
        /// <returns></returns>
        [HttpPost("seckill")]
        public async Task<IActionResult> Seckill([FromBody] SeckillVoucherDto seckillVoucherDto)
        {
            var result = await _seckillVoucherService.SeckillVoucherStreamAsync(seckillVoucherDto.VoucherId, seckillVoucherDto.UserId);
            return Ok(result);
        }

        /// <summary>
        /// 点赞
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("isLike")]
        public async Task<IActionResult> IsLike([FromQuery] long id)
        {
            var result = string.Empty;
            var key = string.Format("{0}:{1}", CacheKeys.ISLIKE_KEY, "blog");
            var flag = await _redisCacheManager.SortedSetScoreAsync(key, id.ToString());
            if (flag == null)
            {
                // 数据点赞库数量+1
                result = await _redisCacheManager.SortedSetAddAsync(key, id.ToString(), double.Parse(DateTime.Now.ToString("yyyyMMddHHmmsss"))) ? "点赞成功" : "点赞失败";
            }
            else
            {
                // 数据点赞库数量-1
                result = await _redisCacheManager.SortedSetRemoveAsync(key, id.ToString()) ? "取消点赞成功" : "取消点赞失败";

            }
            return Ok(result);
        }

        /// <summary>
        /// 获取点赞的前五
        /// </summary>
        /// <returns></returns>
        [HttpGet("isLikeTop")]
        public async Task<IActionResult> IsLikeTop()
        {
            var key = string.Format("{0}:{1}", CacheKeys.ISLIKE_KEY, "blog");
            var arr = await _redisCacheManager.SortedSetRangeByRankWithScoresAsync(key, 0, 5);
            return Ok(arr);
        }

        /// <summary>
        /// 关注
        /// </summary>
        /// <param name="followUserId"></param>
        /// <param name="isFollow"></param>
        /// <returns></returns>
        [HttpGet("isFollow")]
        public async Task<IActionResult> IsFollow([FromQuery] long followUserId, [FromQuery] bool isFollow)
        {
            var result = await _followService.IsFollowAsync(followUserId, isFollow);

            return Ok(result);
        }

        /// <summary>
        /// 共同关注
        /// </summary>
        /// <returns></returns>
        [HttpGet("ofUser")]
        public async Task<IActionResult> OfUser()
        {
            var first = string.Format("{0}:{1}", CacheKeys.FOLLOWS_KEY, "tom");
            var second = string.Format("{0}:{1}", CacheKeys.FOLLOWS_KEY, "jerry");
            var result = await _redisCacheManager.SetCombineAsync(2, first, second);
            return Ok(result);
        }

        /// <summary>
        /// 距离
        /// </summary>
        /// <returns></returns>
        [HttpGet("geoLocation")]
        public async Task<IActionResult> GeoLocation()
        {
            // 美食
            var delicacyKey = string.Format("{0}:{1}", CacheKeys.GEO_KEY, "delicacy");
            GeoEntry[] geoEntriesDelicacy = {
                 new GeoEntry(116.3474879, 39.9432725, "beijing"),
                 new GeoEntry(114.054555, 22.546327,"shenzhen")
            };
            var delicacy = await _redisCacheManager.GeoAddAsync(delicacyKey, geoEntriesDelicacy);
            // 北京到深圳的公里数
            var result = await _redisCacheManager.GeoDistanceAsync(delicacyKey, "beijing", "shenzhen");
            return Ok(result);
        }

        /// <summary>
        /// 签到
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("sign")]
        public async Task<IActionResult> Sign([FromQuery] long userId)
        {
            var result = await _signService.Sign(userId);
            return Ok(result);
        }

        /// <summary>
        /// 统计连续签到的次数
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("signCount")]
        public async Task<IActionResult> SignCount([FromQuery] long userId)
        {
            var result = await _signService.SignCountAsync(userId);
            return Ok(result);
        }
    }
}
