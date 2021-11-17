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
                    db.Database.EnsureDeleted();//É¾³ýÊý¾Ý¿â
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
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())//Ê¹ÓÃAutofac
            .ConfigureServices(services =>
            {
                Console.WriteLine("Ö´ÐÐË³ÐòConfigureServices");
            })
            .ConfigureAppConfiguration(config =>
            {
                Console.WriteLine("Ö´ÐÐË³ÐòConfigureAppConfiguration");
            })
            .ConfigureHostConfiguration(builder =>
            {
                Console.WriteLine("Ö´ÐÐË³ÐòConfigureHostConfiguration");
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                Console.WriteLine("Ö´ÐÐË³ÐòConfigureWebHostDefaults");
                webBuilder.UseStartup<Startup>();
            });
    }
}
