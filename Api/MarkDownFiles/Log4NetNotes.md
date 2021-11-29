# 准备工作
- 引入log4net的NuGet包
- 配置log4net.config文件，找不到该文件时修改该文件属性→复制到输出目录→始终复制

# .NET Core配置
1. 在Program类中配置  
 
``` c#
引入Microsoft.Extensions.Logging.Log4Net.AspNetCore包 
.ConfigureLogging((context ,loggingBuilder) =>
  {
    //过滤掉System和Microsoft开头的命名空间下的组件产生的警告级别一下的日志
    loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
    loggingBuilder.AddFilter("System", LogLevel.Warning);
    loggingBuilder.AddLog4Net();
 })
```

2. 在Startup类中配置 

``` markdown
public void Configure(ILoggerFactory loggerFactory)
{
    loggerFactory.AddLog4Net();
}
```

## 使用
``` c#
private readonly ILogger<HomeController> _logger;
private readonly ILoggerFactory _loggerFactory;

public HomeController(ILogger<HomeController> logger,
     ILoggerFactory loggerFactory)
{
   _logger = logger;
}

public IActionResult Index()
{
  _logger.LogError("错误1");
  _loggerFactory.CreateLogger<HomeController>().LogError("错误2");
}
```
如果要生成日志文件，在启动项配置和帮助类中一样，这样每用一个又要注入，很麻烦。

# Log帮助类
- LogHelper.cs
## 使用
```c#
 LogHelper.Info("信息输出");
```

