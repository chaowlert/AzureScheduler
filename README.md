AzureScheduler
==============

If you don't want to pay for Azure Scheduler Service, this is for create your own scheduler.

####How to set up
1. This library use [Azure Storage Extensions]( https://github.com/chaowlert/AzureStorageExtensions). So you can add new schedule by insert new job item using `SchedulerContext`. 
2. For CronJob, I use [NCronTab](https://code.google.com/p/ncrontab/), you can set up interval as you want.
3. To activate schedule, call `Scheduler.ProcessAsync(leaseTime)`. LeaseTime will prevent schedule from multiple call.
