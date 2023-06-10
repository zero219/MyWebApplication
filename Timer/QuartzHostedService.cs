using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Timer.Jobs;
using Timer.Models;

namespace Timer
{
    public class QuartzHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IConfiguration _configuration;

        public QuartzHostedService(ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory,
            IConfiguration configuration)
        {
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            scheduler.JobFactory = _jobFactory;
            // 读取配置文件
            var jobs = _configuration.GetSection("QuartzConfig:Jobs").Get<List<JobConfig>>();
            foreach (var jobConfig in jobs)
            {
                var jobType = Type.GetType(jobConfig.Type);
                var jobDetail = JobBuilder.Create(jobType)
                    .WithIdentity(jobConfig.Name, "group")
                    .Build();

                var trigger = CreateTrigger(jobConfig.Name, jobConfig.Trigger);
                await scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);
            }
            await scheduler.Start(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 选择触发器
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="triggerConfig"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private ITrigger CreateTrigger(string jobName, TriggerConfig triggerConfig)
        {
            switch (triggerConfig.Type.ToLower())
            {
                case "simple":
                    return CreateSimpleTrigger(jobName, triggerConfig.Properties);
                case "cron":
                    return CreateCronTrigger(jobName, triggerConfig.Properties);
                default:
                    throw new NotSupportedException($"不支持触发器类型'{triggerConfig.Type}'");
            }
        }

        /// <summary>
        /// 创建触发器
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        private ITrigger CreateSimpleTrigger(string jobName, Dictionary<string, string> properties)
        {
            var intervalInSeconds = int.Parse(properties["IntervalInSeconds"]);
            var repeatCount = int.Parse(properties["RepeatCount"]);

            return TriggerBuilder.Create()
                .WithIdentity(jobName + "Trigger", "group")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(intervalInSeconds)
                    .WithRepeatCount(repeatCount))
                .Build();
        }

        /// <summary>
        /// 创建Cron触发器
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        private ITrigger CreateCronTrigger(string jobName, Dictionary<string, string> properties)
        {
            var cronExpression = properties["Expression"];

            return TriggerBuilder.Create()
                .WithIdentity(jobName + "Trigger", "group")
                .StartNow()
                .WithCronSchedule(cronExpression)
                .Build();
        }
    }
}
