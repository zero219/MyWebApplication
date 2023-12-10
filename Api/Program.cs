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
    //���˵�System��Microsoft��ͷ�������ռ��µ���������ľ��漶��һ�µ���־
    loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
    loggingBuilder.AddFilter("System", LogLevel.Warning);
    loggingBuilder.AddLog4Net();
});

// Autofac����
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(startup.ConfigureContainer);

builder.Host.ConfigureServices(services =>
{
    Console.WriteLine("ִ��˳��ConfigureServices");
});
builder.Host.ConfigureAppConfiguration(config =>
{
    Console.WriteLine("ִ��˳��ConfigureAppConfiguration");
});
builder.Host.ConfigureHostConfiguration(builder =>
{
    Console.WriteLine("ִ��˳��ConfigureHostConfiguration");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetService<RoutineDbContext>();
        db.Database.EnsureDeleted();//ɾ�����ݿ�
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
