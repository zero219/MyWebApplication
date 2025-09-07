using Entity.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Middleware
{
    public class TenantInfoMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantInfoMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var tenantInfo = context.RequestServices.GetRequiredService<TenantInfo>();
            var tenantName = context.Request.Headers["Tenant"];

            if (string.IsNullOrEmpty(tenantName))
                tenantName = "DefaultConnection";

            tenantInfo.ConfigName = tenantName;

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
    public class TenantInfo
    {
        /// <summary>
        /// 配置文件名称
        /// </summary>
        public string ConfigName { get; set; } = "DefaultConnection";
    }


    public interface ISqlConnectionResolver
    {
        string GetConnection();
    }

    public class HttpHeaderSqlConnectionResolver : ISqlConnectionResolver
    {
        private readonly TenantInfo _tenantInfo;
        private readonly IConfiguration _configuration;
        public HttpHeaderSqlConnectionResolver(TenantInfo tenantInfo, IConfiguration configuration)
        {
            this._tenantInfo = tenantInfo;
            this._configuration = configuration;
        }
        public string GetConnection()
        {
            var connectionString = _configuration.GetConnectionString(this._tenantInfo.ConfigName);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new NullReferenceException("找不到连接...");
            }
            return connectionString;
        }
    }
}
