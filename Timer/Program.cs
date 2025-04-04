using Common.HttpRequest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer.Jobs;
using Timer.Models;
using Timer.Quartz;

namespace Timer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 注册Quartz调度程序
            builder.Services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            builder.Services.AddSingleton<IJobFactory, QuartzJobFactory>();
            builder.Services.AddHostedService<QuartzHostedService>();

            builder.Services.AddSingleton<HelloWorldJob>();
            builder.Services.AddSingleton<MyJob1>();

            builder.Services.AddSingleton<IApiClient, ApiClient>();

            builder.Services.AddHttpClient();

            // Configure
            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    await context.Response.WriteAsync("启动定时任务...", Encoding.UTF8);
                });
            });
            await app.RunAsync();
        }
    }
}
