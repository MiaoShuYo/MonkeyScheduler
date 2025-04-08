# MonkeyScheduler

一个简单的分布式任务调度系统，支持基于 CRON 表达式的定时任务调度。

## 功能特点

- 基于 CRON 表达式的任务调度
- 支持秒级和分钟级调度
- 可扩展的任务执行器
- 可自定义的任务存储
- 任务执行日志记录
- 支持任务启用/禁用
- 异步日志记录
- 支持多种日志级别（INFO、WARNING、ERROR）
- 灵活的日志格式化
- 自动日志清理策略
- SQLite 存储后端
- 高性能设计

## 系统要求

- .NET 8.0 或更高版本
- .NET 9.0 或更高版本

## 安装

```bash
dotnet add package MonkeyScheduler
dotnet add package System.Data.SQLite
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
using MonkeyScheduler.Logging;

// 创建日志记录器
var logger = new Logger();

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

// 记录系统启动日志
await logger.LogInfoAsync("调度系统启动");

// 启动调度器
scheduler.Start();

// 停止调度器
await logger.LogInfoAsync("调度系统停止");
scheduler.Stop();
```

## 日志记录功能

### 基本使用

```csharp
// 创建默认日志记录器
var logger = new Logger();

// 记录不同级别的日志
await logger.LogInfoAsync("系统启动");
await logger.LogWarningAsync("内存使用率较高");
await logger.LogErrorAsync("发生错误", new Exception("测试异常"));
```

### 自定义配置

```csharp
// 自定义数据库路径和清理策略
var logger = new Logger(
    dbPath: "C:\\Logs\\monkey_scheduler.db",
    maxLogCount: 5000,          // 最多保留5000条日志
    maxLogAge: TimeSpan.FromDays(7)  // 保留最近7天的日志
);

// 自定义日志格式
var formatter = new DefaultLogFormatter(
    format: "{timestamp} [{level}] {message}",
    includeTimestamp: true,
    includeException: true
);
var customLogger = new Logger(formatter: formatter);
```

### 日志清理

```csharp
// 手动执行清理
await logger.CleanupLogsAsync();

// 监控日志状态
var count = await logger.GetLogCountAsync();
var oldestDate = await logger.GetOldestLogDateAsync();
```

## 自定义任务执行器

```csharp
public class CustomTaskExecutor : ITaskExecutor
{
    private readonly ILogger _logger;

    public CustomTaskExecutor(ILogger logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(ScheduledTask task)
    {
        try
        {
            await _logger.LogInfoAsync($"开始执行任务: {task.Name}");
            // 实现自定义的任务执行逻辑
            await Task.CompletedTask;
            await _logger.LogInfoAsync($"任务执行完成: {task.Name}");
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"任务执行失败: {task.Name}", ex);
            throw;
        }
    }
}
```

## 自定义任务存储

```csharp
public class CustomTaskRepository : ITaskRepository
{
    private readonly ILogger _logger;

    public CustomTaskRepository(ILogger logger)
    {
        _logger = logger;
    }

    public void AddTask(ScheduledTask task)
    {
        try
        {
            // 实现自定义的任务存储逻辑
            await _logger.LogInfoAsync($"添加任务: {task.Name}");
        }
        catch (Exception ex)
        {
            await _logger.LogErrorAsync($"添加任务失败: {task.Name}", ex);
            throw;
        }
    }

    // ... 其他方法实现 ...
}
```

## 最佳实践

1. **日志记录**：
   - 在关键操作点记录日志
   - 使用适当的日志级别
   - 记录异常详细信息
   - 定期清理过期日志

2. **任务调度**：
   - 合理设置 CRON 表达式
   - 处理任务执行异常
   - 监控任务执行状态

3. **系统监控**：
   - 监控日志数量
   - 监控任务执行情况
   - 设置告警阈值

## 贡献指南

欢迎提交 Issue 和 Pull Request 来帮助改进这个项目。

## 许可证

MIT License 