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

        //Autofac����
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            Console.WriteLine("ִ��˳��ConfigureContainer");
            /*
             * InstancePerDependency����AddTransient
             * InstancePerLifetimeScope����AddScoped
             * SingleInstance����AddSingleton
             */

            Assembly assemblyDal = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Dal"));
            containerBuilder.RegisterTypes(assemblyDal.GetTypes()).AsImplementedInterfaces().InstancePerLifetimeScope();

            Assembly assemblyBll = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Bll"));
            containerBuilder.RegisterTypes(assemblyBll.GetTypes()).AsImplementedInterfaces().InstancePerLifetimeScope();

            #region Redis����ע��
            var conn = builder.Configuration.GetSection("Redis:DefaultConnection").Value;
            var instanceName = builder.Configuration.GetSection("Redis:InstanceName").Value;
            var defaultDB = int.Parse(builder.Configuration.GetSection("Redis:DefaultDB").Value.ToString() ?? "0");
            containerBuilder.Register(x => new RedisCacheManager(conn, instanceName, defaultDB)).As<IRedisCacheManager>().SingleInstance();
            #endregion

        });

        // ʹ��log4net
        builder.Host.ConfigureLogging((hostBuidleContext, loggingBuilder) =>
        {
            //���˵�System��Microsoft��ͷ�������ռ��µ���������ľ��漶��һ�µ���־
            loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
            loggingBuilder.AddFilter("System", LogLevel.Warning);
            loggingBuilder.AddLog4Net();
        });

        // ע���Զ����쳣��׽�м��
        builder.Services.AddTransient<ExceptionHandlingMiddleware>();

        // ���⻧
        builder.Services.AddScoped<TenantInfo>();
        builder.Services.AddScoped<ISqlConnectionResolver, HttpHeaderSqlConnectionResolver>();

        builder.Services.AddHttpCacheHeaders(expires =>//����ģ��
        {
            //����ʱ��
            expires.MaxAge = 120;
            //˽�е�
            expires.CacheLocation = CacheLocation.Private;
        },
        validation =>//��֤ģ��
        {
            //��Ӧ���ڱ���������֤
            validation.MustRevalidate = true;
        });

        builder.Services.AddControllers(setup =>
        {
            //���������ʽ��һ��ʱ������406״̬��
            setup.ReturnHttpNotAcceptable = true;

            #region ȫ������ResponseCaching��Ӧ�������ʱ��
            setup.CacheProfiles.Add("CacheProfileKey", new CacheProfile
            {
                Duration = 120
            });
            #endregion
        })
        .AddNewtonsoftJson(setup =>
        {
            //֧��json��ʽ,json��xml˳��ͬ,�������ȼ�Ҳ��ͬ
            setup.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        })
        .AddXmlDataContractSerializerFormatters()//֧��xml��ʽ3.x����д��,����ʹ��3.xд��,֧�����͸���
        .ConfigureApiBehaviorOptions(setup =>
        {
            //�Զ���api����
            setup.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Type = "http://www.baidu.com",
                    Title = "������һ������",
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Detail = "ʵ����֤��ͨ��",
                    Instance = context.HttpContext.Request.Path
                };
                problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);
                return new UnprocessableEntityObjectResult(problemDetails)
                {
                    //���ظ�ʽ����
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        #region ���jwt��֤
        builder.Services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                //��֤������
                ValidateIssuer = true,
                //������
                ValidIssuer = builder.Configuration.GetSection("JwtTokenManagement")["Issuer"],
                //��֤������
                ValidateAudience = true,
                //������
                ValidAudience = builder.Configuration.GetSection("JwtTokenManagement")["Audience"],
                //��֤��token�����
                ValidateLifetime = true,
                // �Ƿ���ǩ����֤
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration.GetSection("JwtTokenManagement")["Secret"])),
                //����ǻ������ʱ�䣬Ҳ����˵����ʹ���������˹���ʱ�䣬����ҲҪ���ǽ�ȥ������ʱ��+���壬Ĭ�Ϻ�����7���ӣ������ֱ������Ϊ0
                ClockSkew = TimeSpan.Zero,
                RequireExpirationTime = true,
            };
        });
        #endregion

        #region ���ݿ�����
        builder.Services.AddDbContext<RoutineDbContext>((serviceProvider, options) =>
        {
            var resolver = serviceProvider.GetRequiredService<ISqlConnectionResolver>();
            options.UseSqlite(resolver.GetConnection());
        });
        #endregion

        #region Identity

        #region ע��Identity����
        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<RoutineDbContext>();
        #endregion

        #region Identity����
        builder.Services.Configure<IdentityOptions>(options =>
        {
            // ��������
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 0;
            //��½ʱ�����Ƿ�Ҫ��֤
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

        #region �����Ȩ,����Claim��Ȩ
        builder.Services.AddAuthorization(options =>
        {
            //����Role�Ĳ���
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
            options.AddPolicy("SystemOrAdmin", policy => policy.RequireRole("Admin", "System"));//��Ĺ�ϵ
            options.AddPolicy("SystemAndAdmin", policy => policy.RequireRole("Admin").RequireRole("System"));//�ҵĹ�ϵ
                                                                                                             //����Claims�Ĳ���
            options.AddPolicy("��˾����", policy => policy.RequireClaim("Companies"));
            options.AddPolicy("Ա������", policy => policy.RequireClaim("Employees", "Ա���б�"));
            //����д�е�sb��ֻ����֪����������д������jwt�в�֧�����ֵ�claimsû��List<string>
            options.AddPolicy("�û�����", policy => policy.RequireClaim("Users", new List<string> { "�û��б�" }.First().ToString()));
            //
            options.AddPolicy("��ɫ����", policy => policy.RequireAssertion(context =>
            {
                if (context.User.HasClaim(x => x.Type == "Roles" && x.Value == "��ɫ�б�"))
                {
                    return true;
                }
                return false;
            }));
            //�Զ���Requirement
            options.AddPolicy("�Զ����û�����", policy => policy.AddRequirements(
                //����ж��Requirement������ȫ���������ͨ��
                new ClaimsRequirement("Users"),
                new ActionRequirement()
            ));
        });

        builder.Services.AddSingleton<IAuthorizationHandler, ClaimsHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, CanContactHandler>();
        builder.Services.AddSingleton<IAuthorizationHandler, CanAdminHandler>();
        #endregion

        #endregion

        // ����һ���첽�߳�ִ����������
        builder.Services.AddHostedService<BackgroundServiceStart>();

        #region ע��Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = $"{apiName} �ӿ��ĵ�����Net6",
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

            // ʹ�÷����ȡxml�ļ�����������ļ���·��
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            // ����xmlע��. �÷����ڶ����������ÿ�������ע�ͣ�Ĭ��Ϊfalse.
            c.IncludeXmlComments(xmlPath, true);

            #region Token�󶨵�ConfigureServices
            //����Ȩ��
            c.OperationFilter<AddResponseHeadersFilter>();
            c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
            //��header�����Token,���ݸ���̨
            c.OperationFilter<SecurityRequirementsOperationFilter>();
            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "JWT��Ȩ,�¿�����Bearer {token}(ע:���߼���һ���ո�)",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            #endregion
        });
        #endregion

        #region ����ע��,�Զ��������
        //��ȡ��Ŀ¼
        //Directory.SetCurrentDirectory(Directory.GetParent(Directory.GetCurrentDirectory()).FullName);
        //services.AddAssembly(Path.Combine(Directory.GetCurrentDirectory(), "Dal\\bin\\Debug\\netstandard2.1\\Dal.dll"), ServiceLifetime.Scoped);
        //services.AddAssembly(Path.Combine(Directory.GetCurrentDirectory(), "Bll\\bin\\Debug\\netstandard2.1\\Bll.dll"), ServiceLifetime.Scoped);
        #endregion

        #region ע��AutoMapper
        builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        #endregion

        #region ע��������
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("any", configure =>
            {
                configure.WithOrigins("http://localhost:5000/", "http://localhost:8080")//���������Դ,������Ÿ���
                .AllowAnyMethod()
                .AllowAnyHeader()
                //.AllowAnyOrigin()//��������������Դ
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
                db.Database.EnsureDeleted();//ɾ�����ݿ�
                db.Database.EnsureCreated(); //���ݿ���������ھʹ���
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

        // �Զ����м�������ڲ�׽ȫ���쳣
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        // ���⻧�м��
        app.UseMiddleware<TenantInfoMiddleware>();
        #region ETAG�����м��
        app.UseHttpCacheHeaders();
        #endregion

        #region ��Ӧ�����м��
        app.UseResponseCaching();
        #endregion

        #region Swagger
        //����Swagger�м��
        app.UseSwagger();

        //����SwaggerUI
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"{apiName} V1");

            //·�����ã�����Ϊ�գ���ʾֱ�ӷ��ʸ��ļ������ø��ڵ���� ɾ�����
            //c.RoutePrefix = string.Empty;
        });
        #endregion

        //·���м��
        app.UseRouting();

        #region �����м��
        app.UseCors("any");
        #endregion

        //��֤�м��
        app.UseAuthentication();
        //��Ȩ�м��
        app.UseAuthorization();
        //�˵�
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        await app.RunAsync();
    }
}
