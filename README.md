# MonkeyScheduler 开发者使用手册

## 1. 项目概述

MonkeyScheduler 是一个基于 .NET 平台构建的高性能、分布式任务调度系统。它旨在为需要可靠、可扩展且灵活的任务调度解决方案的应用程序提供支持。该系统采用现代化的分布式架构，将任务调度逻辑与任务执行逻辑分离，通过调度服务（Scheduling Server）和工作节点服务（Worker Service）的协作，实现高效的任务分发、负载均衡和执行。

MonkeyScheduler 的核心特性包括：

- **基于 CRON 表达式的调度**：支持标准的 5 字段（分 时 日 月 周）和扩展的 6 字段（秒 分 时 日 月 周）CRON 表达式，能够满足从秒级到月级的各种复杂定时调度需求。
- **分布式架构**：调度服务负责任务的管理、调度和分发，而工作节点服务负责实际的任务执行。这种分离的设计提高了系统的可伸缩性和容错性。
- **负载均衡**：内置可插拔的负载均衡策略（默认提供基于请求计数的轮询策略，并支持自定义实现），确保任务能够均匀地分发到可用的工作节点上，避免单点过载。
- **节点健康检查**：调度服务会定期检查工作节点的心跳，自动剔除无响应或超时的节点，保证任务只分发给健康的节点。
- **任务管理**：提供 API 用于动态添加、查询、启用和禁用计划任务。
- **可扩展的任务执行器**：开发者可以通过实现 `ITaskExecutor` 接口来定义自己的任务执行逻辑，轻松集成现有业务或执行特定类型的任务。
- **可插拔的数据存储**：系统提供了数据访问接口，当前版本包含 MySQL 的实现，开发者也可以根据需要实现其他存储后端（如 SQL Server, PostgreSQL, MongoDB 等）。
- **任务执行日志与结果追踪**：详细记录每个任务的执行历史，包括开始时间、结束时间、执行状态（成功、失败、运行中）、错误信息和堆栈跟踪，便于监控和故障排查。
- **高可用性设计**：通过节点健康检查和任务重试机制（虽然当前代码中未显式体现重试，但架构支持），提升系统的整体可用性。

本手册旨在为开发人员提供全面的指导，涵盖 MonkeyScheduler 的安装部署、基本使用、高级功能配置、API 参考以及如何进行二次开发和扩展，帮助你快速将 MonkeyScheduler 集成到你的项目中并充分利用其功能。

---

## 2. 安装与配置

本章节将指导你如何安装和配置 MonkeyScheduler 的核心组件。

### 2.1 安装
通过 Nuget 安装包：
```shell
# 核心组件，调度和作业项目都需要装
Install-Package MonkeyScheduler

# 调度组件，安装在调度项目中
Install-Package MonkeyScheduler.SchedulerService

# 工作组件，安装在工作项目中
Install-Package MonkeyScheduler.WorkerService

# 数据库组件，调度和作业项目都需要装
Install-Package MonkeyScheduler.Data.MySQL
```

### 2.2 配置 appsettings.json

1. **配置调度项目的 `appsettings.json`配置：**
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

    - `AllowedHosts`: 生产环境建议设置为调度服务实际部署的域名或 IP 地址。
    - `MonkeyScheduler:Options:HeartbeatInterval`: 工作节点向调度服务发送心跳的频率。
    - `MonkeyScheduler:Options:NodeTimeoutInterval`: 调度服务判定一个节点失联的超时时间。
    - `MonkeyScheduler:SchedulerDb`: **必须**修改为你的 MySQL 数据库连接字符串。

2. **配置工作项目的 `appsettings.json`配置：**
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

    - `MonkeyScheduler:WorkerService:Url`: 工作节点服务自身监听的 URL。
    - `MonkeyScheduler:SchedulingServer:Url`: 调度服务的访问地址。
    - `MonkeyScheduler:SchedulerDb`: 工作节点服务也配置了数据库连接。


3. **数据库设置**
    MonkeyScheduler 需要一个数据库来存储任务信息、执行结果和执行日志。
    - 在你的 MySQL 服务器上创建一个新的数据库：
        
        ```sql
        CREATE DATABASE MonkeyScheduler CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
        ```
    - 在`MonkeyScheduler.Data.MySQL`项目源代码下找到`Scripts`文件，在MySQL中执行其中的`InitializeDatabase.sql`文件创建数据库表。




### 2.3 配置 Program.cs
1. **配置调度项目的`Program.cs`：**
    ```csharp
    // more code ...
    
    // 添加控制器并注册类库的控制器
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(WorkerApiController).Assembly)
        .AddApplicationPart(typeof(TasksController).Assembly);

    // 添加调度服务
    builder.Services.AddSchedulerService();
    // 添加自定义负载均衡
    builder.Services.AddLoadBalancer<CustomLoadBalancer>();
    // 注册NodeRegistry服务
    builder.Services.AddSingleton<INodeRegistry>(sp => 
        sp.GetRequiredService<NodeRegistry>());
    // 添加 MySQL 数据访问服务
    builder.Services.AddMySqlDataAccess();

    // more code ...

    // 引入调度器
    app.UseSchedulerService();

    // more code ...

    ```
2. **配置工作项目的`Program.cs`：**
    ```csharp
    // more code ...
    
    // 添加控制器并注册类库的控制器
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(TaskReceiverController).Assembly); 
    // 添加Worker服务
    builder.Services.AddWorkerService(
        builder.Configuration["MonkeyScheduler:WorkerService:Url"] ?? "http://localhost:5001"
    );

    // 注册自定义任务执行器
    builder.Services.AddSingleton<ITaskExecutor, CustomTaskExecutor>();
    // 添加MySQL数据访问服务
    builder.Services.AddMySqlDataAccess(); 

    // more code ...

    // 添加健康检查端点
    app.UseWorkerService();

    // more code ...
    ```
