# 任务类型插件机制使用指南

## 概述

MonkeyScheduler 提供了强大的任务类型插件机制，允许您轻松扩展系统支持的任务类型。通过实现 `ITaskHandler` 接口，您可以创建自定义的任务处理器来执行各种类型的任务。

## 内置任务类型

### 1. HTTP任务处理器 (`http`)

支持发送HTTP请求的任务类型。

**参数示例：**
```json
{
  "url": "https://api.example.com/data",
  "method": "POST",
  "body": "{\"key\":\"value\"}",
  "contentType": "application/json",
  "headers": {
    "Authorization": "Bearer token123"
  },
  "timeout": 30
}
```

**使用场景：**
- 调用外部API
- 发送Webhook通知
- 数据同步

### 2. SQL任务处理器 (`sql`)

支持执行SQL脚本的任务类型。

**参数示例：**
```json
{
  "sqlScript": "SELECT COUNT(*) FROM users WHERE created_date >= @startDate",
  "connectionString": "Server=localhost;Database=testdb;Trusted_Connection=true;",
  "database": "testdb",
  "parameters": {
    "@startDate": "2024-01-01"
  },
  "timeout": 60
}
```

**使用场景：**
- 数据库备份
- 数据清理
- 报表生成

### 3. Shell任务处理器 (`shell`)

支持执行系统命令的任务类型。

**参数示例：**
```json
{
  "command": "tar -czf backup.tar.gz /var/log",
  "workingDirectory": "/tmp",
  "timeout": 300,
  "environmentVariables": {
    "PATH": "/usr/local/bin:/usr/bin:/bin"
  }
}
```

**使用场景：**
- 文件备份
- 系统维护
- 脚本执行

### 4. 自定义任务处理器 (`custom`)

示例自定义任务处理器，演示如何扩展任务类型。

**参数示例：**
```json
{
  "operation": "echo",
  "message": "Hello World",
  "delayMilliseconds": 1000,
  "number1": 10,
  "number2": 20
}
```

## 创建自定义任务处理器

### 1. 实现 ITaskHandler 接口

```csharp
public class MyCustomTaskHandler : ITaskHandler
{
    private readonly ILogger<MyCustomTaskHandler> _logger;

    public string TaskType => "my-custom";
    public string Description => "我的自定义任务处理器";

    public MyCustomTaskHandler(ILogger<MyCustomTaskHandler> logger)
    {
        _logger = logger;
    }

    public async Task<TaskExecutionResult> HandleAsync(ScheduledTask task, object? parameters = null)
    {
        var startTime = DateTime.UtcNow;
        var result = new TaskExecutionResult
        {
            TaskId = task.Id,
            StartTime = startTime,
            Status = ExecutionStatus.Running
        };

        try
        {
            // 解析参数
            var myParams = ParseParameters(parameters);
            
            // 执行任务逻辑
            var output = await ExecuteMyTask(myParams);
            
            result.Status = ExecutionStatus.Completed;
            result.EndTime = DateTime.UtcNow;
            result.Success = true;
            result.Result = output;
        }
        catch (Exception ex)
        {
            result.Status = ExecutionStatus.Failed;
            result.EndTime = DateTime.UtcNow;
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<bool> ValidateParametersAsync(object? parameters)
    {
        try
        {
            var myParams = ParseParameters(parameters);
            return !string.IsNullOrEmpty(myParams.RequiredField);
        }
        catch
        {
            return false;
        }
    }

    public TaskHandlerConfiguration GetConfiguration()
    {
        return new TaskHandlerConfiguration
        {
            TaskType = TaskType,
            Description = Description,
            SupportsRetry = true,
            SupportsTimeout = true,
            DefaultTimeoutSeconds = 60,
            DefaultParameters = new Dictionary<string, object>
            {
                ["requiredField"] = "",
                ["optionalField"] = "default"
            }
        };
    }

    private MyTaskParameters ParseParameters(object? parameters)
    {
        // 实现参数解析逻辑
        if (parameters is MyTaskParameters myParams)
            return myParams;

        if (parameters is string jsonString)
        {
            return JsonSerializer.Deserialize<MyTaskParameters>(jsonString) 
                ?? throw new ArgumentException("无效的参数");
        }

        throw new ArgumentException("无效的参数类型");
    }

    private async Task<string> ExecuteMyTask(MyTaskParameters parameters)
    {
        // 实现具体的任务执行逻辑
        await Task.Delay(100); // 模拟异步操作
        return $"任务执行完成: {parameters.RequiredField}";
    }
}

public class MyTaskParameters
{
    public string RequiredField { get; set; } = string.Empty;
    public string OptionalField { get; set; } = "default";
}
```

### 2. 注册自定义处理器

在 `Program.cs` 中注册您的自定义处理器：

```csharp
// 配置任务处理器
builder.Services.ConfigureTaskHandlers(factory =>
{
    // 注册自定义任务处理器
    factory.RegisterHandler<MyCustomTaskHandler>("my-custom");
});
```

### 3. 使用自定义任务

创建任务时指定您的自定义任务类型：

```csharp
var task = new ScheduledTask
{
    Name = "我的自定义任务",
    TaskType = "my-custom",
    TaskParameters = JsonSerializer.Serialize(new MyTaskParameters
    {
        RequiredField = "重要参数",
        OptionalField = "可选参数"
    }),
    CronExpression = "0 0 * * *" // 每天执行
};
```

## API 接口

### 获取支持的任务类型

```http
GET /api/taskhandlers/types
```

响应：
```json
["http", "sql", "shell", "custom", "my-custom"]
```

### 获取任务处理器配置

```http
GET /api/taskhandlers/config/{taskType}
```

响应：
```json
{
  "taskType": "my-custom",
  "description": "我的自定义任务处理器",
  "version": "1.0.0",
  "supportsRetry": true,
  "supportsTimeout": true,
  "defaultTimeoutSeconds": 60,
  "defaultParameters": {
    "requiredField": "",
    "optionalField": "default"
  }
}
```

### 验证任务参数

```http
POST /api/taskhandlers/validate/{taskType}
Content-Type: application/json

{
  "requiredField": "test",
  "optionalField": "value"
}
```

响应：
```json
true
```

## 最佳实践

### 1. 参数验证

始终在 `ValidateParametersAsync` 方法中验证参数的有效性：

```csharp
public async Task<bool> ValidateParametersAsync(object? parameters)
{
    try
    {
        var myParams = ParseParameters(parameters);
        
        // 验证必需参数
        if (string.IsNullOrEmpty(myParams.RequiredField))
            return false;
            
        // 验证参数范围
        if (myParams.Timeout < 0 || myParams.Timeout > 3600)
            return false;
            
        return true;
    }
    catch
    {
        return false;
    }
}
```

### 2. 错误处理

在任务执行过程中妥善处理异常：

```csharp
try
{
    // 执行任务逻辑
    var result = await ExecuteTask(parameters);
    
    return new TaskExecutionResult
    {
        TaskId = task.Id,
        Status = ExecutionStatus.Completed,
        Success = true,
        Result = result
    };
}
catch (TimeoutException ex)
{
    return new TaskExecutionResult
    {
        TaskId = task.Id,
        Status = ExecutionStatus.Failed,
        Success = false,
        ErrorMessage = "任务执行超时",
        StackTrace = ex.StackTrace
    };
}
catch (Exception ex)
{
    return new TaskExecutionResult
    {
        TaskId = task.Id,
        Status = ExecutionStatus.Failed,
        Success = false,
        ErrorMessage = ex.Message,
        StackTrace = ex.StackTrace
    };
}
```

### 3. 日志记录

使用结构化日志记录任务执行过程：

```csharp
_logger.LogInformation("开始执行任务: {TaskName}, 类型: {TaskType}", 
    task.Name, task.TaskType);

_logger.LogInformation("任务执行完成: {TaskName}, 成功: {Success}", 
    task.Name, result.Success);

_logger.LogError(ex, "任务执行失败: {TaskName}", task.Name);
```

### 4. 配置管理

提供合理的默认配置：

```csharp
public TaskHandlerConfiguration GetConfiguration()
{
    return new TaskHandlerConfiguration
    {
        TaskType = TaskType,
        Description = Description,
        SupportsRetry = true,
        SupportsTimeout = true,
        DefaultTimeoutSeconds = 300,
        DefaultParameters = new Dictionary<string, object>
        {
            ["timeout"] = 300,
            ["retryCount"] = 3,
            ["enableLogging"] = true
        }
    };
}
```

## 测试

为您的自定义任务处理器编写单元测试：

```csharp
[TestMethod]
public async Task TestMyCustomTaskHandler_ExecuteTask()
{
    var handler = new MyCustomTaskHandler(_logger);
    
    var task = new ScheduledTask
    {
        Id = Guid.NewGuid(),
        Name = "测试任务",
        TaskType = "my-custom"
    };
    
    var parameters = new MyTaskParameters
    {
        RequiredField = "test",
        OptionalField = "value"
    };
    
    var result = await handler.HandleAsync(task, parameters);
    
    Assert.IsTrue(result.Success);
    Assert.AreEqual(ExecutionStatus.Completed, result.Status);
    Assert.IsTrue(result.Result.Contains("test"));
}
```

## 总结

任务类型插件机制为 MonkeyScheduler 提供了强大的扩展能力。通过实现 `ITaskHandler` 接口，您可以轻松添加新的任务类型，满足各种业务需求。记住要遵循最佳实践，确保您的自定义处理器具有良好的错误处理、参数验证和日志记录功能。
