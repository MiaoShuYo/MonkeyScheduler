 # MonkeyScheduler

一个高性能的分布式任务调度系统，支持基于 CRON 表达式的定时任务调度和负载均衡。

## 功能特点

- 基于 CRON 表达式的任务调度
- 支持秒级和分钟级调度
- 分布式架构设计
- 内置负载均衡
- 节点健康检查
- 任务重试机制
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
- .NET 9.0 支持（可选）

## 项目结构

解决方案包含以下项目：

1. **MonkeyScheduler**：核心库项目，包含调度系统的所有核心功能实现。
2. **MonkeyScheduler.Tests**：核心库的单元测试项目。
3. **MonkeyScheduler.WorkerService**：工作节点服务项目，负责实际执行任务。
4. **MonkeyScheduler.WorkerService.Tests**：工作节点服务的单元测试项目。
5. **MonkeyScheduler.SchedulerService**：调度服务项目，负责任务分发和负载均衡。
6. **MonkeyScheduler.SchedulerService.Tests**：调度服务的单元测试项目。
7. **MonkeyScheduler.Sample**：示例项目，展示如何使用 MonkeyScheduler 库。

## 在项目中引入

### 1. 通过 NuGet 包管理器

在 Visual Studio 中：
1. 右键点击项目 -> 管理 NuGet 包
2. 搜索 "MonkeyScheduler"
3. 选择并安装需要的包：
   - MonkeyScheduler（核心库）
   - MonkeyScheduler.WorkerService（如需部署工作节点）
   - MonkeyScheduler.SchedulerService（如需部署调度服务）

### 2. 通过命令行

```bash
# 安装核心库
dotnet add package MonkeyScheduler

# 安装工作节点服务（可选）
dotnet add package MonkeyScheduler.WorkerService

# 安装调度服务（可选）
dotnet add package MonkeyScheduler.SchedulerService
```

### 3. 修改项目文件

在项目的 .csproj 文件中添加引用：

```xml
<ItemGroup>
    <PackageReference Include="MonkeyScheduler" Version="1.0.0" />
    <PackageReference Include="MonkeyScheduler.WorkerService" Version="1.0.0" />
    <PackageReference Include="MonkeyScheduler.SchedulerService" Version="1.0.0" />
</ItemGroup>
```

## 详细使用方法

### 1. 部署调度服务

#### 创建调度服务项目

1. 创建新的 ASP.NET Core Web API 项目
2. 安装必要的包
3. 配置 Program.cs：

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加调度服务
builder.Services.AddSchedulerService(options =>
{
    options.DatabaseConnectionString = builder.Configuration.GetConnectionString("SchedulerDb");
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
    options.NodeTimeoutInterval = TimeSpan.FromMinutes(2);
});

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddCheck<SchedulerHealthCheck>("scheduler_health");

// 添加负载均衡
builder.Services.AddLoadBalancer(options =>
{
    options.Strategy = LoadBalancerStrategy.RoundRobin;
    options.RetryCount = 3;
    options.RetryInterval = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

// 配置中间件
app.UseSchedulerService();
app.MapHealthChecks("/health");

app.Run();
```

#### 配置 appsettings.json：

```json
{
  "ConnectionStrings": {
    "SchedulerDb": "Data Source=scheduler.db"
  },
  "SchedulerOptions": {
    "HeartbeatInterval": "00:00:30",
    "NodeTimeoutInterval": "00:02:00",
    "MaxRetryCount": 3
  }
}
```

### 2. 部署工作节点

#### 创建工作节点项目

1. 创建新的 ASP.NET Core Web API 项目
2. 安装必要的包
3. 配置 Program.cs：

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加工作节点服务
builder.Services.AddWorkerService(options =>
{
    options.SchedulerUrl = builder.Configuration["WorkerOptions:SchedulerUrl"];
    options.NodeId = builder.Configuration["WorkerOptions:NodeId"];
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);
});

// 添加自定义任务执行器
builder.Services.AddScoped<ITaskExecutor, CustomTaskExecutor>();

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddCheck<WorkerHealthCheck>("worker_health");

var app = builder.Build();

// 配置中间件
app.UseWorkerService();
app.MapHealthChecks("/health");

app.Run();
```

#### 配置 appsettings.json：

```json
{
  "WorkerOptions": {
    "SchedulerUrl": "http://localhost:5000",
    "NodeId": "worker-1",
    "HeartbeatInterval": "00:00:30"
  }
}
```

### 3. 实现自定义任务执行器

```csharp
public class CustomTaskExecutor : ITaskExecutor
{
    private readonly ILogger<CustomTaskExecutor> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiBaseUrl;

    public CustomTaskExecutor(
        ILogger<CustomTaskExecutor> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _apiBaseUrl = configuration["ApiBaseUrl"];
    }

    public async Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task> callback)
    {
        try
        {
            _logger.LogInformation($"开始执行任务: {task.Name}");
            
            var startTime = DateTime.UtcNow;
            var client = _httpClientFactory.CreateClient();
            
            // 实现具体的任务执行逻辑
            var response = await client.PostAsync($"{_apiBaseUrl}/api/tasks/{task.Id}/execute", null);
            
            var result = new TaskExecutionResult
            {
                TaskId = task.Id,
                Status = response.IsSuccessStatusCode 
                    ? TaskExecutionStatus.Success 
                    : TaskExecutionStatus.Failed,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                ErrorMessage = response.IsSuccessStatusCode 
                    ? null 
                    : await response.Content.ReadAsStringAsync()
            };

            await callback(result);
            _logger.LogInformation($"任务执行完成: {task.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"任务执行失败: {task.Name}");
            
            var result = new TaskExecutionResult
            {
                TaskId = task.Id,
                Status = TaskExecutionStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };

            await callback(result);
            throw;
        }
    }
}
```

### 4. 添加和管理任务

```csharp
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly IScheduler _scheduler;
    private readonly ITaskRepository _taskRepository;

    public TasksController(IScheduler scheduler, ITaskRepository taskRepository)
    {
        _scheduler = scheduler;
        _taskRepository = taskRepository;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(CreateTaskRequest request)
    {
        var task = new ScheduledTask
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CronExpression = request.CronExpression,
            IsEnabled = true,
            NextRunTime = DateTime.UtcNow
        };

        await _taskRepository.AddTaskAsync(task);
        return Ok(task);
    }

    [HttpPut("{id}/enable")]
    public async Task<IActionResult> EnableTask(Guid id)
    {
        var task = await _taskRepository.GetTaskAsync(id);
        if (task == null) return NotFound();

        task.IsEnabled = true;
        await _taskRepository.UpdateTaskAsync(task);
        return Ok();
    }

    [HttpPut("{id}/disable")]
    public async Task<IActionResult> DisableTask(Guid id)
    {
        var task = await _taskRepository.GetTaskAsync(id);
        if (task == null) return NotFound();

        task.IsEnabled = false;
        await _taskRepository.UpdateTaskAsync(task);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var tasks = await _taskRepository.GetTasksAsync();
        return Ok(tasks);
    }
}
```

### 5. 监控和日志

#### 配置日志

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.AddEventLog();
    
    // 配置日志级别
    logging.SetMinimumLevel(LogLevel.Information);
});
```

#### 查看任务执行历史

```csharp
[ApiController]
[Route("api/[controller]")]
public class TaskHistoryController : ControllerBase
{
    private readonly ITaskExecutionLogRepository _logRepository;

    public TaskHistoryController(ITaskExecutionLogRepository logRepository)
    {
        _logRepository = logRepository;
    }

    [HttpGet("task/{taskId}")]
    public async Task<IActionResult> GetTaskHistory(Guid taskId)
    {
        var logs = await _logRepository.GetTaskExecutionLogsAsync(taskId);
        return Ok(logs);
    }
}
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

## 常见问题解答

### 1. 如何处理任务执行超时？

在任务执行器中实现超时控制：

```csharp
public async Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task> callback)
{
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // 5分钟超时
    try
    {
        await ExecuteWithTimeoutAsync(task, callback, cts.Token);
    }
    catch (OperationCanceledException)
    {
        await callback(new TaskExecutionResult
        {
            TaskId = task.Id,
            Status = TaskExecutionStatus.Timeout,
            ErrorMessage = "任务执行超时"
        });
    }
}
```

### 2. 如何实现自定义存储？

实现 `ITaskRepository` 接口：

```csharp
public class CustomTaskRepository : ITaskRepository
{
    // 实现接口方法
    public Task<IEnumerable<ScheduledTask>> GetTasksAsync()
    {
        // 自定义实现
    }

    public Task<ScheduledTask> GetTaskAsync(Guid id)
    {
        // 自定义实现
    }

    // ... 其他方法实现
}
```

### 3. 如何实现自定义负载均衡策略？

实现 `ILoadBalancer` 接口：

```csharp
public class CustomLoadBalancer : ILoadBalancer
{
    public Task<string> SelectNodeAsync(IEnumerable<string> nodes)
    {
        // 自定义节点选择逻辑
    }
}
```

## 贡献

欢迎提交 Issue 和 Pull Request 来帮助改进这个项目。

## 许可证

本项目采用 MIT 许可证。