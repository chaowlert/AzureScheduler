AzureScheduler
==============

This is for creating your own scheduler on Azure.

#### How to set up
1. This library use [Azure Storage Extensions]( https://github.com/chaowlert/AzureStorageExtensions). So you can add new schedule by insert new job item using `SchedulerContext`. 
2. Add job to `JobItems` table.  
`PartitionKey` is job name.  
`RowKey` must be empty string.  
`Url` is url to trigger.  
`Cron` is Cron expression, I use [NCronTab](https://code.google.com/p/ncrontab/).  
`LeaseMinutes` is lease time for the job. This is to prevent firing job at the same time.  
`AlwaysRun` if this is true, scheduler will ignore Cron and run every time we call `Scheduler.ProcessAsync()`, and it will also not produce any log.  
3. To activate schedule, call `Scheduler.ProcessAsync()`.

#### Example
In `web.config`, add following to connectionStrings
```
<connectionStrings>
  <add name="SchedulerContext" connectionString="DefaultEndpointsProtocol=https;AccountName={name};AccountKey={key}" />
</connectionStrings>
```

Add job item.
- PartitionKey: `daily_report`
- RowKey: (empty string)
- Url: `http://myweb.com/run_daily_report`
- Cron: `0 21 * * *` (everyday on 9pm UTC)
- LeaseMinutes: `30` (not allow to run this task within 30 minutes)
- AlwaysRun: `false`
 
To fire job, run `Scheduler.ProcessAsync()`.
