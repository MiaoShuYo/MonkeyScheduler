using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;

var repo = new InMemoryTaskRepository();
var executor = new SimulatedTaskExecutor();
var scheduler = new Scheduler(repo, executor);

// 每5秒执行一次
repo.AddTask(new ScheduledTask
{
    Name = "每5秒执行的任务T1",
    CronExpression = "*/5 * * * * *",
    NextRunTime = DateTime.UtcNow
});

// 或者每5分钟执行一次
repo.AddTask(new ScheduledTask
{
    Name = "每5分钟执行的任务T2",
    CronExpression = "*/5 * * * *",
    NextRunTime = DateTime.UtcNow
});

scheduler.Start();

Console.WriteLine("调度器已启动。按回车键退出。");
Console.ReadLine();
//scheduler.Stop();
