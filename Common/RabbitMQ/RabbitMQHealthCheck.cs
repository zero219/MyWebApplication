using Common.RabbitMQ;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.RabbitMQ
{
    public class RabbitMQHealthCheck : IHealthCheck
    {
        private readonly RabbitMQHelper _rabbitMQHelper;

        public RabbitMQHealthCheck(RabbitMQHelper rabbitMQHelper)
        {
            _rabbitMQHelper = rabbitMQHelper;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isHealthy = _rabbitMQHelper.IsConnected && _rabbitMQHelper.IsChannelOpen;

                return Task.FromResult(isHealthy ? HealthCheckResult.Healthy("RabbitMQ连接正常") : HealthCheckResult.Unhealthy("RabbitMQ连接异常"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("RabbitMQ健康检查失败", ex));
            }
        }
    }
}