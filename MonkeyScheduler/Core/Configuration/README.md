# MonkeyScheduler 强类型配置管理

## 概述

MonkeyScheduler 现在使用强类型配置类来管理所有配置项，这提供了以下优势：

- **类型安全**：编译时检查配置项的类型
- **智能提示**：IDE 提供完整的配置项提示
- **配置验证**：自动验证配置项的有效性
- **更好的可读性**：配置结构清晰，易于理解

## 配置结构

### 根配置类

```csharp
public class MonkeySchedulerConfiguration
{
    public DatabaseConfiguration Database { get; set; } = new();
    public RetryConfiguration Retry { get; set; } = new();
    public SchedulerConfiguration Scheduler { get; set; } = new();
    public WorkerConfiguration Worker { get; set; } = new();
    public LoadBalancerConfiguration LoadBalancer { get; set; } = new();
    public LoggingConfiguration Logging { get; set; } = new();
    public SecurityConfiguration Security { get; set; } = new();
}
```

### 子配置类

#### 数据库配置 (DatabaseConfiguration)
- `MySQL`: MySQL 连接字符串
- `DatabaseType`: 数据库类型 (MySQL, SqlServer, PostgreSQL, Sqlite)
- `ConnectionTimeoutSeconds`: 连接超时时间
- `CommandTimeoutSeconds`: 命令超时时间
- `EnableConnectionPooling`: 是否启用连接池
- `MaxPoolSize`: 连接池最大大小
- `MinPoolSize`: 连接池最小大小

#### 重试配置 (RetryConfiguration)
- `EnableRetry`: 是否启用重试功能
- `DefaultMaxRetryCount`: 默认最大重试次数
- `DefaultRetryIntervalSeconds`: 默认重试间隔
- `DefaultRetryStrategy`: 默认重试策略 (Exponential, Linear, Fixed)
- `DefaultTimeoutSeconds`: 默认超时时间
- `MaxRetryIntervalSeconds`: 最大重试间隔
- `RetryCooldownSeconds`: 重试冷却时间
- `DisableTaskOnMaxRetries`: 达到最大重试次数时是否禁用任务
- `SkipFailedNodes`: 是否跳过失败的节点
- `EnableRetryLogging`: 是否启用重试日志记录

#### 调度器配置 (SchedulerConfiguration)
- `CheckIntervalMilliseconds`: 调度器检查间隔
- `ExecuteDueTasksOnStartup`: 是否在启动时立即执行到期任务
- `MaxConcurrentTasks`: 最大并发执行任务数
- `TaskExecutionTimeoutSeconds`: 任务执行超时时间
- `EnableTaskExecutionLogging`: 是否启用任务执行日志
- `EnableTaskStatistics`: 是否启用任务统计信息
- `StatisticsCollectionIntervalSeconds`: 统计信息收集间隔
- `EnableHealthCheck`: 是否启用健康检查
- `HealthCheckIntervalSeconds`: 健康检查间隔

#### Worker 配置 (WorkerConfiguration)
- `WorkerUrl`: Worker 服务 URL
- `SchedulerUrl`: 调度器服务 URL
- `HeartbeatIntervalSeconds`: 心跳间隔
- `StatusReportIntervalSeconds`: 状态上报间隔
- `TaskExecutionTimeoutSeconds`: 任务执行超时时间
- `MaxConcurrentTasks`: 最大并发执行任务数
- `EnableTaskExecutionLogging`: 是否启用任务执行日志
- `EnableHealthCheck`: 是否启用健康检查
- `HealthCheckIntervalSeconds`: 健康检查间隔
- `AutoRegisterToScheduler`: 是否自动注册到调度器
- `RegistrationRetryIntervalSeconds`: 注册重试间隔
- `MaxRegistrationRetryCount`: 最大注册重试次数

#### 负载均衡器配置 (LoadBalancerConfiguration)
- `Strategy`: 负载均衡策略 (RoundRobin, LeastConnection, WeightedRoundRobin, Random, IpHash)
- `HealthCheckIntervalSeconds`: 节点健康检查间隔
- `NodeTimeoutSeconds`: 节点超时时间
- `MaxFailureCount`: 最大失败次数
- `NodeRecoveryTimeSeconds`: 节点恢复时间
- `EnableNodeWeighting`: 是否启用节点权重
- `DefaultNodeWeight`: 默认节点权重
- `EnableSessionAffinity`: 是否启用会话亲和性
- `SessionAffinityTimeoutSeconds`: 会话亲和性超时时间

#### 日志配置 (LoggingConfiguration)
- `LogLevel`: 日志级别
- `EnableStructuredLogging`: 是否启用结构化日志
- `LogFilePath`: 日志文件路径
- `MaxLogFileSizeMB`: 最大日志文件大小
- `RetainedLogFileCount`: 保留的日志文件数量
- `EnableConsoleLogging`: 是否启用控制台日志
- `EnableFileLogging`: 是否启用文件日志

#### 安全配置 (SecurityConfiguration)
- `EnableAuthentication`: 是否启用身份验证
- `EnableAuthorization`: 是否启用授权
- `JwtSecret`: JWT 密钥
- `JwtExpirationHours`: JWT 过期时间
- `EnableHttps`: 是否启用 HTTPS
- `AllowedCorsOrigins`: 允许的 CORS 源
- `ApiKey`: API 密钥

## 使用方法

### 1. 在 appsettings.json 中配置

```json
{
  "MonkeyScheduler": {
    "Database": {
      "MySQL": "Server=localhost;Database=monkeyscheduler;User=root;Password=password;",
      "DatabaseType": "MySQL",
      "ConnectionTimeoutSeconds": 30,
      "CommandTimeoutSeconds": 60,
      "EnableConnectionPooling": true,
      "MaxPoolSize": 100,
      "MinPoolSize": 5
    },
    "Retry": {
      "EnableRetry": true,
      "DefaultMaxRetryCount": 3,
      "DefaultRetryIntervalSeconds": 60,
      "DefaultRetryStrategy": "Exponential",
      "DefaultTimeoutSeconds": 300,
      "MaxRetryIntervalSeconds": 3600,
      "RetryCooldownSeconds": 300,
      "DisableTaskOnMaxRetries": false,
      "SkipFailedNodes": true,
      "EnableRetryLogging": true
    },
    "Scheduler": {
      "CheckIntervalMilliseconds": 1000,
      "ExecuteDueTasksOnStartup": true,
      "MaxConcurrentTasks": 10,
      "TaskExecutionTimeoutSeconds": 300,
      "EnableTaskExecutionLogging": true,
      "EnableTaskStatistics": true,
      "StatisticsCollectionIntervalSeconds": 60,
      "EnableHealthCheck": true,
      "HealthCheckIntervalSeconds": 30
    },
    "Worker": {
      "WorkerUrl": "http://localhost:4058",
      "SchedulerUrl": "http://localhost:4057",
      "HeartbeatIntervalSeconds": 30,
      "StatusReportIntervalSeconds": 60,
      "TaskExecutionTimeoutSeconds": 300,
      "MaxConcurrentTasks": 5,
      "EnableTaskExecutionLogging": true,
      "EnableHealthCheck": true,
      "HealthCheckIntervalSeconds": 30,
      "AutoRegisterToScheduler": true,
      "RegistrationRetryIntervalSeconds": 60,
      "MaxRegistrationRetryCount": 10
    },
    "LoadBalancer": {
      "Strategy": "LeastConnection",
      "HealthCheckIntervalSeconds": 30,
      "NodeTimeoutSeconds": 60,
      "MaxFailureCount": 3,
      "NodeRecoveryTimeSeconds": 300,
      "EnableNodeWeighting": true,
      "DefaultNodeWeight": 1,
      "EnableSessionAffinity": false,
      "SessionAffinityTimeoutSeconds": 1800
    },
    "Logging": {
      "LogLevel": "Information",
      "EnableStructuredLogging": true,
      "LogFilePath": "logs/monkeyscheduler.log",
      "MaxLogFileSizeMB": 100,
      "RetainedLogFileCount": 30,
      "EnableConsoleLogging": true,
      "EnableFileLogging": true
    },
    "Security": {
      "EnableAuthentication": false,
      "EnableAuthorization": false,
      "JwtSecret": "",
      "JwtExpirationHours": 24,
      "EnableHttps": false,
      "AllowedCorsOrigins": [],
      "ApiKey": ""
    }
  }
}
```

### 2. 在 Program.cs 中注册配置

```csharp
using MonkeyScheduler.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 添加并验证 MonkeyScheduler 配置
builder.Services.AddMonkeySchedulerConfiguration(builder.Configuration);

// 验证配置
var validationResults = builder.Configuration.ValidateMonkeySchedulerConfiguration();
if (validationResults.Any())
{
    foreach (var result in validationResults)
    {
        Console.WriteLine($"配置验证失败: {result.ErrorMessage}");
    }
    return;
}

// 其他服务注册...
```

### 3. 在服务中使用配置

```csharp
public class MyService
{
    private readonly RetryConfiguration _retryConfig;
    private readonly SchedulerConfiguration _schedulerConfig;

    public MyService(IOptions<RetryConfiguration> retryConfig, IOptions<SchedulerConfiguration> schedulerConfig)
    {
        _retryConfig = retryConfig.Value;
        _schedulerConfig = schedulerConfig.Value;
    }

    public void DoSomething()
    {
        if (_retryConfig.EnableRetry)
        {
            // 使用重试配置
            var maxRetries = _retryConfig.DefaultMaxRetryCount;
            var interval = _retryConfig.DefaultRetryIntervalSeconds;
        }

        if (_schedulerConfig.EnableTaskExecutionLogging)
        {
            // 使用调度器配置
            var checkInterval = _schedulerConfig.CheckIntervalMilliseconds;
        }
    }
}
```

### 4. 使用配置扩展方法

```csharp
// 获取特定配置
var databaseConfig = configuration.GetDatabaseConfiguration();
var retryConfig = configuration.GetRetryConfiguration();
var schedulerConfig = configuration.GetSchedulerConfiguration();

// 获取完整配置
var fullConfig = configuration.GetMonkeySchedulerConfiguration();
```

## 配置验证

系统会自动验证配置的有效性，包括：

- 数值范围检查（如超时时间必须大于0）
- 字符串非空检查（如连接字符串不能为空）
- 逻辑关系检查（如最大重试间隔不能小于默认重试间隔）
- 依赖关系检查（如启用身份验证时JWT密钥不能为空）

如果配置验证失败，系统会在启动时报告错误并停止启动。

## 环境特定配置

可以使用环境特定的配置文件来覆盖默认配置：

- `appsettings.Development.json` - 开发环境
- `appsettings.Production.json` - 生产环境
- `appsettings.Staging.json` - 测试环境

## 配置热重载

配置支持热重载，可以在运行时修改配置文件，系统会自动重新加载配置（某些配置项可能需要重启服务才能生效）。

## 最佳实践

1. **使用强类型配置**：避免直接使用 `IConfiguration` 访问配置项
2. **验证配置**：在应用启动时验证配置的有效性
3. **使用默认值**：为所有配置项提供合理的默认值
4. **环境分离**：使用不同的配置文件管理不同环境的配置
5. **敏感信息保护**：使用用户密钥或环境变量存储敏感信息
6. **配置文档化**：为所有配置项提供清晰的文档说明 