using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quartz;

namespace backupwordservice.LogUtils
{
    public static class ServiceCollectionQuartzConfiguratorExtensions
    {
        public static void AddJobAndTrigger<T>(this IServiceCollectionQuartzConfigurator quartz, IConfiguration configuration) where T :IJob
        {
            string jobName = typeof(T).Name;
            var configKey = $"backupJob";
            var jobCornString = configuration[configKey];

            if(string.IsNullOrEmpty(jobCornString))
            {
                throw new Exception($"{configKey} is not found in configuration");
            }

            var jobKey = new JobKey(jobName);
            quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));
            quartz.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity(jobName + "-trigger")
                .WithCronSchedule(jobCornString));
        }
    }
}