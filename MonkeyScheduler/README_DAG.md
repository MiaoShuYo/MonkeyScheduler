# DAG任务编排功能文档

## 概述

DAG（有向无环图）任务编排功能允许您创建具有依赖关系的任务工作流，实现复杂的业务流程自动化。任务按照依赖关系顺序执行，支持并行执行和错误处理。

## 功能特性

- ✅ **依赖关系管理**：支持任务间的依赖关系定义
- ✅ **循环依赖检测**：自动检测并防止循环依赖
- ✅ **工作流验证**：验证工作流的完整性和正确性
- ✅ **并行执行**：支持同一层级的任务并行执行
- ✅ **状态跟踪**：实时跟踪工作流和任务执行状态
- ✅ **错误处理**：支持任务失败后的处理策略
- ✅ **工作流控制**：支持暂停、恢复、取消工作流

## 核心概念

### 1. DAG任务（ScheduledTask）

DAG任务在原有任务基础上增加了以下字段：

```csharp
public class ScheduledTask
{
    // ... 原有字段 ...
    
    /// <summary>
    /// 依赖的任务ID列表（前置任务）
    /// </summary>
    public List<Guid>? Dependencies { get; set; }
    
    /// <summary>
    /// 后续任务ID列表（后置任务）
    /// </summary>
    public List<Guid>? NextTaskIds { get; set; }
    
    /// <summary>
    /// DAG执行状态
    /// </summary>
    public DagExecutionStatus DagStatus { get; set; }
    
    /// <summary>
    /// 是否属于DAG工作流
    /// </summary>
    public bool IsDagTask => Dependencies?.Any() == true || NextTaskIds?.Any() == true;
    
    /// <summary>
    /// DAG工作流ID（用于分组管理）
    /// </summary>
    public Guid? DagWorkflowId { get; set; }
}
```

### 2. DAG执行状态

```csharp
public enum DagExecutionStatus
{
    Waiting,    // 等待依赖任务完成
    Ready,      // 依赖已满足，可以执行
    Running,    // 正在执行
    Completed,  // 执行完成
    Failed,     // 执行失败
    Skipped     // 跳过执行（依赖任务失败）
}
```

### 3. 工作流状态

```csharp
public enum WorkflowStatus
{
    NotStarted,      // 未开始
    Running,         // 正在执行
    Completed,       // 已完成
    PartiallyFailed, // 部分失败
    Failed,          // 完全失败
    Paused,          // 已暂停
    Cancelled        // 已取消
}
```

## 使用示例

### 1. 创建简单的线性工作流

```csharp
// 创建任务
var task1 = new ScheduledTask
{
    Id = Guid.NewGuid(),
    Name = "数据准备",
    DagWorkflowId = workflowId,
    TaskType = "HttpRequest",
    TaskParameters = "{\"url\":\"http://api.example.com/prepare\"}"
};

var task2 = new ScheduledTask
{
    Id = Guid.NewGuid(),
    Name = "数据处理",
    DagWorkflowId = workflowId,
    Dependencies = new List<Guid> { task1.Id },
    TaskType = "SqlScript",
    TaskParameters = "{\"script\":\"SELECT * FROM data\"}"
};

var task3 = new ScheduledTask
{
    Id = Guid.NewGuid(),
    Name = "结果输出",
    DagWorkflowId = workflowId,
    Dependencies = new List<Guid> { task2.Id },
    TaskType = "ShellCommand",
    TaskParameters = "{\"command\":\"echo 'Done'\"}"
};
```

### 2. 创建并行工作流

```csharp
var task1 = new ScheduledTask
{
    Id = Guid.NewGuid(),
    Name = "数据准备",
    DagWorkflowId = workflowId
};

// 并行任务
var task2a = new ScheduledTask
{
    Id = Guid.NewGuid(),
    Name = "处理A",
    DagWorkflowId = workflowId,
    Dependencies = new List<Guid> { task1.Id }
};

var task2b = new ScheduledTask
{
    Id = Guid.NewGuid(),
    Name = "处理B",
    DagWorkflowId = workflowId,
    Dependencies = new List<Guid> { task1.Id }
};

// 合并任务
var task3 = new ScheduledTask
{
    Id = Guid.NewGuid(),
    Name = "结果合并",
    DagWorkflowId = workflowId,
    Dependencies = new List<Guid> { task2a.Id, task2b.Id }
};
```

## API接口

### 1. 工作流验证

```http
POST /api/dag-workflow/validate?workflowId={workflowId}
Content-Type: application/json

[
  {
    "id": "task1-id",
    "name": "任务1",
    "dagWorkflowId": "workflow-id",
    "dependencies": []
  },
  {
    "id": "task2-id", 
    "name": "任务2",
    "dagWorkflowId": "workflow-id",
    "dependencies": ["task1-id"]
  }
]
```

### 2. 循环依赖检测

```http
POST /api/dag-workflow/detect-cycles
Content-Type: application/json

[
  {
    "id": "task1-id",
    "name": "任务1",
    "dependencies": ["task2-id"]
  },
  {
    "id": "task2-id",
    "name": "任务2", 
    "dependencies": ["task1-id"]
  }
]
```

### 3. 启动工作流

```http
POST /api/dag-workflow/start?workflowId={workflowId}
Content-Type: application/json

[
  {
    "id": "task1-id",
    "name": "任务1",
    "dagWorkflowId": "workflow-id"
  },
  {
    "id": "task2-id",
    "name": "任务2", 
    "dagWorkflowId": "workflow-id",
    "dependencies": ["task1-id"]
  }
]
```

### 4. 获取工作流状态

```http
GET /api/dag-workflow/status/{workflowId}
```

响应示例：
```json
{
  "workflowId": "workflow-id",
  "status": "Running",
  "totalTasks": 3,
  "completedTasks": 1,
  "failedTasks": 0,
  "runningTasks": 1,
  "waitingTasks": 1,
  "skippedTasks": 0,
  "startTime": "2024-01-01T10:00:00Z",
  "endTime": null,
  "progressPercentage": 33.33
}
```

### 5. 工作流控制

```http
POST /api/dag-workflow/pause/{workflowId}
POST /api/dag-workflow/resume/{workflowId}
POST /api/dag-workflow/cancel/{workflowId}
```

## 服务注册

在 `Program.cs` 中注册DAG相关服务：

```csharp
// 注册DAG相关服务
services.AddSingleton<IDagDependencyChecker, DagDependencyChecker>();
services.AddSingleton<IDagExecutionManager, DagExecutionManager>();
```

## 错误处理

### 1. 循环依赖错误

```json
{
  "hasCycle": true,
  "cycles": [
    ["task1-id", "task2-id", "task1-id"]
  ],
  "errorMessage": "检测到 1 个循环依赖"
}
```

### 2. 依赖检查错误

```json
{
  "isValid": false,
  "missingDependencies": ["missing-task-id"],
  "invalidDependencies": ["disabled-task-id"],
  "errorMessage": "任务 'Task3' 的依赖检查失败：缺失依赖: missing-task-id，无效依赖: disabled-task-id"
}
```

### 3. 工作流验证错误

```json
{
  "isValid": false,
  "errorMessage": "工作流 workflow-id 没有入口任务"
}
```

## 最佳实践

### 1. 工作流设计

- **单一职责**：每个任务应该只负责一个特定的功能
- **合理依赖**：避免过度复杂的依赖关系
- **错误处理**：为关键任务设置重试机制
- **监控告警**：为重要工作流设置监控和告警

### 2. 性能优化

- **并行执行**：合理利用并行执行提高效率
- **资源控制**：避免同时执行过多任务
- **超时设置**：为长时间运行的任务设置超时

### 3. 维护性

- **命名规范**：使用清晰的任务和工作流命名
- **文档记录**：为复杂工作流编写文档
- **版本控制**：对工作流配置进行版本控制

## 常见问题

### Q1: 如何检测循环依赖？

A: 使用 `POST /api/dag-workflow/detect-cycles` 接口，传入任务列表进行检测。

### Q2: 任务失败后如何处理？

A: 系统会自动跳过依赖该失败任务的后续任务，您可以通过API查看工作流状态。

### Q3: 如何实现条件分支？

A: 目前支持基于任务执行结果的分支，失败的任务会阻止后续依赖任务执行。

### Q4: 如何监控工作流执行？

A: 使用 `GET /api/dag-workflow/status/{workflowId}` 接口实时获取工作流状态。

### Q5: 支持的最大任务数量？

A: 理论上没有限制，但建议单个工作流不超过100个任务以保证性能。

## 更新日志

### v1.0.0 (2024-01-01)
- ✅ 基础DAG功能实现
- ✅ 依赖检查和循环检测
- ✅ 工作流执行管理
- ✅ API接口提供
- ✅ 测试用例覆盖
