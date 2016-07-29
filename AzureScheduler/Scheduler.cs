using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Base64Url;
using NCrontab;

namespace AzureScheduler
{
    public static class Scheduler
    {
        public static Task ProcessAsync(string jobName = null)
        {
            var context = new SchedulerContext();
            var query = context.JobItems.Query().AsQueryable();
            if (jobName != null)
                query = from item in query
                        where item.PartitionKey == jobName &&
                              item.RowKey == string.Empty
                        select item;
            var jobItems = query.AsEnumerable();

            var start = DateTime.UtcNow;
            var tasks = jobItems.Where(jobItem => canRun(context, jobItem, start))
                                .TakeWhile(jobItem => context.JobItems.Lease(jobItem, TimeSpan.FromMinutes(jobItem.LeaseMinutes)))
                                .Select(jobItem => doJobAsync(context, jobItem, start));
            
            return Task.WhenAll(tasks);
        }

        static bool canRun(SchedulerContext context, JobItem jobItem, DateTime start)
        {
            if (jobItem.LeaseExpire.GetValueOrDefault() > start)
                return false;
            if (jobItem.AlwaysRun)
                return true;

            var lastRun = jobItem.LastRun.GetValueOrDefault();
            var cron = CrontabSchedule.TryParse(jobItem.Cron);
            if (cron == null)
            {
                if (jobItem.LastRunResult != "CRON ERROR")
                    postJob(context, jobItem, -1, "CRON ERROR", 0);
                return false;
            }

            var nextRun = cron.GetNextOccurrence(lastRun);
            if (nextRun > start)
                return false;

            jobItem.NextRun = cron.GetNextOccurrence(start);
            return true;
        }

        static async Task doJobAsync(SchedulerContext context, JobItem jobItem, DateTime start)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(jobItem.LeaseMinutes));
            var response = await _client.GetAsync(jobItem.Url, cts.Token);
            var content = string.Empty;
            if (response.Content != null)
                content = await response.Content.ReadAsStringAsync();

            if (!jobItem.AlwaysRun)
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

        private static readonly HttpClient _client = createHttpClient();
        static HttpClient createHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };
            var client = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan,
            };
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

            return client;
        }
    }
}