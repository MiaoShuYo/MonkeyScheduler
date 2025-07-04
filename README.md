# MonkeyScheduler - 分布式任务调度系统

## 1. 项目概述

MonkeyScheduler 是一个基于 .NET 平台构建的高性能、分布式任务调度系统。它旨在为需要可靠、可扩展且灵活的任务调度解决方案的应用程序提供支持。该系统采用现代化的分布式架构，将任务调度逻辑与任务执行逻辑分离，通过调度服务（Scheduling Server）和工作节点服务（Worker Service）的协作，实现高效的任务分发、负载均衡和执行。

### 核心特性：

*   **基于 CRON 表达式的调度**：支持标准的 5 字段（分 时 日 月 周）和扩展的 6 字段（秒 分 时 日 月 周）CRON 表达式，能够满足从秒级到月级的各种复杂定时调度需求。
*   **分布式架构**：调度服务负责任务的管理、调度和分发，而工作节点服务负责实际的任务执行。这种分离的设计提高了系统的可伸缩性和容错性。
*   **负载均衡**：内置可插拔的负载均衡策略（默认提供基于请求计数的轮询策略，并支持自定义实现），确保任务能够均匀地分发到可用的工作节点上，避免单点过载。
*   **节点健康检查**：调度服务会定期检查工作节点的心跳，自动剔除无响应或超时的节点，保证任务只分发给健康的节点。
*   **任务管理**：提供 API 用于动态添加、查询、启用和禁用计划任务。
*   **可扩展的任务执行器**：开发者可以通过实现 `ITaskExecutor` 接口来定义自己的任务执行逻辑，轻松集成现有业务或执行特定类型的任务。
*   **可插拔的数据存储**：系统提供了数据访问接口，当前版本包含 MySQL 的实现，开发者也可以根据需要实现其他存储后端（如 SQL Server, PostgreSQL, MongoDB 等）。
*   **任务执行日志与结果追踪**：详细记录每个任务的执行历史，包括开始时间、结束时间、执行状态（成功、失败、运行中）、错误信息和堆栈跟踪，便于监控和故障排查。
*   **高可用性设计**：通过节点健康检查和任务重试机制（目前代码中未显式实现任务重试，但架构支持通过外部实现或集成），提升系统的整体可用性。

本手册旨在为开发人员提供全面的指导，涵盖 MonkeyScheduler 的安装部署、基本使用、高级功能配置、API 参考以及如何进行二次开发和扩展，帮助你快速将 MonkeyScheduler 集成到你的项目中并充分利用其功能。

## 2. 安装与配置

本章节将指导你如何安装和配置 MonkeyScheduler 的核心组件。

### 2.1 通过 NuGet 安装

你可以通过 NuGet 包管理器安装 MonkeyScheduler 的各个组件：

```shell
# 核心组件，调度和工作节点项目都需要安装
Install-Package MonkeyScheduler

# 调度服务组件，安装在调度项目中
Install-Package MonkeyScheduler.SchedulerService

# 工作节点服务组件，安装在工作项目中
Install-Package MonkeyScheduler.WorkerService

# MySQL 数据访问组件，调度和工作节点项目都需要安装
Install-Package MonkeyScheduler.Data.MySQL
```

### 2.2 配置 `appsettings.json`

#### 2.2.1 调度项目配置 (`appsettings.json`)

```json
{
  "AllowedHosts": "*",
  "MonkeyScheduler": {
    "Options": {
      "HeartbeatInterval": "00:00:30",
      "NodeTimeoutInterval": "00:02:00",
      "MaxRetryCount": 3
    },
    "SchedulerDb": "server=YOUR_MYSQL_HOST;port=3306;database=MonkeyScheduler;user=YOUR_USERNAME;password=YOUR_PASSWORD;"
  }
}
```

*   `AllowedHosts`: 生产环境建议设置为调度服务实际部署的域名或 IP 地址。
*   `MonkeyScheduler:Options:HeartbeatInterval`: 工作节点向调度服务发送心跳的频率。
*   `MonkeyScheduler:Options:NodeTimeoutInterval`: 调度服务判定一个节点失联的超时时间。
*   `MonkeyScheduler:Options:MaxRetryCount`: 任务的最大重试次数（当前架构支持，但需自行实现重试逻辑）。
*   `MonkeyScheduler:SchedulerDb`: **必须**修改为你的 MySQL 数据库连接字符串。

#### 2.2.2 工作节点项目配置 (`appsettings.json`)

```json
{
  "AllowedHosts": "*",
  "MonkeyScheduler": {
    "WorkerService": {
      "Url": "http://localhost:5046"
    },
    "SchedulingServer": {
      "Url": "http://localhost:5190"
    },
    "SchedulerDb": "server=YOUR_MYSQL_HOST;port=3306;database=MonkeyScheduler;user=YOUR_USERNAME;password=YOUR_PASSWORD;"
  }
}
```

*   `MonkeyScheduler:WorkerService:Url`: 工作节点服务自身监听的 URL。
*   `MonkeyScheduler:SchedulingServer:Url`: 调度服务的访问地址。
*   `MonkeyScheduler:SchedulerDb`: 工作节点服务也需要配置数据库连接，用于记录任务执行日志等。

### 2.3 数据库设置

MonkeyScheduler 需要一个 MySQL 数据库来存储任务信息、执行结果和执行日志。

1.  **创建数据库**：

    在你的 MySQL 服务器上创建一个新的数据库：

    ```sql
    CREATE DATABASE MonkeyScheduler CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
    ```

2.  **初始化数据库表**：

    在 `MonkeyScheduler.Data.MySQL` 项目源代码下找到 `Scripts` 文件夹，执行其中的 `InitializeDatabase.sql` 文件来创建所需的数据库表。

### 2.4 配置 `Program.cs`

#### 2.4.1 调度项目配置 (`Program.cs`)

```csharp
// ... 其他代码 ...

// 添加控制器并注册类库的控制器
builder.Services.AddControllers()
    .AddApplicationPart(typeof(WorkerApiController).Assembly)
    .AddApplicationPart(typeof(TasksController).Assembly);

// 添加调度服务
builder.Services.AddSchedulerService();
// 添加自定义负载均衡（如果需要，否则可移除）
// builder.Services.AddLoadBalancer<CustomLoadBalancer>();
// 注册NodeRegistry服务
builder.Services.AddSingleton<INodeRegistry>(sp => 
    sp.GetRequiredService<NodeRegistry>());
// 添加 MySQL 数据访问服务
builder.Services.AddMySqlDataAccess();

// ... 其他代码 ...

// 引入调度器
app.UseSchedulerService();

// ... 其他代码 ...
```

#### 2.4.2 工作节点项目配置 (`Program.cs`)

```csharp
// ... 其他代码 ...

// 添加控制器并注册类库的控制器
builder.Services.AddControllers()
    .AddApplicationPart(typeof(TaskReceiverController).Assembly); 
// 添加Worker服务
builder.Services.AddWorkerService(
    builder.Configuration["MonkeyScheduler:WorkerService:Url"] ?? "http://localhost:5001"
);

// 注册自定义任务执行器
builder.Services.AddSingleton<ITaskExecutor, CustomTaskExecutor>(); // 替换为你的自定义执行器
// 添加MySQL数据访问服务
builder.Services.AddMySqlDataAccess(); 

// ... 其他代码 ...

// 添加健康检查端点
app.UseWorkerService();

// ... 其他代码 ...
```

## 3. 快速开始与使用

本节将指导你如何启动 MonkeyScheduler 并添加你的第一个计划任务。

### 3.1 启动调度服务和工作节点服务

1.  **构建项目**：在项目根目录运行 `dotnet build`。
2.  **启动调度服务**：进入 `SchedulingServer` 项目目录，运行 `dotnet run`。
3.  **启动工作节点服务**：进入 `WorkerService` 项目目录，运行 `dotnet run`。

确保两个服务都成功启动，并且 `appsettings.json` 中的 URL 配置正确，以便它们可以相互通信。

### 3.2 定义你的任务执行器

在工作节点服务中，你需要定义如何执行你的任务。这通过实现 `ITaskExecutor` 接口来完成。例如，创建一个 `CustomTaskExecutor.cs`：

```csharp
using MonkeyScheduler.Core.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class CustomTaskExecutor : ITaskExecutor
{
    private readonly ILogger<CustomTaskExecutor> _logger;

    public CustomTaskExecutor(ILogger<CustomTaskExecutor> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(string taskName, string taskData)
    {
        _logger.LogInformation($"开始执行任务: {taskName}, 数据: {taskData}");
        // 在这里实现你的业务逻辑
        // 例如，调用外部API，处理数据，发送通知等
        await Task.Delay(1000); // 模拟耗时操作
        _logger.LogInformation($"任务 {taskName} 执行完成。");
    }
}
```

然后在工作节点项目的 `Program.cs` 中注册你的自定义执行器：

```csharp
builder.Services.AddSingleton<ITaskExecutor, CustomTaskExecutor>();
```

### 3.3 添加计划任务

你可以通过调用调度服务的 API 来动态添加计划任务。调度服务提供了 RESTful API 接口。以下是一个使用 `curl` 命令添加任务的示例：

```bash
curl -X POST \ \
  http://localhost:5190/api/tasks \ \
  -H 'Content-Type: application/json' \ \
  -d '{
    "taskName": "MyDailyReportTask",
    "cronExpression": "0 0 * * *",
    "taskData": "Generate daily sales report",
    "enabled": true
  }'
```

*   `taskName`: 任务的唯一名称。
*   `cronExpression`: CRON 表达式，定义任务的执行频率。例如，`"0 0 * * *"` 表示每天午夜执行。
*   `taskData`: 任务执行时传递给 `ITaskExecutor` 的数据，可以是任何字符串格式，例如 JSON。
*   `enabled`: 任务是否启用。

**CRON 表达式示例：**

*   `"*/5 * * * * *"`: 每 5 秒执行一次 (秒 分 时 日 月 周)
*   `"0 */1 * * * *"`: 每分钟执行一次 (秒 分 时 日 月 周)
*   `"0 0 12 * * ?"`: 每天中午 12 点执行 (秒 分 时 日 月 周)
*   `"0 0 10,14,16 * * ?"`: 每天上午 10 点、下午 2 点和 4 点执行 (秒 分 时 日 月 周)

### 3.4 任务管理 API

调度服务提供了以下 API 用于任务管理：

*   **添加任务**：`POST /api/tasks`
    *   请求体：`CreateTaskRequest` (包含 `taskName`, `cronExpression`, `taskData`, `enabled`)
*   **查询所有任务**：`GET /api/tasks`
*   **查询单个任务**：`GET /api/tasks/{taskName}`
*   **更新任务**：`PUT /api/tasks/{taskName}`
    *   请求体：`UpdateTaskRequest` (包含 `cronExpression`, `taskData`, `enabled`)
*   **启用任务**：`POST /api/tasks/{taskName}/enable`
*   **禁用任务**：`POST /api/tasks/{taskName}/disable`
*   **删除任务**：`DELETE /api/tasks/{taskName}`

## 4. 高级配置与扩展

### 4.1 自定义负载均衡

MonkeyScheduler 允许你实现自定义的负载均衡策略。你需要实现 `ILoadBalancer` 接口：

```csharp
using MonkeyScheduler.Core.Services;
using System.Collections.Generic;
using System.Linq;

public class CustomLoadBalancer : ILoadBalancer
{
    public string SelectWorker(IEnumerable<string> availableWorkers)
    {
        // 实现你的负载均衡逻辑，例如：
        // 轮询、最少连接、随机等
        return availableWorkers.FirstOrDefault(); // 简单示例：总是选择第一个可用的工作节点
    }
}
```

然后在调度项目的 `Program.cs` 中注册你的自定义负载均衡器：

```csharp
builder.Services.AddLoadBalancer<CustomLoadBalancer>();
```

### 4.2 任务重试机制

虽然 MonkeyScheduler 架构支持任务重试，但当前版本并未内置显式的重试逻辑。你可以在 `ITaskExecutor` 的实现中自行添加重试逻辑，或者集成第三方库（如 Polly）来实现更复杂的重试策略。

例如，在 `CustomTaskExecutor` 中添加简单的重试：

```csharp
using MonkeyScheduler.Core.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class CustomTaskExecutor : ITaskExecutor
{
    private readonly ILogger<CustomTaskExecutor> _logger;
    private const int MaxRetries = 3;

    public CustomTaskExecutor(ILogger<CustomTaskExecutor> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(string taskName, string taskData)
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                _logger.LogInformation($"开始执行任务: {taskName}, 数据: {taskData} (尝试 {i + 1}/{MaxRetries})");
                // 你的业务逻辑
                // 模拟可能失败的操作
                if (new Random().Next(0, 5) == 0 && i < MaxRetries - 1) // 模拟失败，但最后一次尝试不失败
                {
                    throw new Exception("模拟任务执行失败");
                }
                await Task.Delay(1000); 
                _logger.LogInformation($"任务 {taskName} 执行完成。");
                return; // 成功则退出
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"任务 {taskName} 执行失败 (尝试 {i + 1}/{MaxRetries}): {ex.Message}");
                if (i < MaxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // 指数退避
                }
            }
        }
        _logger.LogError($"任务 {taskName} 最终执行失败，已达最大重试次数。");
    }
}
```

## 5. 贡献与开发

我们欢迎社区贡献！如果你想为 MonkeyScheduler 做出贡献，请遵循以下步骤：

1.  **Fork 项目**：在 GitHub 上 Fork `MiaoShuYo/MonkeyScheduler` 仓库。
2.  **克隆仓库**：将你的 Fork 克隆到本地开发环境。
    ```bash
    git clone https://github.com/YOUR_USERNAME/MonkeyScheduler.git
    cd MonkeyScheduler
    git checkout dev
    ```
3.  **安装依赖**：确保你安装了 .NET SDK。
4.  **构建项目**：在项目根目录运行 `dotnet build`。
5.  **运行测试**：在项目根目录运行 `dotnet test` 来确保所有测试通过。
6.  **创建新分支**：为你的功能或 Bug 修复创建一个新的分支。
    ```bash
    git checkout -b feature/your-feature-name
    ```
7.  **编写代码**：实现你的功能或修复 Bug，并编写相应的单元测试。
8.  **提交更改**：提交你的代码，并编写清晰的提交信息。
9.  **创建 Pull Request**：将你的分支推送到 GitHub，并创建一个 Pull Request 到 `dev` 分支。

## 6. 许可证

[待补充许可证信息]

## 7. 联系方式

如果你有任何问题或建议，可以通过 GitHub Issues 联系我们。

---

## 代码改进建议总结

基于对 Dev 分支代码的分析，以下是一些建议的改进点，旨在提高系统的健壮性、性能和可维护性：

1.  **异常处理优化**：
    *   在 `CronParser.cs` 中，避免捕获泛型 `Exception`，应捕获更具体的异常类型（如 `CronFormatException`）。
    *   将异常信息通过 `ILogger` 记录，而不是直接 `Console.WriteLine`。
    *   对于无法解析的 CRON 表达式，考虑抛出自定义异常或返回明确的错误状态，而不是返回一个“猜测”的 `DateTime`。

2.  **数据库连接管理**：
    *   在 `MySqlDbContext.cs` 中，`Connection` 属性的 `get` 访问器应检查 `_connection.State == ConnectionState.Closed`，以避免在连接已关闭但对象未 `Dispose` 的情况下重复创建连接。
    *   考虑引入连接池管理，或者确保 `MySqlDbContext` 实例的生命周期与数据库操作的生命周期一致，以减少不必要的连接创建和关闭开销。

3.  **任务重试机制的明确实现**：
    *   虽然架构支持，但目前代码中没有显式的任务重试逻辑。建议在 `SchedulerService` 或 `WorkerService` 中实现一个可配置的、通用的任务重试机制，例如使用 Polly 库。
    *   将 `MaxRetryCount` 配置项与实际的重试逻辑关联起来。

4.  **负载均衡策略的默认实现和示例**：
    *   确保 `ILoadBalancer` 有一个清晰的默认实现，并提供如何自定义和注册负载均衡器的详细示例。

5.  **异步编程的推广**：
    *   在 `Scheduler.cs` 的 `Start` 方法中，`_repo.GetAllTasks()` 和 `_dispatcher.Dispatch()` 等操作应尽可能使用异步版本，以提高系统的并发处理能力和响应性。
    *   检查整个项目，将所有 IO 密集型和计算密集型操作改为异步方法，以充分利用 .NET 的异步特性。

6.  **配置管理强类型化**：
    *   将 `appsettings.json` 中的配置项绑定到强类型的 C# 配置类，例如使用 `IOptions<T>` 模式，以提高配置的类型安全性和可读性，并减少运行时错误。

7.  **测试覆盖率提升**：
    *   增加对核心调度逻辑、分布式组件间通信以及异常路径的单元测试和集成测试，确保代码的健壮性。

8.  **代码注释和文档**：
    *   在复杂或关键的代码段增加更详细的注释，解释设计思路、算法选择和潜在的注意事项。
    *   保持代码风格一致性，提高可读性。

通过实施这些改进，MonkeyScheduler 项目将变得更加稳定、高效和易于维护。

