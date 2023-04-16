using Common.Redis;
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

namespace Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class RedisController : CustomBase<RedisController>
    {
        private readonly ICompanyService _companyService;
        private readonly ISeckillVoucherService _seckillVoucherService;
        private readonly IMapper _mapper;
        public RedisController(ILogger<RedisController> logger,
            IRedisCacheManager redisCacheManager,
            ICompanyService companyService,
            ISeckillVoucherService seckillVoucherService,
            IMapper mapper) : base(logger, redisCacheManager)
        {
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _seckillVoucherService = seckillVoucherService ?? throw new ArgumentNullException(nameof(seckillVoucherService));
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
    }
}
