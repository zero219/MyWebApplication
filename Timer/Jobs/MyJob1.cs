using Quartz;
using System;
using System.Threading.Tasks;

namespace Timer.Jobs
{
    public class MyJob1 : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("启动Job1！！！！");
            return Task.CompletedTask;
        }
    }
}
