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
using Entity.Models.IdentityModels;
using Microsoft.AspNetCore.Identity;
using Common.IdentityAuth;
using Microsoft.AspNetCore.Authorization;
using Bll;
using Common.Middleware;

namespace Api
{
    /// <summary>
    /// ������Startup
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ApiName { get; set; } = "RESTfull";
        public IConfiguration _configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("ִ��˳��ConfigureServices");

            // ע���Զ����쳣��׽�м��
            services.AddTransient<ExceptionHandlingMiddleware>();

            #region ע����Ӧ����
            services.AddResponseCaching();
            #endregion

            #region ȫ��ע��ETAG����
            services.AddHttpCacheHeaders(expires =>//����ģ��
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
            #endregion

            services.AddControllers(setup =>
            {
                //���������ʽ��һ��ʱ������406״̬��
                setup.ReturnHttpNotAcceptable = true;

                #region ȫ������ResponseCaching��Ӧ�������ʱ��
                setup.CacheProfiles.Add("CacheProfileKey", new CacheProfile
                {
                    Duration = 120
                });
                #endregion
            }).AddNewtonsoftJson(setup =>
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
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    //��֤������
                    ValidateIssuer = true,
                    //������
                    ValidIssuer = _configuration.GetSection("JwtTokenManagement")["Issuer"],
                    //��֤������
                    ValidateAudience = true,
                    //������
                    ValidAudience = _configuration.GetSection("JwtTokenManagement")["Audience"],
                    //��֤��token�����
                    ValidateLifetime = true,
                    // �Ƿ���ǩ����֤
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration.GetSection("JwtTokenManagement")["Secret"])),
                    //����ǻ������ʱ�䣬Ҳ����˵����ʹ���������˹���ʱ�䣬����ҲҪ���ǽ�ȥ������ʱ��+���壬Ĭ�Ϻ�����7���ӣ������ֱ������Ϊ0
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true,
                };
            });
            #endregion

            #region ���ݿ�����
            services.AddDbContext<RoutineDbContext>(options =>
            {
                options.UseSqlite(_configuration.GetConnectionString("DefaultConnection"));
            });
            #endregion

            #region Identity

            #region ע��Identity����
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<RoutineDbContext>();
            #endregion

            #region Identity����
            services.Configure<IdentityOptions>(options =>
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
            services.AddAuthorization(options =>
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

            services.AddSingleton<IAuthorizationHandler, ClaimsHandler>();
            services.AddSingleton<IAuthorizationHandler, CanContactHandler>();
            services.AddSingleton<IAuthorizationHandler, CanAdminHandler>();
            #endregion

            #endregion

            // ����һ���첽�߳�ִ����������
            services.AddHostedService<BackgroundServiceStart>();

            #region ע��Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = $"{ApiName} �ӿ��ĵ�����Net5",
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
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            #endregion

            #region ע��������
            services.AddCors(options =>
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
        }

        #region Autofac����
        public void ConfigureContainer(ContainerBuilder builder)
        {
            Console.WriteLine("ִ��˳��ConfigureContainer");
            /*
             * InstancePerDependency����AddTransient
             * InstancePerLifetimeScope����AddScoped
             * SingleInstance����AddSingleton
             */

            Assembly assemblyDal = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Dal"));
            builder.RegisterTypes(assemblyDal.GetTypes()).AsImplementedInterfaces().InstancePerLifetimeScope();

            Assembly assemblyBll = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName("Bll"));
            builder.RegisterTypes(assemblyBll.GetTypes()).AsImplementedInterfaces().InstancePerLifetimeScope();

            #region Redis����ע��
            var conn = _configuration.GetSection("Redis:DefaultConnection").Value;
            var instanceName = _configuration.GetSection("Redis:InstanceName").Value;
            var defaultDB = int.Parse(_configuration.GetSection("Redis:DefaultDB").Value.ToString() ?? "0");
            builder.Register(x => new RedisCacheManager(conn, instanceName, defaultDB)).As<IRedisCacheManager>().SingleInstance();
            #endregion

        }
        #endregion


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            Console.WriteLine("ִ��˳��Configure");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // �Զ����м�������ڲ�׽ȫ���쳣
            app.UseMiddleware<ExceptionHandlingMiddleware>();

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
                c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"{ApiName} V1");

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
        }
    }
}
