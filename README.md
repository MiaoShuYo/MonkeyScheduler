# MonkeyScheduler 官方文档

## 目录

1.  项目概述
    1.1. 什么是 MonkeyScheduler？
    1.2. 核心特性
    1.3. 适用场景

2.  系统架构与设计理念
    2.1. 整体架构概览
    2.2. 调度服务 (Scheduling Server)
    2.3. 工作节点服务 (Worker Service)
    2.4. 数据存储层
    2.5. 核心组件交互

3.  安装与配置
    3.1. 环境准备
    3.2. 通过 NuGet 安装
    3.3. 数据库配置
    3.4. `appsettings.json` 配置
    3.5. `Program.cs` 配置

4.  快速开始与使用
    4.1. 启动服务
    4.2. 定义你的任务执行器
    4.3. 添加计划任务
    4.4. 任务管理 API

5.  高级功能与扩展
    5.1. 自定义负载均衡
    5.2. 任务重试机制
    5.3. 任务持久化与恢复
    5.4. 监控与日志
    5.5. 扩展任务执行器

6.  API 参考
    6.1. 调度服务 API
    6.2. 工作节点服务 API
    6.3. 核心库接口

7.  贡献指南
    7.1. 如何贡献
    7.2. 开发环境搭建
    7.3. 测试
    7.4. 提交规范

8.  常见问题 (FAQ)

9.  许可证

## 1. 项目概述

### 1.1. 什么是 MonkeyScheduler？

MonkeyScheduler 是一个专为 .NET 平台设计的高性能、可扩展的分布式任务调度系统。它旨在解决企业级应用中复杂的定时任务管理和执行需求。无论是需要定时发送报告、处理批量数据、执行系统维护任务，还是集成第三方服务，MonkeyScheduler 都能提供一个稳定、可靠且易于使用的解决方案。它将任务的调度逻辑与实际的业务执行逻辑分离，通过智能的任务分发和负载均衡机制，确保任务能够高效、准确地在分布式环境中运行。

### 1.2. 核心特性

MonkeyScheduler 具备以下核心特性，使其成为一个强大而灵活的任务调度平台：

*   **基于 CRON 表达式的灵活调度**：支持标准的 5 字段（分 时 日 月 周）和扩展的 6 字段（秒 分 时 日 月 周）CRON 表达式。这意味着你可以精确地定义任务的执行时间，从每秒执行一次到每月执行一次，甚至更复杂的周期性调度，都能轻松实现。

*   **高性能分布式架构**：系统由独立的调度服务（Scheduling Server）和多个工作节点服务（Worker Service）组成。调度服务负责任务的注册、管理、触发和分发，而工作节点服务则专注于接收并执行任务。这种解耦的设计极大地提升了系统的可伸缩性、容错性和并发处理能力。当任务量增加时，只需增加工作节点即可水平扩展。

*   **智能负载均衡**：内置可插拔的负载均衡策略，确保任务能够均匀地分发到所有可用的工作节点上。这不仅可以避免单个节点过载，提高系统整体的吞吐量，还支持开发者根据自身业务需求实现自定义的负载均衡算法，以适应更复杂的调度场景。

*   **健壮的节点健康检查**：调度服务会持续监控所有注册工作节点的心跳状态。一旦发现节点无响应或超时，系统会自动将其从可用节点列表中移除，确保任务只会被分发到健康的、可正常工作的节点上，从而提升系统的可靠性和任务的成功率。

*   **全面的任务管理 API**：提供一套完整的 RESTful API 接口，允许开发者通过编程方式动态地添加、查询、更新、启用、禁用和删除计划任务。这使得 MonkeyScheduler 可以轻松地集成到现有的管理后台或自动化流程中，实现任务的自动化管理。

*   **可扩展的任务执行器**：MonkeyScheduler 提供了 `ITaskExecutor` 接口，开发者只需实现这个接口，即可定义自己的任务执行逻辑。这意味着你可以轻松地将现有的业务代码、第三方库或外部服务集成到任务调度系统中，实现高度定制化的任务处理流程。

*   **可插拔的数据存储层**：系统设计了抽象的数据访问接口，当前版本提供了基于 MySQL 的实现。这种设计允许开发者根据项目需求，轻松切换或扩展到其他数据库系统，如 SQL Server、PostgreSQL、MongoDB 等，而无需修改核心调度逻辑。

*   **详细的任务执行日志与结果追踪**：系统会详细记录每个任务的执行历史，包括任务的开始时间、结束时间、执行状态（成功、失败、运行中）、任何发生的错误信息以及堆栈跟踪。这些日志信息对于监控任务执行情况、进行故障排查和性能分析至关重要。

*   **高可用性设计**：通过分布式部署、节点健康检查以及对任务重试机制的架构支持（尽管具体重试逻辑可能需要开发者根据业务需求自行实现或集成第三方库），MonkeyScheduler 致力于提供一个高可用的任务调度解决方案，最大限度地减少因单点故障导致的服务中断。

### 1.3. 适用场景

MonkeyScheduler 适用于各种需要自动化、可靠执行定时任务的场景，包括但不限于：

*   **数据同步与备份**：定时从不同系统同步数据，或定期备份关键业务数据。
*   **报表生成与发送**：每天、每周或每月自动生成销售报表、运营数据分析报告并发送给指定用户。
*   **系统维护任务**：如定时清理日志文件、数据库优化、缓存刷新等。
*   **批量数据处理**：定时处理用户上传的文件、生成缩略图、进行数据清洗和转换。
*   **消息队列消费**：定时从消息队列中拉取并处理消息。
*   **第三方服务集成**：定时调用第三方 API 获取数据或触发外部系统操作。
*   **定时通知与提醒**：如用户生日提醒、订单状态变更通知等。

无论你的应用规模大小，MonkeyScheduler 都能提供一个稳定、高效的任务调度基础。



## 2. 系统架构与设计理念

MonkeyScheduler 的设计核心在于其分布式、模块化和可扩展的架构，旨在提供一个高性能、高可用且易于维护的任务调度解决方案。整个系统由几个关键组件协同工作，共同完成任务的调度、分发和执行。

### 2.1. 整体架构概览

MonkeyScheduler 采用典型的分布式系统架构，将任务调度和任务执行的职责清晰分离。其主要组成部分包括：

*   **调度服务 (Scheduling Server)**：作为系统的“大脑”，负责任务的集中管理、调度逻辑的触发、任务的分发以及工作节点的健康监控。它不直接执行任务，而是将任务分配给合适的工作节点。

*   **工作节点服务 (Worker Service)**：作为系统的“执行者”，负责接收调度服务分发的任务，并根据预定义的业务逻辑执行这些任务。工作节点可以部署多个实例，实现任务的并行处理和负载均衡。

*   **数据存储层**：用于持久化存储任务的元数据（如 CRON 表达式、任务名称、任务数据）、任务执行日志以及工作节点的状态信息。当前版本默认支持 MySQL 数据库，但通过抽象接口设计，可以轻松扩展到其他数据库。

*   **核心库 (MonkeyScheduler.Core)**：包含了整个系统的核心逻辑，如 CRON 表达式解析、任务调度算法、任务分发接口以及任务执行器接口等。它是调度服务和工作节点服务的基础依赖。

下图展示了 MonkeyScheduler 的整体架构：

```mermaid
graph TD
    A[用户/管理界面] -->|API调用| B(调度服务 - Scheduling Server)
    B -->|任务分发| C{工作节点服务 - Worker Service}
    B -->|心跳检测/节点管理| C
    C -->|执行任务| D[业务逻辑/外部系统]
    B --&gt;|读写任务元数据| E[数据存储层 (MySQL)]
    C --&gt;|读写任务执行日志| E
    subgraph MonkeyScheduler System
        B
        C
        E
    end
```

### 2.2. 调度服务 (Scheduling Server)

调度服务是 MonkeyScheduler 的核心控制中心，其主要职责包括：

*   **任务管理**：提供 RESTful API 接口，允许用户或外部系统对计划任务进行 CRUD（创建、读取、更新、删除）操作。这包括设置任务的名称、CRON 表达式、任务数据以及启用/禁用状态。

*   **任务调度**：根据任务定义的 CRON 表达式，计算任务的下一次执行时间。调度服务会持续扫描即将到期的任务，并在任务到达执行时间时触发分发。

*   **任务分发**：当任务被触发时，调度服务会根据内置或自定义的负载均衡策略，选择一个健康且可用的工作节点，并将任务分发给该节点执行。任务分发通常通过 HTTP/HTTPS 请求完成。

*   **节点健康监控**：调度服务会定期接收来自工作节点的心跳报告。通过心跳机制，调度服务能够实时了解每个工作节点的运行状态。如果工作节点长时间未发送心跳，调度服务会将其标记为不健康或离线，并停止向其分发任务，从而确保任务的可靠执行。

*   **任务状态追踪**：调度服务会记录任务的当前状态（如待调度、已分发、执行中、已完成、失败等），并与数据存储层交互，更新任务的执行历史和结果。

### 2.3. 工作节点服务 (Worker Service)

工作节点服务是 MonkeyScheduler 的任务执行单元，其主要职责是：

*   **任务接收**：监听来自调度服务的任务分发请求，接收具体的任务信息（任务名称、任务数据等）。

*   **任务执行**：根据接收到的任务信息，调用预先注册的 `ITaskExecutor` 实现来执行实际的业务逻辑。`ITaskExecutor` 是一个可扩展的接口，允许开发者将任何 .NET 代码封装为任务。

*   **心跳报告**：定期向调度服务发送心跳信号，报告自身的健康状态和可用性。这是调度服务进行节点健康监控的基础。

*   **任务执行日志记录**：记录任务的执行过程、结果、耗时以及任何发生的异常信息，并将这些日志持久化到数据存储层，以便后续的审计和故障排查。

*   **结果反馈**：在任务执行完成后，工作节点可以将执行结果（成功或失败）反馈给调度服务，以便调度服务更新任务状态。

### 2.4. 数据存储层

数据存储层是 MonkeyScheduler 的持久化基础设施，负责存储所有关键数据，确保系统在重启或故障后能够恢复状态。主要存储以下类型的数据：

*   **任务元数据**：包括任务的唯一标识、名称、CRON 表达式、任务数据、启用状态、下一次运行时间等。
*   **任务执行日志**：记录每次任务执行的详细信息，如开始时间、结束时间、执行状态、错误信息、堆栈跟踪等。
*   **工作节点信息**：存储注册的工作节点列表、其最近的心跳时间、状态等。

MonkeyScheduler 通过抽象的数据访问接口（如 `ITaskRepository`）实现了数据存储的可插拔性。当前版本提供了基于 Dapper 和 MySQL 的实现，开发者可以根据需要实现其他数据库的适配。

### 2.5. 核心组件交互

MonkeyScheduler 的核心组件通过以下方式进行交互：

1.  **任务注册**：用户通过调度服务的 API 注册新任务，任务元数据被持久化到数据存储层。
2.  **任务扫描与触发**：调度服务周期性地从数据存储层读取任务列表，并根据 CRON 表达式计算任务的下一次运行时间。当任务到达预定时间时，调度服务触发任务分发流程。
3.  **工作节点注册与心跳**：工作节点服务启动后，会向调度服务注册自身，并定期发送心跳。调度服务维护一个健康的工作节点列表。
4.  **任务分发**：调度服务从健康的工作节点列表中选择一个节点，通过 HTTP 请求将任务信息发送给该工作节点。
5.  **任务执行**：工作节点接收到任务后，调用其内部注册的 `ITaskExecutor` 来执行任务的业务逻辑。
6.  **执行结果与日志**：任务执行过程中产生的日志和最终结果会被工作节点记录，并通过数据存储层持久化。工作节点也可以将简要结果反馈给调度服务。
7.  **状态更新**：调度服务根据任务执行结果和工作节点心跳，更新任务和节点的状态信息到数据存储层。

这种松耦合的架构设计使得 MonkeyScheduler 能够灵活应对不同的部署环境和业务需求，同时保证了系统的高性能和高可用性。



## 3. 安装与配置

本章节将详细指导你如何安装和配置 MonkeyScheduler 的各个组件，以便你能够顺利地部署和运行任务调度系统。

### 3.1. 环境准备

在开始安装之前，请确保你的开发或部署环境满足以下要求：

*   **.NET SDK 6.0 或更高版本**：MonkeyScheduler 是基于 .NET 平台开发的，因此你需要安装相应版本的 .NET SDK。你可以从 [Microsoft 官方网站](https://dotnet.microsoft.com/download) 下载并安装。
*   **MySQL 数据库**：MonkeyScheduler 默认使用 MySQL 作为数据存储。你需要一个运行中的 MySQL 服务器实例，并拥有创建数据库和表的权限。推荐使用 MySQL 8.0 或更高版本。
*   **网络连接**：确保调度服务和工作节点服务之间可以相互访问，并且它们都能够连接到 MySQL 数据库。

### 3.2. 通过 NuGet 安装

MonkeyScheduler 的各个组件都以 NuGet 包的形式提供，你可以通过 NuGet 包管理器或 .NET CLI 将它们添加到你的项目中。

打开你的项目文件 (`.csproj`) 或在命令行中执行以下命令：

```shell
# 核心组件：包含 MonkeyScheduler 的核心接口和逻辑。调度和工作节点项目都需要安装。
Install-Package MonkeyScheduler

# 调度服务组件：用于构建调度服务应用程序。
Install-Package MonkeyScheduler.SchedulerService

# 工作节点服务组件：用于构建工作节点服务应用程序。
Install-Package MonkeyScheduler.WorkerService

# MySQL 数据访问组件：提供 MySQL 数据库的实现。调度和工作节点项目都需要安装。
Install-Package MonkeyScheduler.Data.MySQL
```

如果你使用的是 .NET CLI，可以使用 `dotnet add package` 命令：

```bash
# 在你的调度项目目录下执行
dotnet add package MonkeyScheduler
dotnet add package MonkeyScheduler.SchedulerService
dotnet add package MonkeyScheduler.Data.MySQL

# 在你的工作节点项目目录下执行
dotnet add package MonkeyScheduler
dotnet add package MonkeyScheduler.WorkerService
dotnet add package MonkeyScheduler.Data.MySQL
```

### 3.3. 数据库配置

MonkeyScheduler 需要一个 MySQL 数据库来存储任务的元数据、执行日志和节点信息。请按照以下步骤进行数据库的创建和初始化：

1.  **创建数据库**：

    首先，在你的 MySQL 服务器上创建一个新的数据库。建议使用 `MonkeyScheduler` 作为数据库名称，并设置合适的字符集和排序规则。

    ```sql
    CREATE DATABASE MonkeyScheduler CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
    ```

2.  **初始化数据库表**：

    MonkeyScheduler 提供了 SQL 脚本来自动创建所需的数据库表结构。你可以在克隆的项目源代码中找到 `MonkeyScheduler.Data.MySQL` 项目，在其内部的 `Scripts` 文件夹下有一个名为 `InitializeDatabase.sql` 的文件。请在你的 MySQL 客户端（如 MySQL Workbench, DataGrip, Navicat 或命令行）中执行此脚本，以创建所有必要的表。

    **文件路径示例**：`MonkeyScheduler/MonkeyScheduler.Data.MySQL/Scripts/InitializeDatabase.sql`

### 3.4. `appsettings.json` 配置

`appsettings.json` 文件用于配置应用程序的各种设置，包括数据库连接字符串、服务地址和调度参数等。调度服务和工作节点服务都需要进行相应的配置。

#### 3.4.1. 调度项目配置 (`appsettings.json`)

在你的调度服务项目（例如 `SchedulingServer`）的 `appsettings.json` 文件中，添加或修改以下配置节：

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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

*   `AllowedHosts`: 指定允许访问调度服务的 HTTP 请求的 Host 头。在生产环境中，建议将其设置为调度服务实际部署的域名或 IP 地址，以增强安全性。开发环境中可以使用 `*` 表示允许所有。
*   `MonkeyScheduler:Options:HeartbeatInterval`: 工作节点向调度服务发送心跳的频率。这是一个 `TimeSpan` 格式的字符串，例如 `"00:00:30"` 表示每 30 秒发送一次心跳。合理的心跳间隔有助于调度服务及时发现节点状态变化。
*   `MonkeyScheduler:Options:NodeTimeoutInterval`: 调度服务判定一个工作节点失联的超时时间。如果一个工作节点在此时间内没有发送心跳，调度服务会认为该节点已离线或不健康，并停止向其分发任务。例如 `"00:02:00"` 表示 2 分钟超时。
*   `MonkeyScheduler:Options:MaxRetryCount`: 任务的最大重试次数。请注意，虽然此配置项提供了重试次数的设定，但具体的任务重试逻辑需要你在 `ITaskExecutor` 的实现中自行处理或集成第三方库（如 Polly）。
*   `MonkeyScheduler:SchedulerDb`: **这是最重要的配置项**。你需要将其修改为你的 MySQL 数据库的实际连接字符串。请替换 `YOUR_MYSQL_HOST`, `YOUR_USERNAME`, `YOUR_PASSWORD` 为你的数据库信息。

#### 3.4.2. 工作节点项目配置 (`appsettings.json`)

在你的工作节点服务项目（例如 `WorkerService`）的 `appsettings.json` 文件中，添加或修改以下配置节：

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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

*   `MonkeyScheduler:WorkerService:Url`: 工作节点服务自身监听的 URL。这是调度服务向该工作节点分发任务时需要访问的地址。请确保这个 URL 在调度服务所在网络环境中是可访问的。
*   `MonkeyScheduler:SchedulingServer:Url`: 调度服务的访问地址。工作节点服务需要知道调度服务的地址，以便发送心跳报告和接收任务。请替换为你的调度服务实际部署的 URL。
*   `MonkeyScheduler:SchedulerDb`: 工作节点服务也需要配置数据库连接。这主要用于记录任务执行日志，确保每个任务的执行历史都能被持久化。同样，请替换为你的 MySQL 数据库的实际连接字符串。

### 3.5. `Program.cs` 配置

`Program.cs` 文件是 .NET Core 应用程序的入口点，你需要在其中注册 MonkeyScheduler 的服务和组件。

#### 3.5.1. 调度项目配置 (`Program.cs`)

在你的调度服务项目（例如 `SchedulingServer`）的 `Program.cs` 文件中，进行以下修改：

```csharp
using MonkeyScheduler.SchedulerService.Controllers;
using MonkeyScheduler.SchedulerService.Services;
using MonkeyScheduler.Data.MySQL;
using MonkeyScheduler.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器并注册类库的控制器
builder.Services.AddControllers()
    .AddApplicationPart(typeof(WorkerApiController).Assembly) // 注册 WorkerApiController
    .AddApplicationPart(typeof(TasksController).Assembly);     // 注册 TasksController

// 添加调度服务所需的服务
builder.Services.AddSchedulerService();

// 如果你需要自定义负载均衡策略，取消注释并替换 CustomLoadBalancer 为你的实现
// builder.Services.AddLoadBalancer<CustomLoadBalancer>();

// 注册 NodeRegistry 服务，用于管理工作节点信息
builder.Services.AddSingleton<INodeRegistry>(sp => 
    sp.GetRequiredService<NodeRegistry>());

// 添加 MySQL 数据访问服务
builder.Services.AddMySqlDataAccess();

// 配置 CORS 策略 (如果你的前端应用与调度服务不在同一个域，需要配置)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseCors(); // 使用 CORS 策略
app.UseAuthorization();

// 引入调度器中间件，启用调度功能
app.UseSchedulerService();

app.MapControllers();

app.Run();
```

**关键点说明**：

*   `AddApplicationPart`: 用于注册 MonkeyScheduler 内部控制器所在的程序集，这样你的调度服务才能暴露任务管理和工作节点通信的 API 接口。
*   `AddSchedulerService()`: 注册调度服务所需的所有依赖项和后台服务。
*   `AddLoadBalancer<CustomLoadBalancer>()`: 如果你实现了自定义的负载均衡策略，需要在这里注册你的实现。否则，系统将使用默认的负载均衡器。
*   `AddSingleton<INodeRegistry>()`: 注册节点注册服务，用于管理工作节点的生命周期和状态。
*   `AddMySqlDataAccess()`: 注册 MySQL 数据访问层所需的服务，包括数据库上下文和各个仓储的实现。
*   `UseSchedulerService()`: 启用调度器中间件，它会启动后台任务来扫描和分发任务。

#### 3.5.2. 工作节点项目配置 (`Program.cs`)

在你的工作节点服务项目（例如 `WorkerService`）的 `Program.cs` 文件中，进行以下修改：

```csharp
using MonkeyScheduler.WorkerService.Controllers;
using MonkeyScheduler.WorkerService.Services;
using MonkeyScheduler.Data.MySQL;
using MonkeyScheduler.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器并注册类库的控制器
builder.Services.AddControllers()
    .AddApplicationPart(typeof(TaskReceiverController).Assembly); // 注册 TaskReceiverController

// 添加 Worker 服务所需的服务
builder.Services.AddWorkerService(
    builder.Configuration["MonkeyScheduler:WorkerService:Url"] ?? "http://localhost:5001" // 从配置中获取 WorkerService 的 URL
);

// 注册自定义任务执行器。你需要替换 CustomTaskExecutor 为你实际的任务执行器实现。
// 如果有多个任务执行器，可以注册多个 ITaskExecutor 实例，或者使用一个统一的执行器来分发。
builder.Services.AddSingleton<ITaskExecutor, CustomTaskExecutor>();

// 添加 MySQL 数据访问服务
builder.Services.AddMySqlDataAccess(); 

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();

// 添加健康检查端点和启动心跳机制
app.UseWorkerService();

app.MapControllers();

app.Run();
```

**关键点说明**：

*   `AddApplicationPart`: 用于注册 MonkeyScheduler 内部控制器所在的程序集，这样你的工作节点服务才能接收来自调度服务的任务分发请求。
*   `AddWorkerService()`: 注册工作节点服务所需的所有依赖项和后台服务，包括心跳发送器和任务接收器。
*   `AddSingleton<ITaskExecutor, CustomTaskExecutor>()`: **这是非常重要的一步**。你需要在这里注册你的自定义任务执行器。`CustomTaskExecutor` 是一个示例，你需要将其替换为你实际实现的 `ITaskExecutor`。这是 MonkeyScheduler 执行你业务逻辑的入口。
*   `AddMySqlDataAccess()`: 注册 MySQL 数据访问层所需的服务，用于记录任务执行日志。
*   `UseWorkerService()`: 启用工作节点服务中间件，它会启动后台任务来发送心跳和处理任务接收。

完成以上配置后，你的 MonkeyScheduler 调度服务和工作节点服务就准备就绪了。接下来，你将学习如何启动这些服务并开始使用它们来管理和执行你的定时任务。



## 4. 快速开始与使用

本节将通过一个完整的示例，指导你如何启动 MonkeyScheduler 的调度服务和工作节点服务，定义一个自定义的任务执行器，并通过 API 添加你的第一个计划任务。

### 4.1. 启动服务

在完成安装与配置后，你可以通过以下步骤启动 MonkeyScheduler 的核心服务：

1.  **构建项目**：
    打开命令行或终端，导航到你的项目解决方案的根目录，然后运行 `dotnet build` 命令来编译所有项目。

    ```bash
    dotnet build
    ```

2.  **启动调度服务 (Scheduling Server)**：
    导航到你的调度服务项目（例如 `SchedulingServer`）的目录，然后运行 `dotnet run` 命令。服务启动后，你将在控制台看到类似以下的输出，表明调度服务正在运行并监听指定的端口。

    ```bash
    cd SchedulingServer
    dotnet run
    ```

    **控制台输出示例**：
    ```
    info: Microsoft.Hosting.Lifetime[14]
          Now listening on: http://localhost:5190
    info: Microsoft.Hosting.Lifetime[0]
          Application started. Press Ctrl+C to shut down.
    info: Microsoft.Hosting.Lifetime[0]
          Hosting environment: Development
    info: Microsoft.Hosting.Lifetime[0]
          Content root path: /path/to/your/SchedulingServer
    ```

3.  **启动工作节点服务 (Worker Service)**：
    打开一个新的命令行或终端窗口，导航到你的工作节点服务项目（例如 `WorkerService`）的目录，然后运行 `dotnet run` 命令。服务启动后，它将向调度服务注册自身并开始发送心跳。

    ```bash
    cd ../WorkerService
    dotnet run
    ```

    **控制台输出示例**：
    ```
    info: Microsoft.Hosting.Lifetime[14]
          Now listening on: http://localhost:5046
    info: Microsoft.Hosting.Lifetime[0]
          Application started. Press Ctrl+C to shut down.
    info: MonkeyScheduler.WorkerService.Services.NodeHeartbeatService[0]
          Heartbeat sent to scheduler at http://localhost:5190
    ```

现在，你的调度服务和工作节点服务都已经成功启动，并且它们之间可以相互通信。调度服务已经准备好接收任务注册请求，并将任务分发给工作节点执行。

### 4.2. 定义你的任务执行器

MonkeyScheduler 的核心在于其可扩展的任务执行器。你需要通过实现 `ITaskExecutor` 接口来定义你的具体业务逻辑。以下是一个创建自定义任务执行器的详细步骤：

1.  **创建任务执行器类**：
    在你的工作节点服务项目中，创建一个新的 C# 类文件，例如 `CustomTaskExecutor.cs`。这个类需要实现 `MonkeyScheduler.Core.Services.ITaskExecutor` 接口。

    ```csharp
    using MonkeyScheduler.Core.Services;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    
    public class CustomTaskExecutor : ITaskExecutor
    {
        private readonly ILogger<CustomTaskExecutor> _logger;
    
        // 通过依赖注入获取日志记录器
        public CustomTaskExecutor(ILogger<CustomTaskExecutor> logger)
        {
            _logger = logger;
        }
    
        // 实现 ExecuteAsync 方法，这是任务执行的入口
        public async Task ExecuteAsync(string taskName, string taskData)
        {
            _logger.LogInformation($"开始执行任务: {taskName}, 任务数据: {taskData}");
    
            try
            {
                // 在这里实现你的核心业务逻辑
                // 例如，调用外部 API、处理数据、发送通知等
                // taskData 可以是任何字符串格式，例如 JSON，你可以根据需要进行解析
    
                // 模拟一个耗时的操作
                await Task.Delay(TimeSpan.FromSeconds(5));
    
                _logger.LogInformation($"任务 {taskName} 已成功执行完成。");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"任务 {taskName} 执行失败: {ex.Message}");
                // 抛出异常，以便调度系统记录任务失败
                throw;
            }
        }
    }
    ```

2.  **注册任务执行器**：
    创建完任务执行器后，你需要在工作节点项目的 `Program.cs` 文件中将其注册到依赖注入容器中。这样，当工作节点接收到任务时，它才知道如何实例化并调用你的执行器。

    ```csharp
    // 在 Program.cs 中找到以下行
    // builder.Services.AddSingleton<ITaskExecutor, YourOriginalExecutor>();
    
    // 将其替换为你的自定义执行器
    builder.Services.AddSingleton<ITaskExecutor, CustomTaskExecutor>();
    ```

    通过以上两步，你已经成功地将你的业务逻辑集成到了 MonkeyScheduler 的工作节点中。

### 4.3. 添加计划任务

你可以通过调用调度服务提供的 RESTful API 来动态地添加、管理和删除计划任务。以下是使用 `curl` 命令添加一个新任务的示例。你也可以使用任何支持发送 HTTP 请求的工具，如 Postman、Insomnia 或在你的代码中使用 `HttpClient`。

**示例：添加一个每天午夜执行的报表生成任务**

```bash
curl -X POST \
  http://localhost:5190/api/tasks \
  -H 'Content-Type: application/json' \
  -d '{
    "taskName": "MyDailyReportTask",
    "cronExpression": "0 0 * * *",
    "taskData": "{\"reportType\": \"sales\", \"period\": \"daily\"}",
    "enabled": true
  }'
```

**请求参数说明**：

*   `taskName` (string, required): 任务的唯一名称。这个名称将用于后续对该任务的管理操作。
*   `cronExpression` (string, required): 定义任务执行周期的 CRON 表达式。例如，`"0 0 * * *"` 表示每天的午夜 00:00 执行。
*   `taskData` (string, optional): 传递给 `ITaskExecutor` 的任务数据。这可以是一个简单的字符串，也可以是一个序列化后的 JSON 对象，用于向你的任务执行器传递参数。
*   `enabled` (boolean, required): 任务是否启用。如果设置为 `false`，任务将被创建但不会被调度执行，直到你手动启用它。

**CRON 表达式示例**：

| 表达式             | 描述                                         |
| ------------------ | -------------------------------------------- |
| `"*/5 * * * * *"`  | 每 5 秒执行一次 (秒 分 时 日 月 周)          |
| `"0 */1 * * * *"`  | 每分钟执行一次 (秒 分 时 日 月 周)          |
| `"0 0 12 * * ?"`   | 每天中午 12 点执行 (秒 分 时 日 月 周)       |
| `"0 0 10,14,16 * * ?"` | 每天上午 10 点、下午 2 点和 4 点执行 (秒 分 时 日 月 周) |
| `"0 0 2 1 * ?"`    | 每月 1 日的凌晨 2 点执行 (秒 分 时 日 月 周) |

### 4.4. 任务管理 API

调度服务提供了一套完整的 RESTful API 用于任务的生命周期管理。以下是主要的 API 接口：

| HTTP 方法 | URL                               | 描述                                     |
| --------- | --------------------------------- | ---------------------------------------- |
| `POST`    | `/api/tasks`                      | 添加一个新的计划任务                     |
| `GET`     | `/api/tasks`                      | 查询所有已注册的计划任务                 |
| `GET`     | `/api/tasks/{taskName}`           | 根据任务名称查询单个任务的详细信息       |
| `PUT`     | `/api/tasks/{taskName}`           | 更新一个已存在的任务（CRON 表达式、任务数据、启用状态） |
| `POST`    | `/api/tasks/{taskName}/enable`    | 启用一个已禁用的任务                     |
| `POST`    | `/api/tasks/{taskName}/disable`   | 禁用一个已启用的任务                     |
| `DELETE`  | `/api/tasks/{taskName}`           | 删除一个计划任务                         |

通过这些 API，你可以轻松地将 MonkeyScheduler 集成到你的管理后台或自动化脚本中，实现对定时任务的全面控制。



## 5. 高级功能与扩展

MonkeyScheduler 的设计理念之一是提供高度的可扩展性，允许开发者根据自己的具体需求定制和扩展系统的行为。本节将深入探讨 MonkeyScheduler 提供的一些高级功能和扩展点。

### 5.1. 自定义负载均衡

在分布式任务调度系统中，负载均衡是确保任务高效、均匀分发到各个工作节点上的关键。MonkeyScheduler 提供了可插拔的负载均衡机制，允许你实现自己的负载均衡策略。

#### 5.1.1. `ILoadBalancer` 接口

要实现自定义负载均衡，你需要实现 `MonkeyScheduler.Core.Services` 命名空间下的 `ILoadBalancer` 接口。这个接口定义了一个方法：

```csharp
public interface ILoadBalancer
{
    /// <summary>
    /// 从可用的工作节点列表中选择一个节点来分发任务。
    /// </summary>
    /// <param name="availableWorkers">当前可用的工作节点 URL 列表。</param>
    /// <returns>被选中的工作节点 URL。</returns>
    string SelectWorker(IEnumerable<string> availableWorkers);
}
```

`SelectWorker` 方法接收一个 `IEnumerable<string>` 类型的 `availableWorkers` 参数，其中包含了当前所有健康且可用的工作节点的 URL。你的实现需要从这个列表中选择一个工作节点并返回其 URL。

#### 5.1.2. 实现自定义负载均衡器

以下是一个简单的自定义负载均衡器示例，它实现了基于请求计数的轮询策略：

```csharp
using MonkeyScheduler.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class RoundRobinLoadBalancer : ILoadBalancer
{
    private static int _lastIndex = -1;

    public string SelectWorker(IEnumerable<string> availableWorkers)
    {
        var workers = availableWorkers.ToList();
        if (!workers.Any())
        {
            throw new InvalidOperationException("没有可用的工作节点。");
        }

        // 使用 Interlocked.Increment 确保线程安全地更新索引
        int currentIndex = Interlocked.Increment(ref _lastIndex);
        
        // 计算当前索引，防止越界
        int selectedIndex = currentIndex % workers.Count;

        return workers[selectedIndex];
    }
}
```

#### 5.1.3. 注册自定义负载均衡器

实现自定义负载均衡器后，你需要在调度服务项目的 `Program.cs` 文件中将其注册到依赖注入容器中。请确保在 `builder.Services.AddSchedulerService()` 之后进行注册，并且替换掉默认的负载均衡器（如果存在）。

```csharp
// ... 其他代码 ...

// 添加调度服务
builder.Services.AddSchedulerService();

// 注册你的自定义负载均衡器
// 如果你没有自定义，可以不添加此行，系统会使用默认实现
builder.Services.AddSingleton<ILoadBalancer, RoundRobinLoadBalancer>();

// ... 其他代码 ...
```

通过这种方式，你可以根据你的业务场景（例如，基于节点负载、地理位置、特定任务类型等）实现更复杂的负载均衡逻辑。

### 5.2. 任务重试机制

在分布式系统中，任务执行失败是常态。为了提高系统的健壮性和任务的成功率，任务重试机制至关重要。MonkeyScheduler 的架构支持任务重试，但具体的重试逻辑需要开发者在任务执行器中实现或集成第三方库。

#### 5.2.1. 在 `ITaskExecutor` 中实现重试

最直接的方式是在你的 `ITaskExecutor` 实现中加入重试逻辑。你可以使用简单的 `for` 循环结合 `try-catch` 块，并引入指数退避（Exponential Backoff）策略来避免对失败任务的立即重试导致的服务压力。

```csharp
using MonkeyScheduler.Core.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class ResilientTaskExecutor : ITaskExecutor
{
    private readonly ILogger<ResilientTaskExecutor> _logger;
    private const int MaxRetries = 5; // 最大重试次数
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(1); // 初始重试间隔

    public ResilientTaskExecutor(ILogger<ResilientTaskExecutor> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(string taskName, string taskData)
    {
        for (int retryCount = 0; retryCount < MaxRetries; retryCount++)
        {
            try
            {
                _logger.LogInformation($"开始执行任务: {taskName}, 任务数据: {taskData} (尝试 {retryCount + 1}/{MaxRetries})");
                
                // 在这里放置你的实际业务逻辑
                // 模拟一个可能失败的操作
                if (new Random().Next(0, 3) == 0 && retryCount < MaxRetries - 1) 
                {
                    throw new Exception("模拟任务执行失败，需要重试。");
                }

                await Task.Delay(500); // 模拟任务执行时间

                _logger.LogInformation($"任务 {taskName} 成功执行。");
                return; // 任务成功，退出重试循环
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"任务 {taskName} 执行失败 (尝试 {retryCount + 1}/{MaxRetries}): {ex.Message}");

                if (retryCount < MaxRetries - 1)
                {
                    // 计算指数退避延迟：1s, 2s, 4s, 8s, ...
                    TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(InitialDelay.TotalSeconds * 2, retryCount));
                    _logger.LogWarning($"任务 {taskName} 将在 {delay.TotalSeconds} 秒后重试。");
                    await Task.Delay(delay);
                }
            }
        }
        _logger.LogError($"任务 {taskName} 达到最大重试次数 {MaxRetries}，最终执行失败。");
    }
}
```

#### 5.2.2. 集成 Polly

对于更复杂的重试策略，例如断路器模式、超时、缓存等，强烈推荐使用 [Polly](https://github.com/App-vNext/Polly) 这样的弹性策略库。Polly 提供了流畅的 API 来定义各种故障处理策略。

首先，通过 NuGet 安装 Polly：

```shell
Install-Package Polly
```

然后，你可以在 `ITaskExecutor` 中使用 Polly 来封装你的业务逻辑：

```csharp
using MonkeyScheduler.Core.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly; // 引入 Polly 命名空间

public class PollyTaskExecutor : ITaskExecutor
{
    private readonly ILogger<PollyTaskExecutor> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    public PollyTaskExecutor(ILogger<PollyTaskExecutor> logger)
    {
        _logger = logger;

        // 定义重试策略：重试 3 次，每次间隔 1、2、4 秒
        _retryPolicy = Policy
            .Handle<Exception>() // 捕获所有异常，你可以指定更具体的异常类型
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception, $"任务执行失败，将在 {timeSpan.TotalSeconds} 秒后进行第 {retryCount} 次重试。");
                });
    }

    public async Task ExecuteAsync(string taskName, string taskData)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            _logger.LogInformation($"开始执行任务: {taskName}, 任务数据: {taskData}");

            // 你的实际业务逻辑
            // 模拟一个可能失败的操作
            if (new Random().Next(0, 2) == 0) 
            {
                throw new InvalidOperationException("模拟业务逻辑失败。");
            }

            await Task.Delay(500); // 模拟任务执行时间

            _logger.LogInformation($"任务 {taskName} 成功执行。");
        });
    }
}
```

### 5.3. 任务持久化与恢复

MonkeyScheduler 默认将任务元数据存储在 MySQL 数据库中，这确保了任务的持久化。当调度服务重启时，它会从数据库中加载所有已启用的任务，并恢复调度。这意味着即使调度服务崩溃，已注册的任务也不会丢失。

#### 5.3.1. 数据库结构

任务信息主要存储在 `ScheduledTasks` 表中，执行日志存储在 `TaskExecutionLogs` 表中。这些表的结构定义在 `MonkeyScheduler.Data.MySQL/Scripts/InitializeDatabase.sql` 文件中。

#### 5.3.2. 任务状态管理

`ScheduledTask` 实体中包含 `Enabled` 和 `NextRunTime` 等字段，调度服务会根据这些字段来决定任务是否需要被调度和何时调度。`TaskExecutionLog` 记录了每次任务执行的详细状态，这对于审计和故障排查非常重要。

### 5.4. 监控与日志

良好的监控和日志系统是分布式应用稳定运行的基石。MonkeyScheduler 内置了日志记录功能，并支持集成到更全面的监控体系中。

#### 5.4.1. 日志记录

MonkeyScheduler 使用 `Microsoft.Extensions.Logging` 框架进行日志记录。这意味着你可以轻松地将其与各种日志提供程序集成，例如：

*   **Console**：开发环境中最常用的输出方式。
*   **Debug**：输出到调试窗口。
*   **File**：使用 Serilog 或 NLog 等库将日志写入文件。
*   **Elasticsearch/Seq**：集中式日志管理系统，便于日志的收集、查询和分析。
*   **Application Insights**：Azure 提供的应用性能管理服务。

你可以在 `appsettings.json` 中配置日志级别，例如：

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning",
    "MonkeyScheduler": "Debug" // 针对 MonkeyScheduler 相关的日志，可以设置为 Debug 级别以获取更详细的信息
  }
}
```

#### 5.4.2. 监控指标

为了更好地监控 MonkeyScheduler 的运行状态，你可以考虑收集以下关键指标：

*   **任务执行成功率**：成功执行的任务数 / 总执行任务数。
*   **任务执行失败率**：失败执行的任务数 / 总执行任务数。
*   **任务执行耗时**：平均耗时、最大耗时、P95/P99 耗时。
*   **待调度任务数量**：当前等待调度器处理的任务数量。
*   **工作节点健康状态**：在线工作节点数量、离线工作节点数量。
*   **心跳延迟**：工作节点发送心跳到调度服务接收到的延迟。
*   **数据库连接池使用情况**：连接的打开和关闭频率、活跃连接数。

你可以使用 Prometheus、Grafana 等工具来收集和可视化这些指标，并通过报警系统及时发现潜在问题。

### 5.5. 扩展任务执行器

`ITaskExecutor` 接口是 MonkeyScheduler 扩展任务执行逻辑的核心。除了简单的业务逻辑，你还可以利用它实现更复杂的集成。

#### 5.5.1. 动态任务类型

如果你的系统需要处理多种不同类型的任务，你可以设计一个统一的 `ITaskExecutor`，并在 `ExecuteAsync` 方法中根据 `taskData` 或其他元数据来分发到不同的子执行器。

例如，`taskData` 可以是一个 JSON 字符串，包含 `"taskType"` 字段：

```json
{
  "taskType": "EmailSender",
  "recipient": "test@example.com",
  "subject": "Hello from MonkeyScheduler"
}
```

你的 `ITaskExecutor` 可以解析这个 JSON，并根据 `taskType` 调用相应的处理逻辑。

#### 5.5.2. 外部服务调用

`ITaskExecutor` 可以用于调用外部的微服务、消息队列、第三方 API 等。例如，你可以创建一个 `HttpClientTaskExecutor`，它接收一个 URL 和请求体作为 `taskData`，然后发送 HTTP 请求。

#### 5.5.3. 批处理任务

对于需要处理大量数据的批处理任务，`ITaskExecutor` 可以负责启动一个批处理作业，并监控其完成状态。例如，它可以触发一个 Azure Batch Job 或 AWS Lambda 函数。

通过灵活运用 `ITaskExecutor`，MonkeyScheduler 能够适应各种复杂的业务场景，成为你自动化工作流的强大引擎。



## 6. API 参考

MonkeyScheduler 提供了清晰的 RESTful API 接口，用于任务的创建、查询、管理以及工作节点与调度服务之间的通信。本节将详细介绍这些 API 接口及其使用方法。

### 6.1. 调度服务 API

调度服务 (Scheduling Server) 暴露了一系列用于任务管理的 HTTP API。所有 API 均以 `/api/tasks` 为基础路径。

#### 6.1.1. 创建任务

*   **URL**: `/api/tasks`
*   **方法**: `POST`
*   **描述**: 创建一个新的计划任务。
*   **请求体**: `application/json`

    ```json
    {
      "taskName": "string",         // 任务的唯一名称，例如 "DailyReportGeneration"
      "cronExpression": "string",   // CRON 表达式，例如 "0 0 * * *" (每天午夜)
      "taskData": "string",         // 任务执行时传递给 ITaskExecutor 的数据，可以是 JSON 字符串
      "enabled": true               // 任务是否启用，默认为 true
    }
    ```

*   **响应**: `application/json`

    *   **成功 (201 Created)**: 返回创建的任务信息。
        ```json
        {
          "taskName": "string",
          "cronExpression": "string",
          "taskData": "string",
          "enabled": true,
          "nextRunTime": "2025-07-03T00:00:00Z" // 下一次运行时间 (UTC)
        }
        ```
    *   **错误 (400 Bad Request)**: 请求参数无效。
    *   **错误 (409 Conflict)**: 任务名称已存在。

#### 6.1.2. 查询所有任务

*   **URL**: `/api/tasks`
*   **方法**: `GET`
*   **描述**: 获取所有已注册的计划任务列表。
*   **响应**: `application/json`

    *   **成功 (200 OK)**: 返回任务列表。
        ```json
        [
          {
            "taskName": "string",
            "cronExpression": "string",
            "taskData": "string",
            "enabled": true,
            "nextRunTime": "2025-07-03T00:00:00Z"
          }
        ]
        ```

#### 6.1.3. 查询单个任务

*   **URL**: `/api/tasks/{taskName}`
*   **方法**: `GET`
*   **描述**: 根据任务名称获取单个任务的详细信息。
*   **路径参数**: `taskName` (string) - 任务的唯一名称。
*   **响应**: `application/json`

    *   **成功 (200 OK)**: 返回任务信息。
        ```json
        {
          "taskName": "string",
          "cronExpression": "string",
          "taskData": "string",
          "enabled": true,
          "nextRunTime": "2025-07-03T00:00:00Z"
        }
        ```
    *   **错误 (404 Not Found)**: 任务不存在。

#### 6.1.4. 更新任务

*   **URL**: `/api/tasks/{taskName}`
*   **方法**: `PUT`
*   **描述**: 更新一个已存在的任务的 CRON 表达式、任务数据或启用状态。
*   **路径参数**: `taskName` (string) - 任务的唯一名称。
*   **请求体**: `application/json`

    ```json
    {
      "cronExpression": "string",   // 新的 CRON 表达式
      "taskData": "string",         // 新的任务数据
      "enabled": true               // 新的启用状态
    }
    ```

*   **响应**: `application/json`

    *   **成功 (200 OK)**: 返回更新后的任务信息。
    *   **错误 (400 Bad Request)**: 请求参数无效。
    *   **错误 (404 Not Found)**: 任务不存在。

#### 6.1.5. 启用任务

*   **URL**: `/api/tasks/{taskName}/enable`
*   **方法**: `POST`
*   **描述**: 启用一个已禁用的任务。
*   **路径参数**: `taskName` (string) - 任务的唯一名称。
*   **响应**: `application/json`

    *   **成功 (200 OK)**: 返回启用后的任务信息。
    *   **错误 (404 Not Found)**: 任务不存在。

#### 6.1.6. 禁用任务

*   **URL**: `/api/tasks/{taskName}/disable`
*   **方法**: `POST`
*   **描述**: 禁用一个已启用的任务。
*   **路径参数**: `taskName` (string) - 任务的唯一名称。
*   **响应**: `application/json`

    *   **成功 (200 OK)**: 返回禁用后的任务信息。
    *   **错误 (404 Not Found)**: 任务不存在。

#### 6.1.7. 删除任务

*   **URL**: `/api/tasks/{taskName}`
*   **方法**: `DELETE`
*   **描述**: 删除一个计划任务。
*   **路径参数**: `taskName` (string) - 任务的唯一名称。
*   **响应**: `application/json`

    *   **成功 (204 No Content)**: 任务成功删除。
    *   **错误 (404 Not Found)**: 任务不存在。

### 6.2. 工作节点服务 API

工作节点服务 (Worker Service) 主要暴露一个用于接收任务分发的 API 接口，以及内部用于心跳报告的接口。

#### 6.2.1. 接收任务

*   **URL**: `/api/worker/execute`
*   **方法**: `POST`
*   **描述**: 接收调度服务分发的任务并执行。此 API 通常由调度服务内部调用，不建议直接从外部调用。
*   **请求体**: `application/json`

    ```json
    {
      "taskName": "string",         // 任务的名称
      "taskData": "string"          // 任务数据
    }
    ```

*   **响应**: `application/json`

    *   **成功 (200 OK)**: 任务已接收并开始执行。
        ```json
        {
          "status": "success",
          "message": "Task received and started execution."
        }
        ```
    *   **错误 (500 Internal Server Error)**: 任务执行失败。

### 6.3. 核心库接口

MonkeyScheduler 的核心库 (`MonkeyScheduler.Core`) 定义了多个重要的接口，这些接口是系统可扩展性的基础。开发者可以通过实现这些接口来定制系统的行为。

#### 6.3.1. `ITaskExecutor`

*   **命名空间**: `MonkeyScheduler.Core.Services`
*   **描述**: 定义了任务执行的契约。所有需要由 MonkeyScheduler 执行的业务逻辑都必须实现此接口。

    ```csharp
    public interface ITaskExecutor
    {
        /// <summary>
        /// 异步执行任务的业务逻辑。
        /// </summary>
        /// <param name="taskName">任务的名称。</param>
        /// <param name="taskData">任务数据，由调度服务传递。</param>
        /// <returns>表示异步操作的任务。</returns>
        Task ExecuteAsync(string taskName, string taskData);
    }
    ```

#### 6.3.2. `ILoadBalancer`

*   **命名空间**: `MonkeyScheduler.Core.Services`
*   **描述**: 定义了负载均衡策略的契约。允许开发者自定义任务分发到工作节点的逻辑。

    ```csharp
    public interface ILoadBalancer
    {
        /// <summary>
        /// 从可用的工作节点列表中选择一个节点来分发任务。
        /// </summary>
        /// <param name="availableWorkers">当前可用的工作节点 URL 列表。</param>
        /// <returns>被选中的工作节点 URL。</returns>
        string SelectWorker(IEnumerable<string> availableWorkers);
    }
    ```

#### 6.3.3. `ITaskRepository`

*   **命名空间**: `MonkeyScheduler.Storage`
*   **描述**: 定义了任务数据持久化的契约。允许开发者集成不同的数据存储后端。

    ```csharp
    public interface ITaskRepository
    {
        Task AddTaskAsync(ScheduledTask task);
        Task<ScheduledTask?> GetTaskByNameAsync(string taskName);
        Task<IEnumerable<ScheduledTask>> GetAllTasksAsync();
        Task UpdateTaskAsync(ScheduledTask task);
        Task DeleteTaskAsync(string taskName);
        // ... 其他可能的 CRUD 操作
    }
    ```

#### 6.3.4. `ILogRepository`

*   **命名空间**: `MonkeyScheduler.Storage`
*   **描述**: 定义了任务执行日志持久化的契约。

    ```csharp
    public interface ILogRepository
    {
        Task AddLogAsync(TaskExecutionLog log);
        Task<IEnumerable<TaskExecutionLog>> GetLogsByTaskNameAsync(string taskName);
        // ... 其他可能的日志查询操作
    }
    ```

通过这些接口，MonkeyScheduler 实现了高度的模块化和可扩展性，使得开发者可以根据具体需求轻松地替换或扩展系统的各个组件。



## 7. 贡献指南

我们非常欢迎社区成员为 MonkeyScheduler 项目做出贡献！无论是 Bug 修复、新功能开发、文档改进还是提供反馈，您的参与都将帮助 MonkeyScheduler 变得更好。请遵循以下指南，以确保贡献过程顺畅高效。

### 7.1. 如何贡献

1.  **报告 Bug**：如果您在使用 MonkeyScheduler 过程中发现任何 Bug，请在 GitHub Issues 页面提交一个详细的 Bug 报告。请尽可能提供重现步骤、错误信息和您的环境信息。

2.  **提出功能请求**：如果您有新的功能想法或改进建议，也请在 GitHub Issues 页面提出。在提交之前，请先搜索是否已有类似的请求，避免重复。

3.  **贡献代码**：
    *   **Fork 项目**：首先，在 GitHub 上 Fork `MiaoShuYo/MonkeyScheduler` 仓库到您自己的账户。
    *   **克隆仓库**：将您 Fork 后的仓库克隆到本地开发环境。
        ```bash
        git clone https://github.com/YOUR_USERNAME/MonkeyScheduler.git
        cd MonkeyScheduler
        ```
    *   **切换到 `dev` 分支**：所有新功能和 Bug 修复都应该基于 `dev` 分支进行开发。
        ```bash
        git checkout dev
        ```
    *   **创建新分支**：为您的功能或 Bug 修复创建一个新的、描述性的分支。例如：`feature/add-new-scheduler-type` 或 `bugfix/fix-cron-parser-issue`。
        ```bash
        git checkout -b feature/your-feature-name
        ```
    *   **编写代码**：实现您的功能或修复 Bug。请确保您的代码风格与现有代码库保持一致，并编写清晰、简洁的代码。
    *   **编写测试**：为您的更改编写相应的单元测试和/或集成测试，确保您的代码按预期工作，并且没有引入新的 Bug。测试覆盖率是衡量代码质量的重要指标。
    *   **提交更改**：提交您的代码，并编写清晰、有意义的提交信息。请遵循 [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) 规范（可选但推荐），例如 `feat: add new task retry mechanism` 或 `fix: resolve database connection issue`。
    *   **同步上游**：在提交 Pull Request 之前，请确保您的分支与上游 `dev` 分支保持同步，以避免合并冲突。
        ```bash
        git fetch upstream
        git rebase upstream/dev
        ```
    *   **创建 Pull Request (PR)**：将您的分支推送到 GitHub，然后创建一个 Pull Request 到 `MiaoShuYo/MonkeyScheduler` 仓库的 `dev` 分支。请在 PR 描述中详细说明您的更改内容、解决的问题或实现的功能，以及相关的测试。

### 7.2. 开发环境搭建

1.  **安装 .NET SDK**：确保您的开发机器上安装了 .NET SDK 6.0 或更高版本。您可以从 [Microsoft 官方网站](https://dotnet.microsoft.com/download) 下载。
2.  **安装 Visual Studio 或 Visual Studio Code**：推荐使用这些 IDE 进行开发，它们提供了强大的 .NET 开发工具和调试功能。
3.  **安装 MySQL**：如果您需要进行本地开发和测试，请确保安装了 MySQL 服务器，并按照 [安装与配置](#3-安装与配置) 中的说明进行数据库初始化。

### 7.3. 测试

MonkeyScheduler 项目包含多个测试项目（例如 `MonkeySchedulerTest`、`MonkeyScheduler.Data.MySQL.Tests` 等）。在提交代码之前，请务必运行所有测试，确保您的更改没有破坏现有功能。

在项目根目录运行以下命令来执行所有测试：

```bash
dotnet test
```

### 7.4. 提交规范

为了保持提交历史的整洁和可读性，我们建议遵循以下提交信息规范：

*   **类型 (Type)**：说明提交的类型，例如 `feat` (新功能), `fix` (Bug 修复), `docs` (文档), `style` (代码风格), `refactor` (重构), `test` (测试), `chore` (构建过程或辅助工具的变动) 等。
*   **范围 (Scope)**：可选，表示本次提交影响的范围，例如 `core`, `scheduler-service`, `worker-service`, `mysql-data` 等。
*   **描述 (Description)**：简短的描述，不超过 50 个字符，使用祈使句。

**示例**：

```
feat(scheduler-service): add new task retry mechanism

This commit introduces a new configurable task retry mechanism in the scheduler service.
It uses Polly for robust retry policies with exponential backoff.

Fixes #123
```

## 8. 常见问题 (FAQ)

本节收集了 MonkeyScheduler 用户可能遇到的一些常见问题及其解答。

**Q1: MonkeyScheduler 支持哪些 CRON 表达式格式？**

A1: MonkeyScheduler 支持标准的 5 字段（分 时 日 月 周）和扩展的 6 字段（秒 分 时 日 月 周）CRON 表达式。这意味着您可以精确到秒级来定义任务的执行频率。

**Q2: 如何添加我的自定义任务逻辑？**

A2: 您需要在工作节点服务项目中实现 `ITaskExecutor` 接口，并在 `Program.cs` 中将其注册到依赖注入容器。`ITaskExecutor` 的 `ExecuteAsync` 方法是您编写业务逻辑的地方。

**Q3: 调度服务和工作节点服务可以部署在不同的机器上吗？**

A3: 是的，MonkeyScheduler 被设计为分布式系统，调度服务和工作节点服务可以部署在不同的机器上。您只需要确保它们之间可以通过网络相互访问，并在 `appsettings.json` 中正确配置它们的 URL。

**Q4: 如果工作节点服务宕机了，正在执行的任务会怎么样？**

A4: 如果工作节点服务在执行任务过程中宕机，该任务的执行会中断。MonkeyScheduler 的架构支持任务重试机制（需要您在 `ITaskExecutor` 中实现或集成相关库），以便在工作节点恢复后重新尝试执行失败的任务，从而提高系统的容错性。

**Q5: 如何确保任务不会重复执行？**

A5: MonkeyScheduler 的调度服务会确保每个任务在同一时间点只被分发一次。但在分布式环境中，由于网络延迟或节点故障，任务可能会被重复分发（尽管概率较低）。建议您的 `ITaskExecutor` 实现具备幂等性，即多次执行同一个任务不会产生副作用。

**Q6: 我可以使用除 MySQL 之外的其他数据库吗？**

A6: MonkeyScheduler 的数据存储层是可插拔的。当前版本提供了 MySQL 的实现。如果您需要使用其他数据库（如 SQL Server, PostgreSQL, MongoDB 等），您可以实现 `ITaskRepository` 和 `ILogRepository` 接口，并注册您的自定义数据访问层。

**Q7: 如何监控 MonkeyScheduler 的运行状态？**

A7: 您可以通过查看调度服务和工作节点服务的日志来监控其运行状态。此外，您可以集成 Prometheus、Grafana 等监控工具，收集任务执行成功率、失败率、耗时、节点健康状态等指标，以便更全面地了解系统运行情况。

**Q8: 任务执行失败后，如何获取错误信息？**

A8: 任务执行失败的详细信息会记录在数据库的 `TaskExecutionLogs` 表中。您可以通过查询这些日志来获取任务的错误信息和堆栈跟踪，以便进行故障排查。

**Q9: 如何自定义负载均衡策略？**

A9: 您可以实现 `ILoadBalancer` 接口来定义自己的负载均衡逻辑，并在调度服务的 `Program.cs` 中注册您的自定义实现。这允许您根据特定的业务需求（例如，基于节点负载、任务类型等）来分发任务。

## 9. 许可证

MonkeyScheduler 采用 MIT 许可证。这意味着您可以自由地使用、修改和分发本软件，但需要保留原始的版权声明和许可证信息。详情请参阅项目根目录下的 `LICENSE` 文件（如果存在）。



