 # MonkeyScheduler

一个高性能的分布式任务调度系统，支持基于 CRON 表达式的定时任务调度、DAG 任务编排、负载均衡和任务重试。

## 功能特点

- 基于 CRON 表达式的任务调度
- 支持秒级和分钟级调度
- 分布式架构设计
- 内置负载均衡（支持LeastConnection、RoundRobin、Random等多种策略）
- 节点健康检查
- 任务重试机制（支持固定间隔、指数退避、线性增长策略）
- 可扩展的任务执行器和插件化任务类型
- 可自定义的任务存储（支持InMemory、MySQL等）
- 任务执行日志记录
- 支持任务启用/禁用
- 异步日志记录
- 支持多种日志级别（INFO、WARNING、ERROR）
- 灵活的日志格式化
- 自动日志清理策略
- SQLite 和 MySQL 存储后端
- 高性能设计
- DAG 任务编排支持（依赖关系管理、循环检测、并行执行）

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
    - MonkeyScheduler.Data.MySQL（如需使用 MySQL 存储后端）

### 2. 通过命令行

```bash
# 安装核心库
dotnet add package MonkeyScheduler

# 安装工作节点服务
dotnet add package MonkeyScheduler.WorkerService

# 安装调度服务
dotnet add package MonkeyScheduler.SchedulerService

# 安装 MySQL 数据存储（可选）
dotnet add package MonkeyScheduler.Data.MySQL
```

### 3. 修改项目文件

在项目的 .csproj 文件中添加引用：

```xml
<ItemGroup>
    <PackageReference Include="MonkeyScheduler" Version="1.1.0-Beta" />
    <PackageReference Include="MonkeyScheduler.WorkerService" Version="1.1.0-Beta" />
    <PackageReference Include="MonkeyScheduler.SchedulerService" Version="1.1.0-Beta" />
    <PackageReference Include="MonkeyScheduler.Data.MySQL" Version="1.1.0-Beta" />
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

### 6. DAG 任务编排

MonkeyScheduler 支持 DAG（有向无环图）任务编排，允许定义任务依赖关系。

示例：
```csharp
var taskA = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task A" };
var taskB = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task B", Dependencies = new List<Guid> { taskA.Id } };

// 添加到调度器
await taskRepository.AddTaskAsync(taskA);
await taskRepository.AddTaskAsync(taskB);

// 启动 DAG 工作流
await dagExecutionManager.StartWorkflowAsync(workflowId, new List<ScheduledTask> { taskA, taskB });
```

### 7. 负载均衡配置

支持多种策略配置：

```csharp
builder.Services.AddLoadBalancer(options =>
{
    options.Strategy = LoadBalancingStrategy.LeastConnection;
    options.MaxConnectionsPerNode = 100;
});
```

### 8. 任务重试配置

示例任务重试配置：

```csharp
var task = new ScheduledTask
{
    Name = "Retry Task",
    EnableRetry = true,
    MaxRetryCount = 5,
    RetryStrategy = RetryStrategy.Exponential,
    RetryIntervalSeconds = 60
};
```

### 9. 任务类型插件

支持自定义任务类型：

```csharp
public class CustomTaskHandler : ITaskHandler
{
    public async Task<TaskExecutionResult> HandleAsync(ScheduledTask task, object? parameters = null)
    {
        // 自定义任务逻辑
    }
}
```

### 10. 自定义任务存储

实现 `ITaskRepository` 接口来自定义任务存储：

```csharp
public class CustomTaskRepository : ITaskRepository
{
    public async Task<IEnumerable<ScheduledTask>> GetTasksAsync()
    {
        // 自定义实现：从文件或自定义数据库读取任务
        return new List<ScheduledTask>(); // 示例返回空列表
    }

    public async Task<ScheduledTask> GetTaskAsync(Guid id)
    {
        // 自定义实现：根据 ID 获取任务
        return new ScheduledTask(); // 示例返回空任务
    }

    // ... 实现其他方法，如 AddTaskAsync, UpdateTaskAsync 等
}
```

在服务注册中添加：
```csharp
builder.Services.AddSingleton<ITaskRepository, CustomTaskRepository>();
```

### 11. 自定义日志格式化

实现 `ILogFormatter` 接口来自定义日志格式：

```csharp
public class CustomLogFormatter : ILogFormatter
{
    public string Format(string level, string message)
    {
        return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}"; // 自定义格式
    }
}
```

在日志配置中注册：
```csharp
builder.Services.AddSingleton<ILogFormatter, CustomLogFormatter>();
```

### 12. 自定义节点注册与心跳

扩展 `INodeRegistry` 接口或修改 `NodeHeartbeatService` 来自定义心跳逻辑：

```csharp
public class CustomNodeRegistry : INodeRegistry
{
    public void Register(string nodeUrl)
    {
        // 自定义注册逻辑，例如添加节点元数据
    }

    public void Heartbeat(string nodeUrl)
    {
        // 自定义心跳逻辑，例如记录负载指标
    }

    // ... 实现其他方法
}
```

在服务注册中添加：
```csharp
builder.Services.AddSingleton<INodeRegistry, CustomNodeRegistry>();
```

### 13. 自定义 API 扩展

扩展现有控制器添加自定义端点：

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomTasksController : TasksController
{
    [HttpGet("custom")]
    public async Task<IActionResult> GetCustomTasks()
    {
        // 自定义逻辑：返回特定任务列表
        return Ok(await _taskRepository.GetTasksAsync());
    }
}
```

在 Program.cs 中注册控制器。

### 14. 自定义 UI

如果使用 Blazor 或 Vue.js 等前端框架，自定义 UI 页面：

示例（Blazor 组件）：
```razor
@page "/custom-dashboard"
<h1>自定义任务仪表盘</h1>
<!-- 自定义 UI 元素，如图表或按钮 -->
<button @onclick="LoadTasks">加载任务</button>
```

连接到 API 端点以获取数据。详细 UI 自定义需参考前端项目。

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