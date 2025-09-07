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
using System.Threading.Tasks;
using Common.Middleware;
using Autofac.Core;
using Marvin.Cache.Headers;
using Bll;
using Common.IdentityAuth;
using Entity.Models.IdentityModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;
using System.IO;
using System.Reflection;
using System.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Common.Redis;
using System.Runtime.Loader;
using System.Linq;
using Microsoft.Extensions.Options;

public class Program
{
    public static async Task Main(string[] args)
    {
        string apiName = "RESTfull";

        var builder = WebApplication.CreateBuilder(args);

        //Autofac容器
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            Console.WriteLine("执行顺序ConfigureContainer");
            /*
             * InstancePerDependency等于AddTransient
             * InstancePerLifetimeScope等于AddScoped
             * SingleInstance等于AddSingleton
             */

            Assembly assemblyDal = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Dal"));
            containerBuilder.RegisterTypes(assemblyDal.GetTypes()).AsImplementedInterfaces().InstancePerLifetimeScope();

            Assembly assemblyBll = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Bll"));
            containerBuilder.RegisterTypes(assemblyBll.GetTypes()).AsImplementedInterfaces().InstancePerLifetimeScope();

            #region Redis缓存注入
            var conn = builder.Configuration.GetSection("Redis:DefaultConnection").Value;
            var instanceName = builder.Configuration.GetSection("Redis:InstanceName").Value;
            var defaultDB = int.Parse(builder.Configuration.GetSection("Redis:DefaultDB").Value.ToString() ?? "0");
            containerBuilder.Register(x => new RedisCacheManager(conn, instanceName, defaultDB)).As<IRedisCacheManager>().SingleInstance();
            #endregion

        });

        // 使用log4net
        builder.Host.ConfigureLogging((hostBuidleContext, loggingBuilder) =>
        {
            //过滤掉System和Microsoft开头的命名空间下的组件产生的警告级别一下的日志
            loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
            loggingBuilder.AddFilter("System", LogLevel.Warning);
            loggingBuilder.AddLog4Net();
        });

        // 注册自定义异常捕捉中间件
        builder.Services.AddTransient<ExceptionHandlingMiddleware>();

        // 多租户
        builder.Services.AddScoped<TenantInfo>();
        builder.Services.AddScoped<ISqlConnectionResolver, HttpHeaderSqlConnectionResolver>();

        builder.Services.AddHttpCacheHeaders(expires =>//过期模型
        {
            //过期时间
            expires.MaxAge = 120;
            //私有的
            expires.CacheLocation = CacheLocation.Private;
        },
        validation =>//验证模型
        {
            //响应过期必须重新验证
            validation.MustRevalidate = true;
        });

        builder.Services.AddControllers(setup =>
        {
            //开启请求格式不一致时，返回406状态码
            setup.ReturnHttpNotAcceptable = true;

            #region 全局设置ResponseCaching响应缓存过期时间
            setup.CacheProfiles.Add("CacheProfileKey", new CacheProfile
            {
                Duration = 120
            });
            #endregion
        })
        .AddNewtonsoftJson(setup =>
        {
            //支持json格式,json与xml顺序不同,返回优先级也不同
            setup.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        })
        .AddXmlDataContractSerializerFormatters()//支持xml格式3.x以上写法,建议使用3.x写法,支持类型更多
        .ConfigureApiBehaviorOptions(setup =>
        {
            //自定义api错误
            setup.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Type = "http://www.baidu.com",
                    Title = "出现了一个错误",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = "实体验证不通过",
                    Instance = context.HttpContext.Request.Path
                };
                problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);
                return new UnprocessableEntityObjectResult(problemDetails)
                {
                    //返回格式类型
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        #region 添加jwt验证
        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                //验证发行人
                ValidateIssuer = true,
                //发行人
                ValidIssuer = builder.Configuration.GetSection("JwtTokenManagement")["Issuer"],
                //验证订阅人
                ValidateAudience = true,
                //订阅人
                ValidAudience = builder.Configuration.GetSection("JwtTokenManagement")["Audience"],
                //验证是token否过期
                ValidateLifetime = true,
                // 是否开启签名认证
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration.GetSection("JwtTokenManagement")["Secret"])),
                //这个是缓冲过期时间，也就是说，即使我们配置了过期时间，这里也要考虑进去，过期时间+缓冲，默认好像是7分钟，你可以直接设置为0
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true,
            };
        });
        #endregion

        #region 数据库连接
        builder.Services.AddDbContext<RoutineDbContext>((serviceProvider, options) =>
        {
            var resolver = serviceProvider.GetRequiredService<ISqlConnectionResolver>();
            options.UseSqlite(resolver.GetConnection());
        });
        #endregion

        #region Identity

        #region 注册Identity服务
        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<RoutineDbContext>();
        #endregion

        #region Identity设置
        builder.Services.Configure<IdentityOptions>(options =>
        {
            // 密码设置
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 0;
            //登陆时邮箱是否要验证
            options.SignIn.RequireConfirmedAccount = false;
            // Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings.
            options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = false;
        });
        #endregion

        #region 添加授权,基于Claim授权
        builder.Services.AddAuthorization(options =>
        {
            //基于Role的策略
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
            options.AddPolicy("SystemOrAdmin", policy => policy.RequireRole("Admin", "System"));//或的关系
            options.AddPolicy("SystemAndAdmin", policy => policy.RequireRole("Admin").RequireRole("System"));//且的关系
                                                                                                             //基于Claims的策略
            options.AddPolicy("公司管理", policy => policy.RequireClaim("Companies"));
            options.AddPolicy("员工管理", policy => policy.RequireClaim("Employees", "员工列表"));
            //这样写有点sb，只不过知道可以这样写，但是jwt中不支持这种的claims没有List<string>
            options.AddPolicy("用户管理", policy => policy.RequireClaim("Users", new List<string> { "用户列表" }.First().ToString()));
            //
            options.AddPolicy("角色管理", policy => policy.RequireAssertion(context =>
            {
                if (context.User.HasClaim(x => x.Type == "Roles" && x.Value == "角色列表"))
                {
                    return true;
                }
                return false;
            }));
            //自定义Requirement
            options.AddPolicy("自定义用户管理", policy => policy.AddRequirements(
                //如果有多个Requirement，必须全部满足才能通过
                new ClaimsRequirement("Users"),
                new ActionRequirement()
            ));
        });

        builder.Services.AddSingleton<IAuthorizationHandler, ClaimsHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, CanContactHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, CanAdminHandler>();
        #endregion

        #endregion

        // 启动一个异步线程执行永久任务
        builder.Services.AddHostedService<BackgroundServiceStart>();

        #region 注册Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = $"{apiName} 接口文档——Net6",
                Description = $"{apiName} HTTP API V1",
                TermsOfService = new Uri("https://www.cnblogs.com/-zzc/"),
                Contact = new OpenApiContact
                {
                    Name = apiName,
                    Email = "zero219@foxmail.com",
                    Url = new Uri("https://www.cnblogs.com/-zzc/")
                },
                License = new OpenApiLicense
                {
                    Name = apiName,
                    Url = new Uri("https://www.cnblogs.com/-zzc/")
                }

            });
            c.OrderActionsBy(o => o.RelativePath);

            // 使用反射获取xml文件。并构造出文件的路径
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            // 启用xml注释. 该方法第二个参数启用控制器的注释，默认为false.
            c.IncludeXmlComments(xmlPath, true);

            #region Token绑定到ConfigureServices
            //开启权限
            c.OperationFilter<AddResponseHeadersFilter>();
            c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
            //在header中添加Token,传递给后台
            c.OperationFilter<SecurityRequirementsOperationFilter>();
            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "JWT授权,下框输入Bearer {token}(注:两者间有一个空格)",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            #endregion
        });
        #endregion

        #region 依赖注入,自定义帮助类
        //获取父目录
        //Directory.SetCurrentDirectory(Directory.GetParent(Directory.GetCurrentDirectory()).FullName);
        //services.AddAssembly(Path.Combine(Directory.GetCurrentDirectory(), "Dal\\bin\\Debug\\netstandard2.1\\Dal.dll"), ServiceLifetime.Scoped);
        //services.AddAssembly(Path.Combine(Directory.GetCurrentDirectory(), "Bll\\bin\\Debug\\netstandard2.1\\Bll.dll"), ServiceLifetime.Scoped);
        #endregion

        #region 注册AutoMapper
        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        #endregion

        #region 注册跨域服务
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("any", configure =>
            {
                configure.WithOrigins("http://localhost:5000/", "http://localhost:8080")//允许跨域来源,多个逗号隔开
                .AllowAnyMethod()
                .AllowAnyHeader()
                //.AllowAnyOrigin()//允许所有主机来源
                .AllowCredentials();
            });
        });
        #endregion
        // Configure
        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var db = scope.ServiceProvider.GetService<RoutineDbContext>();
                db.Database.EnsureDeleted();//删除数据库
                db.Database.EnsureCreated(); //数据库如果不存在就创建
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Database Migration Error!");
                throw new Exception(ex.Message);
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // 自定义中间件，用于捕捉全局异常
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        // 多租户中间件
        app.UseMiddleware<TenantInfoMiddleware>();
        #region ETAG缓存中间件
        app.UseHttpCacheHeaders();
        #endregion

        #region 响应缓存中间件
        app.UseResponseCaching();
        #endregion

        #region Swagger
        //启用Swagger中间件
        app.UseSwagger();

        //配置SwaggerUI
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"{apiName} V1");

            //路径配置，设置为空，表示直接访问该文件，设置根节点访问 删掉这句
            //c.RoutePrefix = string.Empty;
        });
        #endregion

        //路由中间件
        app.UseRouting();

        #region 跨域中间件
        app.UseCors("any");
        #endregion

        //认证中间件
        app.UseAuthentication();
        //授权中间件
        app.UseAuthorization();
        //端点
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        await app.RunAsync();
    }
}
