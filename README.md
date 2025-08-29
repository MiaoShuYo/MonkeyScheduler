# MonkeyScheduler 使用指南（基于四个项目）

本指南面向使用解决方案内四个项目的落地实践：`MonkeyScheduler`（核心库）、`MonkeyScheduler.Data.MySQL`（MySQL 数据接入与日志）、`MonkeyScheduler.SchedulerService`（调度服务 API）、`MonkeyScheduler.WorkerService`（工作节点）。忽略仓库内其它 md 文档，以本 README 为准。

## 一、整体架构与运行流程

- 调度服务 `MonkeyScheduler.SchedulerService`：
  - 托管核心调度器 `MonkeyScheduler.Core.Scheduler`，周期性扫描待执行任务。
  - 通过 `ITaskDispatcher` + 负载均衡器将任务分发到在线 Worker 节点。
  - 接收 Worker 上报的执行结果，并（如配置 MySQL）落库。
  - 提供任务管理、重试配置、负载均衡、节点注册/心跳等 API。

- 工作节点 `MonkeyScheduler.WorkerService`：
  - 定期向调度服务发送注册与心跳，维持节点在线状态。
  - 接收调度服务下发的任务并执行，执行完成后回调上报状态。

- 核心库 `MonkeyScheduler`：
  - 定义任务模型、Cron 解析器、DAG 管理、任务处理器接口及内置处理器（HTTP/Shell/SQL）。
  - 调度器根据 Cron/重试/DAG 状态决定执行计划，最终通过分发器调用 Worker。

- MySQL 数据接入 `MonkeyScheduler.Data.MySQL`（可选）：
  - 提供 `ITaskRepository`、`ITaskExecutionResult` 的 MySQL 实现与日志持久化。
  - 通过 `AddMySqlDataAccess(...)` 注入仓储与日志 Provider。

数据流简述：Scheduler 定时轮询 → 选择可执行任务 → 负载均衡选择 Worker → 调度服务调用 Worker `/api/task/execute` → Worker 执行并 `/api/tasks/status` 回传结果。

## 二、快速开始

### 1. 准备环境
- .NET 8 SDK
- MySQL（可选，用于持久化与日志）。若仅尝试内存模式，可跳过 MySQL。

### 2. 初始化数据库（使用 MySQL 时）
在 MySQL 中执行初始化脚本（位于 `MonkeyScheduler.Data.MySQL/Scripts/InitializeDatabase.sql`）：

```sql
CREATE TABLE IF NOT EXISTS Logs (...);
CREATE TABLE IF NOT EXISTS ScheduledTasks (...);
CREATE TABLE IF NOT EXISTS TaskExecutionResults (...);
```

该脚本会创建日志、计划任务与执行结果三张表，字符集为 `utf8mb4`。

### 3. 启动调度服务（SchedulerService）

关键入口：`MonkeyScheduler.SchedulerService/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册调度服务（默认内存实现）
builder.Services.AddSchedulerService(builder.Configuration);

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
// 启动调度器 + 健康检查端点 /scheduler_health
app.UseSchedulerService();
app.Run();
```

应用配置 `MonkeyScheduler.SchedulerService/appsettings.json`（示例，注意更新连接串）：

```json
{
  "MonkeyScheduler": {
    "Database": {
      "MySQL": "Server=127.0.0.1;Database=monkeyscheduler;User=root;Password=your_password;"
    },
    "Retry": { "EnableRetry": true, "DefaultMaxRetryCount": 3, "DefaultRetryIntervalSeconds": 60, "DefaultRetryStrategy": "Exponential" },
    "Scheduler": { "CheckIntervalMilliseconds": 1000, "ExecuteDueTasksOnStartup": true, "MaxConcurrentTasks": 10 },
    "LoadBalancer": { "Strategy": "LeastConnection" }
  }
}
```

如需接入 MySQL 仓储与日志，请在宿主（通常是 SchedulerService）里调用：

```csharp
// using MonkeyScheduler.Data.MySQL;
builder.Services.AddMySqlDataAccess(); // 从配置读取连接串：MonkeyScheduler:Database:MySQL
```

启动后：
- Swagger 文档：`/swagger`
- 健康检查：`/scheduler_health`

### 4. 启动工作节点（WorkerService）

关键入口：`MonkeyScheduler.WorkerService/Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 绑定 Worker 配置
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("MonkeyScheduler:Worker"));

// Worker 核心服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<ITaskExecutor, DefaultTaskExecutor>();
builder.Services.AddScoped<IStatusReporterService, StatusReporterService>();
builder.Services.AddHostedService<NodeHeartbeatService>();
builder.Services.AddHealthChecks();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(c => c.RoutePrefix = "swagger"); }
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
```

应用配置 `MonkeyScheduler.WorkerService/appsettings.json`（关键字段）：

```json
{
  "MonkeyScheduler": {
    "Worker": {
      "WorkerUrl": "http://localhost:4058",           // 当前 Worker 对外可访问的根地址
      "SchedulerUrl": "http://localhost:4057",        // 调度服务地址
      "HeartbeatIntervalSeconds": 30,
      "MaxConcurrentTasks": 5,
      "AutoRegisterToScheduler": true
    }
  }
}
```

Worker 启动后动作：
- POST `${SchedulerUrl}/api/worker/register`，Body 为字符串 `WorkerUrl`。
- 定期 POST `${SchedulerUrl}/api/worker/heartbeat`，Body 同上。
- 接收任务：调度服务会调用 `POST ${WorkerUrl}/api/task/execute`。
- 上报结果：Worker 调用 `POST ${SchedulerUrl}/api/tasks/status`。

## 三、核心功能与 API

### 1) 任务管理（调度服务）
- 创建任务：`POST /api/tasks`
  - Body（示例）：
  ```json
  {
    "name": "demo-http",
    "description": "simple http task",
    "cronExpression": "*/30 * * * * *",
    "taskType": "http",
    "taskParameters": "{ \"url\": \"https://httpbin.org/get\", \"method\": \"GET\" }",
    "enableRetry": true,
    "maxRetryCount": 3,
    "retryIntervalSeconds": 60,
    "retryStrategy": "Exponential",
    "timeoutSeconds": 30
  }
  ```
- 启用任务：`PUT /api/tasks/{id}/enable`
- 禁用任务：`PUT /api/tasks/{id}/disable`
- 查询任务：`GET /api/tasks`
- 查看详情：`GET /api/tasks/{id}`
- 删除任务：`DELETE /api/tasks/{id}`
- 上报结果（由 Worker 调用）：`POST /api/tasks/status`

重试相关：
- 获取重试信息：`GET /api/tasks/{id}/retry-info`
- 手动重试：`POST /api/tasks/{id}/retry`
- 重置重试状态：`POST /api/tasks/{id}/reset-retry`
- 更新重试配置（针对单任务）：`PUT /api/tasks/{id}/retry-config`

全局重试配置：
- 获取：`GET /api/retryconfiguration`
- 更新：`PUT /api/retryconfiguration`
- 策略枚举：`GET /api/retryconfiguration/strategies`
- 计算试算：`GET /api/retryconfiguration/test-intervals?baseInterval=60&strategy=Exponential&maxRetries=3`

### 2) 负载均衡与节点
- 查询策略与详情：`GET /api/loadbalancing/strategies`、`GET /api/loadbalancing/strategies/{name}`
- 查看当前状态与节点负载：`GET /api/loadbalancing/status`、`GET /api/loadbalancing/node-loads`
- 更新策略配置：`PUT /api/loadbalancing/configuration`（Body 为键值对）
- 注册自定义策略：`POST /api/loadbalancing/register-strategy`
- 节点注册与心跳（由 Worker 调用）：`POST /api/worker/register`、`POST /api/worker/heartbeat`（Body: 字符串 `nodeUrl`）

### 3) 任务类型与参数校验
- 列出支持的任务类型：`GET /api/taskhandlers/types`
- 获取处理器配置：`GET /api/taskhandlers/config/{taskType}`
- 校验参数：`POST /api/taskhandlers/validate/{taskType}`（Body: 任意对象）
- 获取所有处理器配置：`GET /api/taskhandlers/configs`
- 检查类型是否支持：`GET /api/taskhandlers/supported/{taskType}`

内置处理器与示例参数：
- http：
  ```json
  { "url": "https://httpbin.org/post", "method": "POST", "body": "{\"hello\":\"world\"}", "headers": {"Content-Type":"application/json"}, "timeout": 30 }
  ```
- shell：
  ```json
  { "command": "echo hello", "workingDirectory": "C:/", "timeout": 60 }
  ```
- sql：
  ```json
  { "connectionString": "Server=...;Database=...;User=...;Password=...;", "sqlScript": "SELECT 1", "timeout": 60 }
  ```

## 四、DAG（可选）

调度器支持 DAG 依赖与工作流，`Scheduler` 会在执行时通过 `IDagDependencyChecker` 与 `IDagExecutionManager` 判断/触发上下游。若任务为 DAG 任务（如设置 `IsDagTask`、`DagWorkflowId`、依赖等），调度器会在依赖满足后触发后续任务。

要点：
- DAG 校验与启动通过 `Scheduler` 暴露的方法完成（内部由服务管理）。
- 普通定时任务与 DAG 任务可并存。

## 五、使用 MySQL 仓储与日志

在调度服务启动时调用扩展：

```csharp
// Program.cs
using MonkeyScheduler.Data.MySQL;
builder.Services.AddMySqlDataAccess();
```

或显式传入连接串/选项：

```csharp
builder.Services.AddMySqlDataAccess("Server=...;Database=...;User=...;Password=...");
// 或
builder.Services.AddMySqlDataAccess(new MySqlConnectionOptions { ConnectionString = "...", MaxRetryAttempts = 3 });
```

效果：
- 用 MySQL 实现替换默认内存的 `ITaskRepository` 与 `ITaskExecutionResult`。
- 注入 `ILoggerProvider`，将日志写入表 `Logs`。

## 六、本地开发与运行

1) 还原依赖并编译：
```bash
dotnet restore
dotnet build -c Debug
```

2) 先启动 SchedulerService（默认端口示例假设为 `http://localhost:4057`），再启动 WorkerService（`http://localhost:4058`）。确保两端口在配置中一致。

3) 打开调度服务 Swagger：创建任务并观察 Worker 控制台与数据库（如启用 MySQL）。

## 七、常见问题（FAQ）

- 如何确认节点在线？
  - 访问 `GET /api/loadbalancing/status` 查看 `TotalNodes` 与 `NodeLoads`。
  - Worker 健康检查：`GET {WorkerUrl}/health`。

- 收不到回调/结果？
  - 检查 `WorkerUrl` 是否为调度服务可达地址（容器/跨机部署时尤其注意）。
  - 检查防火墙与反向代理转发规则。

- SQL 任务连不上数据库？
  - 在 SQL 任务参数中使用正确的 `connectionString`。与 `AddMySqlDataAccess` 无直接耦合，彼此独立。

- 想扩展任务类型？
  - 实现 `ITaskHandler` 并通过 `TaskHandlerFactory.RegisterHandler<T>("type")` 注册即可；也可按现有 `HttpTaskHandler`、`ShellTaskHandler`、`SqlTaskHandler` 参考实现。

## 八、许可

MIT


