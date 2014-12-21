using System;
using AzureStorageExtensions;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureScheduler
{
    public class JobItem : TableEntity, ILeasable
    {
        //PartitionKey: name
        //RowKey: (none)

        public string Url { get; set; }
        public string Cron { get; set; }
        public DateTime? LastRun { get; set; }
        public DateTime? LastSuccessRun { get; set; }
        public int? LastRunStatusCode { get; set; }
        public string LastRunResult { get; set; }
        public DateTime? NextRun { get; set; }
        public DateTime? LeaseExpire { get; set; }
    }
}
