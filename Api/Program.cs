using Entity.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;

namespace Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetService<RoutineDbContext>();
                    db.Database.EnsureDeleted();//删除数据库
                    db.Database.EnsureCreated();

                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Database Migration Error!");
                    throw;
                }
            }
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureLogging((hostBuidleContext, loggingBuilder) =>
            {
                //过滤掉System和Microsoft开头的命名空间下的组件产生的警告级别一下的日志
                loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
                loggingBuilder.AddFilter("System", LogLevel.Warning);
                loggingBuilder.AddLog4Net();
            })
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())//使用Autofac
            .ConfigureServices(services =>
            {
                Console.WriteLine("执行顺序ConfigureServices");
            })
            .ConfigureAppConfiguration(config =>
            {
                Console.WriteLine("执行顺序ConfigureAppConfiguration");
            })
            .ConfigureHostConfiguration(builder =>
            {
                Console.WriteLine("执行顺序ConfigureHostConfiguration");
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                Console.WriteLine("执行顺序ConfigureWebHostDefaults");
                webBuilder.UseStartup<Startup>();
            });
    }
}
