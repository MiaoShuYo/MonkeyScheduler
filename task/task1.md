好！我们来规划并搭建 **第一阶段（单节点 MVP）** 的核心功能。目标是：

---

## ✅ **阶段一目标：单节点任务调度 MVP**
实现一个基本的任务调度器，具备以下能力：
1. 定时任务调度（基于 CRON 表达式）
2. 执行任务（模拟任务执行）
3. 本地任务存储（In-Memory 或简单数据库）
4. 执行日志记录

---

## 📁 一、项目结构（建议）

```
MonkeyScheduler/
├── Core/
│   ├── Scheduler.cs               # 主调度器
│   ├── TaskManager.cs             # 管理任务的增删查改
│   ├── CronParser.cs              # CRON 解析封装
│   ├── Models/
│   │   ├── ScheduledTask.cs       # 任务定义
│   │   ├── TaskExecutionLog.cs    # 执行日志
│   └── Services/
│       ├── ITaskExecutor.cs       # 抽象执行器接口
│       └── SimulatedTaskExecutor.cs # 模拟执行器实现
├── Storage/
│   └── InMemoryTaskRepository.cs  # 存储实现
├── Program.cs                     # 控制台入口
└── MonkeyScheduler.csproj
```

---

## 🧱 二、核心模型设计

### `ScheduledTask.cs`
```csharp
public class ScheduledTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public DateTime NextRunTime { get; set; }
    public bool Enabled { get; set; } = true;
}
```

### `TaskExecutionLog.cs`
```csharp
public class TaskExecutionLog
{
    public Guid TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Result { get; set; }
    public bool Success { get; set; }
}
```

---

## ⚙️ 三、调度引擎设计

### `Scheduler.cs`
```csharp
public class Scheduler
{
    private readonly ITaskRepository _repo;
    private readonly ITaskExecutor _executor;
    private readonly CancellationTokenSource _cts = new();

    public Scheduler(ITaskRepository repo, ITaskExecutor executor)
    {
        _repo = repo;
        _executor = executor;
    }

    public void Start()
    {
        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var tasks = _repo.GetAllTasks()
                                 .Where(t => t.Enabled && t.NextRunTime <= now)
                                 .ToList();

                foreach (var task in tasks)
                {
                    _ = _executor.ExecuteAsync(task); // 异步执行
                    task.NextRunTime = CronParser.GetNextOccurrence(task.CronExpression, now);
                }

                await Task.Delay(1000, _cts.Token);
            }
        }, _cts.Token);
    }

    public void Stop() => _cts.Cancel();
}
```

---

## 🧠 四、任务执行器（模拟）

### `ITaskExecutor.cs` & `SimulatedTaskExecutor.cs`
```csharp
public interface ITaskExecutor
{
    Task ExecuteAsync(ScheduledTask task);
}

public class SimulatedTaskExecutor : ITaskExecutor
{
    public async Task ExecuteAsync(ScheduledTask task)
    {
        var log = new TaskExecutionLog
        {
            TaskId = task.Id,
            StartTime = DateTime.UtcNow
        };

        try
        {
            Console.WriteLine($"[INFO] Executing: {task.Name}");
            await Task.Delay(500); // 模拟耗时任务
            log.Result = "Success";
            log.Success = true;
        }
        catch (Exception ex)
        {
            log.Result = ex.Message;
            log.Success = false;
        }

        log.EndTime = DateTime.UtcNow;
        Console.WriteLine($"[INFO] Task {task.Name} finished. Success: {log.Success}");
    }
}
```

---

## 🧪 五、启动程序

### `Program.cs`
```csharp
var repo = new InMemoryTaskRepository();
var executor = new SimulatedTaskExecutor();
var scheduler = new Scheduler(repo, executor);

// 添加两个任务
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

Console.WriteLine("Scheduler started. Press Enter to exit.");
Console.ReadLine();
scheduler.Stop();
```

> ⚠️ 推荐使用 `NCrontab` 或 `Cronos` 解析库来处理 CRON 表达式。

---

## ✅ 下一步建议（进入阶段二前）：

1. ✅ 抽象出接口，便于后续实现数据库存储
2. ✅ 实现日志收集器，可考虑写入 SQLite 或 JSON 文件
3. ✅ 编写单元测试：Cron 解析、调度轮询、任务执行器
4. 🚀 准备进入多节点调度设计（引入 Worker/调度协调器）

---

如果你需要，我可以直接帮你生成这套项目代码或者搭建 GitHub 仓库模板。你想动手写，还是我先生成骨架？