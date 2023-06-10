using Quartz;
using System.Threading.Tasks;
using System;
using Common.HttpRequest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Net.Http;
using StackExchange.Redis;

namespace Timer.Jobs
{
    public class HelloWorldJob : IJob
    {
        private readonly IApiClient  _apiClient;
        private IServiceScopeFactory _serviceScopeFactory { get; set; }

        public HelloWorldJob(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("HelloWorld！！！！");
            var result = await _apiClient.GetAsync<SortedSetEntry[]>("http://localhost:5000/api/isLikeTop");
            Console.WriteLine(result);
        }
    }
}
