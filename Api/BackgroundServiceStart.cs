using IBll;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bll
{
    /*
     * BackgroundService类继承IHostedService
     * BackgroundService 是 IHostedService的一个简单实现，
     * 内部IHostedService 的StartAsync调用了ExecuteAsync”，本质上就是使用了 IHostedService
     * StartAsync 应仅限于短期任务，因为托管服务是按顺序运行的，在 StartAsync 运行完成之前不会启动其他服务。 长期任务应放置在 ExecuteAsync 中。
     */
    public class BackgroundServiceStart : BackgroundService
    {
        private readonly IServiceProvider _services;

        public BackgroundServiceStart(IServiceProvider services)
        {
            _services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();

            var taskWorkService = scope.ServiceProvider.GetRequiredService<ISeckillVoucherService>();

            await taskWorkService.TaskWorkAsync(stoppingToken);
        }
    }
}
