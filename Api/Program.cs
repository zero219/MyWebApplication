using Entity.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Autofac;
using Api;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

builder.Host.ConfigureLogging((hostBuidleContext, loggingBuilder) =>
{
    //过滤掉System和Microsoft开头的命名空间下的组件产生的警告级别一下的日志
    loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
    loggingBuilder.AddFilter("System", LogLevel.Warning);
    loggingBuilder.AddLog4Net();
});

// Autofac容器
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(startup.ConfigureContainer);

builder.Host.ConfigureServices(services =>
{
    Console.WriteLine("执行顺序ConfigureServices");
});
builder.Host.ConfigureAppConfiguration(config =>
{
    Console.WriteLine("执行顺序ConfigureAppConfiguration");
});
builder.Host.ConfigureHostConfiguration(builder =>
{
    Console.WriteLine("执行顺序ConfigureHostConfiguration");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
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
        throw new Exception(ex.Message);
    }
}

startup.Configure(app, app.Environment);

app.Run();
