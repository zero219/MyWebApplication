using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Ioc;
using Entity.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Marvin.Cache.Headers;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Swashbuckle.AspNetCore.Filters;
using Autofac;
using System.Runtime.Loader;
using Common.Redis;

namespace Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string ApiName { get; set; } = "RESTfull";
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("执行顺序ConfigureServices");

            #region 响应缓存,框架自带的,会被ETAG覆盖,然并卵.
            //框架自带当做例子
            services.AddResponseCaching();
            #endregion

            #region 全局注册ETAG缓存
            //ETAG会覆盖掉框架中的缓存
            services.AddHttpCacheHeaders(expires =>//过期模型
            {
                //过期时间
                expires.MaxAge = 90;
                //私有的
                expires.CacheLocation = CacheLocation.Private;
            },
            validation =>//验证模型
            {
                //响应过期必须重新验证
                validation.MustRevalidate = true;
            });
            #endregion

            services.AddControllers(setup =>
            {
                //开启请求格式不一致时，返回406状态码
                setup.ReturnHttpNotAcceptable = true;

                #region 全局设置响应缓存过期时间,会被ETAG覆盖,然并卵.
                setup.CacheProfiles.Add("CacheProfileKey", new CacheProfile
                {
                    Duration = 120
                });
                #endregion

                #region xml2.0写法
                //默认是json格式，也支持xml格式
                //setup.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                //设置默认格式XML
                //setup.OutputFormatters.Insert(0, new XmlDataContractSerializerOutputFormatter());
                #endregion

            }).AddNewtonsoftJson(setup =>
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
                        Detail = "请看详细信息",
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

            #region 全局注册Aceept:application/vdn.company.hateoas+json的Media Type
            services.Configure<MvcOptions>(config =>
            {
                var newtonSoftJsonOutputFormatter = config.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>()?.FirstOrDefault();
                newtonSoftJsonOutputFormatter?.SupportedMediaTypes.Add("application/vnd.company.friendly+json");

            });
            #endregion

            #region 添加jwt验证
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(y =>
            {
                y.TokenValidationParameters = new TokenValidationParameters
                {
                    // 是否开启签名认证
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("JwtTokenManagement")["Secret"])),
                    //验证发行人
                    ValidateIssuer = true,
                    //发行人
                    ValidIssuer = Configuration.GetSection("JwtTokenManagement")["Issuer"],
                    //验证订阅人
                    ValidateAudience = true,
                    //订阅人
                    ValidAudience = Configuration.GetSection("JwtTokenManagement")["Audience"],
                    //验证是token否过期
                    ValidateLifetime = true,
                    //这个是缓冲过期时间，也就是说，即使我们配置了过期时间，这里也要考虑进去，过期时间+缓冲，默认好像是7分钟，你可以直接设置为0
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true,
                };
            });
            #endregion

            #region 添加授权
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
                options.AddPolicy("System", policy => policy.RequireRole("System").Build());
                options.AddPolicy("SystemOrAdmin", policy => policy.RequireRole("Admin", "System"));//或的关系
                options.AddPolicy("SystemAndAdmin", policy => policy.RequireRole("Admin").RequireRole("System"));//且的关系
            });
            #endregion

            #region 数据库连接
            services.AddDbContext<RoutineDbContext>(options =>
            {
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection"));
            });
            #endregion

            #region 注册Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = $"{ApiName} 接口文档——Net5",
                    Description = $"{ApiName} HTTP API V1",
                    TermsOfService = new Uri("https://www.cnblogs.com/-zzc/"),
                    Contact = new OpenApiContact
                    {
                        Name = ApiName,
                        Email = "zero219@foxmail.com",
                        Url = new Uri("https://www.cnblogs.com/-zzc/")
                    },
                    License = new OpenApiLicense
                    {
                        Name = ApiName,
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
                    Type = SecuritySchemeType.ApiKey
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
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            #endregion
        }

        #region Autofac容器
        public void ConfigureContainer(ContainerBuilder builder)
        {
            Console.WriteLine("执行顺序ConfigureContainer");
            /*
             * InstancePerDependency等于AddTransient
             * InstancePerLifetimeScope等于AddScoped
             * SingleInstance等于AddSingleton
             */

            Assembly assemblyDal = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Dal"));
            builder.RegisterTypes(assemblyDal.GetTypes()).AsImplementedInterfaces().InstancePerLifetimeScope();

            Assembly assemblyBll = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Bll"));
            builder.RegisterTypes(assemblyBll.GetTypes()).AsImplementedInterfaces().InstancePerLifetimeScope();

            //redis缓存
            var section = Configuration.GetSection("Redis:DefaultConnection").Value;
            builder.Register(x => new RedisHelpers(section, "")).SingleInstance();
        }
        #endregion


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Console.WriteLine("执行顺序Configure");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //处理服务器故障
                app.UseExceptionHandler(appBuilder =>
                {
                    appBuilder.Run(async context =>
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("Unexpected Error!");
                    });
                });
            }

            #region Swagger
            //启用Swagger中间件
            app.UseSwagger();

            //配置SwaggerUI
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"{ApiName} V1");

                //路径配置，设置为空，表示直接访问该文件，设置根节点访问 删掉这句
                //c.RoutePrefix = string.Empty;
            });
            #endregion

            #region 响应缓存中间件,会被ETAG覆盖,然并卵.
            //此中间件没有验证ETAG,只是实验例子
            app.UseResponseCaching();
            #endregion

            #region ETAG缓存中间件
            app.UseHttpCacheHeaders();
            #endregion
            //路由中间件
            app.UseRouting();
            //认证中间件
            app.UseAuthentication();
            //授权中间件
            app.UseAuthorization();
            //端点
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
