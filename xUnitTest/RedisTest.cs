using Common.Redis;
using Entity.Models;
using IBll;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace xUnitTest
{
    public class RedisTest
    {
        private readonly ITestOutputHelper _testOutput;
        //public readonly IRedisCacheManager _redisCacheManager;
        //public readonly IDatabase _database;
        private readonly ICompanyService _companyService;

        public RedisTest(IRedisCacheManager redisCacheManager)
        {
            //_database = redisCacheManager.GetRedisData();
            //_redisCacheManager = redisCacheManager;
        }
        [Fact]
        public void TruePatient()
        {
            var company = new Company();
            string id = Guid.NewGuid().ToString();
            //var result = new RedisCacheManager.CachePenetration("CachePenetration", company, id, (id) =>
            //{
            //    var result = _companyService.QueryWhere(p => p.Id == Guid.Parse(id)).FirstOrDefault();
            //    return result;
            //}, new TimeSpan(0, 0, 0, 59));
            //if (result == null)
            //{
            //    _testOutput.WriteLine("数据为空");
            //}
            //Assert.Null(result);
        }
    }
}
