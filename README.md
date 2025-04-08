# MonkeyScheduler

一个简单的分布式任务调度系统，支持基于 CRON 表达式的定时任务调度。

## 功能特点

- 基于 CRON 表达式的任务调度
- 支持秒级和分钟级调度
- 可扩展的任务执行器
- 可自定义的任务存储
- 任务执行日志记录
- 支持任务启用/禁用

## 系统要求

- .NET 8.0 或更高版本
- .NET 9.0 或更高版本

## 安装

```bash
dotnet add package MonkeyScheduler
```

## CRON 表达式格式

MonkeyScheduler 支持两种 CRON 表达式格式：

1. **标准 5 字段格式**（分钟 时 日 月 周）：
   ```
   */5 * * * *     # 每5分钟执行一次
   0 */2 * * *     # 每2小时执行一次
   0 0 * * *       # 每天午夜执行
   ```

2. **扩展 6 字段格式**（秒 分 时 日 月 周）：
   ```
   */5 * * * * *   # 每5秒执行一次
   0 */30 * * * *  # 每30秒执行一次
   ```

## 快速开始

```csharp
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;

// 创建任务存储
var repo = new InMemoryTaskRepository();

// 创建任务执行器
var executor = new CustomTaskExecutor();

// 创建调度器
var scheduler = new Scheduler(repo, executor);

// 添加任务
repo.AddTask(new ScheduledTask
{
    Name = "示例任务",
    CronExpression = "*/5 * * * * *", // 每5秒执行一次
    NextRunTime = DateTime.UtcNow
});

// 启动调度器
scheduler.Start();

// 停止调度器
scheduler.Stop();
```

## 自定义任务执行器

```csharp
public class CustomTaskExecutor : ITaskExecutor
{
    public async Task ExecuteAsync(ScheduledTask task)
    {
        // 实现自定义的任务执行逻辑
        await Task.CompletedTask;
    }
}
```

## 自定义任务存储

```csharp
public class CustomTaskRepository : ITaskRepository
{
    public void AddTask(ScheduledTask task)
    {
        // 实现自定义的任务存储逻辑
    }

    public void UpdateTask(ScheduledTask task)
    {
        // 实现自定义的任务更新逻辑
    }

    public void DeleteTask(Guid taskId)
    {
        // 实现自定义的任务删除逻辑
    }

    public ScheduledTask? GetTask(Guid taskId)
    {
        // 实现自定义的任务获取逻辑
        return null;
    }

    public IEnumerable<ScheduledTask> GetAllTasks()
    {
        // 实现自定义的任务列表获取逻辑
        return Enumerable.Empty<ScheduledTask>();
    }
}
```

## 许可证

MIT 