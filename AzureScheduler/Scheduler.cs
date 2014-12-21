using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Base64Url;
using NCrontab;

namespace AzureScheduler
{
    public static class Scheduler
    {
        public static Task ProcessAsync(TimeSpan leaseTime)
        {
            var context = new SchedulerContext();
            var jobItems = context.JobItems.Query().Execute();

            var now = DateTime.UtcNow;
            var tasks = jobItems.Where(jobItem => jobItem.LeaseExpire.GetValueOrDefault() <= now)
                                .TakeWhile(jobItem => context.JobItems.Lease(jobItem, leaseTime))
                                .Select(jobItem => doJobAsync(context, jobItem, leaseTime));
            return Task.WhenAll(tasks);
        }

        static async Task doJobAsync(SchedulerContext context, JobItem jobItem, TimeSpan timeout)
        {
            var lastRun = jobItem.LastRun.GetValueOrDefault();
            var cron = CrontabSchedule.TryParse(jobItem.Cron);
            if (cron == null)
            {
                if (jobItem.LastRunResult != "CRON ERROR")
                    postJob(context, jobItem, -1, "CRON ERROR", 0);
                return;
            }

            var start = DateTime.UtcNow;
            var nextRun = cron.GetNextOccurrence(lastRun);
            if (nextRun > start)
                return;

            jobItem.NextRun = cron.GetNextOccurrence(start);

            var client = createHttpClient(timeout);
            var response = await client.GetAsync(jobItem.Url);
            var content = string.Empty;
            if (response.Content != null)
                content = await response.Content.ReadAsStringAsync();

            postJob(context, jobItem, (int)response.StatusCode, content, (int)(DateTime.UtcNow - start).TotalSeconds);
        }

        static void postJob(SchedulerContext context, JobItem jobItem, int statusCode, string result, int runTime)
        {
            var jobLog = new JobLog
            {
                PartitionKey = TimeId.NewSortableId(),
                RowKey = string.Empty,
                JobGroup = jobItem.PartitionKey,
                JobName = jobItem.RowKey,
                StatusCode = statusCode,
                Result = result,
                RunTime = runTime,
            };
            context.JobLogs.Insert(jobLog, true);
            jobItem.LastRun = DateTime.UtcNow;
            jobItem.LastRunResult = jobLog.Result;
            jobItem.LastRunStatusCode = jobLog.StatusCode;
            if (jobLog.StatusCode >= 200 && jobLog.StatusCode < 400)
                jobItem.LastSuccessRun = jobItem.LastRun;
            context.JobItems.Replace(jobItem);
        }

        static HttpClient createHttpClient(TimeSpan timeout)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };
            var client = new HttpClient(handler)
            {
                Timeout = timeout,
            };
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

            return client;
        }
    }
}