using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;

namespace Timer.Quartz
{
    public class QuartzJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public QuartzJobFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var jobType = bundle.JobDetail.JobType;
            bool flag = jobType.Namespace.StartsWith("Timer.Jobs");
            if (flag)
            {
                return (IJob)_serviceProvider.GetRequiredService(jobType);
            }
            else
            {
                return (IJob)Activator.CreateInstance(jobType);
            }
        }

        public void ReturnJob(IJob job)
        {

        }
    }

}
