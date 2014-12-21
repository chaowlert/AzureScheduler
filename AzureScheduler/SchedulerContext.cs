using AzureStorageExtensions;

namespace AzureScheduler
{
    public class SchedulerContext : BaseCloudContext
    {
        public SchedulerContext() { }
        public SchedulerContext(string connectionName) : base(connectionName) { }

        public CloudTable<JobItem> JobItems { get; set; } 

        [Setting(Period = Period.Month)]
        public CloudTable<JobLog> JobLogs { get; set; } 
    }
}
