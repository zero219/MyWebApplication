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
            Console.WriteLine("ִ��˳��ConfigureServices");

            #region ��Ӧ����,����Դ���,�ᱻETAG����,Ȼ����.
            //����Դ���������
            services.AddResponseCaching();
            #endregion

            #region ȫ��ע��ETAG����
            //ETAG�Ḳ�ǵ�����еĻ���
            services.AddHttpCacheHeaders(expires =>//����ģ��
            {
                //����ʱ��
                expires.MaxAge = 90;
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

                #region ȫ��������Ӧ�������ʱ��,�ᱻETAG����,Ȼ����.
                setup.CacheProfiles.Add("CacheProfileKey", new CacheProfile
                {
                    Duration = 120
                });
                #endregion

                #region xml2.0д��
                //Ĭ����json��ʽ��Ҳ֧��xml��ʽ
                //setup.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                //����Ĭ�ϸ�ʽXML
                //setup.OutputFormatters.Insert(0, new XmlDataContractSerializerOutputFormatter());
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
                        Detail = "�뿴��ϸ��Ϣ",
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

            #region ȫ��ע��Aceept:application/vdn.company.hateoas+json��Media Type
            services.Configure<MvcOptions>(config =>
            {
                var newtonSoftJsonOutputFormatter = config.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>()?.FirstOrDefault();
                newtonSoftJsonOutputFormatter?.SupportedMediaTypes.Add("application/vnd.company.friendly+json");

            });
            #endregion

            #region ���jwt��֤
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(y =>
            {
                y.TokenValidationParameters = new TokenValidationParameters
                {
                    // �Ƿ���ǩ����֤
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("JwtTokenManagement")["Secret"])),
                    //��֤������
                    ValidateIssuer = true,
                    //������
                    ValidIssuer = Configuration.GetSection("JwtTokenManagement")["Issuer"],
                    //��֤������
                    ValidateAudience = true,
                    //������
                    ValidAudience = Configuration.GetSection("JwtTokenManagement")["Audience"],
                    //��֤��token�����
                    ValidateLifetime = true,
                    //����ǻ������ʱ�䣬Ҳ����˵����ʹ���������˹���ʱ�䣬����ҲҪ���ǽ�ȥ������ʱ��+���壬Ĭ�Ϻ�����7���ӣ������ֱ������Ϊ0
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true,
                };
            });
            #endregion

            #region �����Ȩ
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
                options.AddPolicy("System", policy => policy.RequireRole("System").Build());
                options.AddPolicy("SystemOrAdmin", policy => policy.RequireRole("Admin", "System"));//��Ĺ�ϵ
                options.AddPolicy("SystemAndAdmin", policy => policy.RequireRole("Admin").RequireRole("System"));//�ҵĹ�ϵ
            });
            #endregion

            #region ���ݿ�����
            services.AddDbContext<RoutineDbContext>(options =>
            {
                options.UseSqlite(Configuration.GetConnectionString("DefaultConnection"));
            });
            #endregion

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
                    Type = SecuritySchemeType.ApiKey
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

            //redis����
            var section = Configuration.GetSection("Redis:DefaultConnection").Value;
            builder.Register(x => new RedisHelpers(section, "")).SingleInstance();
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
            else
            {
                //�������������
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

            #region ��Ӧ�����м��,�ᱻETAG����,Ȼ����.
            //���м��û����֤ETAG,ֻ��ʵ������
            app.UseResponseCaching();
            #endregion

            #region ETAG�����м��
            app.UseHttpCacheHeaders();
            #endregion
            //·���м��
            app.UseRouting();
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
