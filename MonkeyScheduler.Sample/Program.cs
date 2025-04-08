using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;

var repo = new InMemoryTaskRepository();
var executor = new SimulatedTaskExecutor();
var scheduler = new Scheduler(repo, executor);

// 添加两个示例任务
repo.AddTask(new ScheduledTask
{
    Name = "Task A",
    CronExpression = "*/5 * * * * *", // 每5秒
    NextRunTime = DateTime.UtcNow
});

repo.AddTask(new ScheduledTask
{
    Name = "Task B",
    CronExpression = "*/10 * * * * *", // 每10秒
    NextRunTime = DateTime.UtcNow
});

scheduler.Start();

Console.WriteLine("调度器已启动。按回车键退出。");
Console.ReadLine();
scheduler.Stop();
