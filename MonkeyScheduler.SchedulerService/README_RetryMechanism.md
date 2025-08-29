# MonkeyScheduler 任务重试机制

## 概述

MonkeyScheduler 提供了强大的任务重试机制，确保任务在遇到临时性故障时能够自动重试，提高系统的可靠性和可用性。

## 功能特性

### 1. 多种重试策略
- **固定间隔重试 (Fixed)**: 每次重试间隔相同
- **指数退避重试 (Exponential)**: 重试间隔呈指数增长，避免对系统造成过大压力
- **线性增长重试 (Linear)**: 重试间隔呈线性增长

### 2. 灵活的重试配置
- 支持全局重试配置和任务级别重试配置
- 可配置最大重试次数、重试间隔、超时时间等
- 支持跳过失败的节点，自动选择健康节点重试

### 3. 智能重试管理
- 自动检测任务是否应该重试
- 支持手动重试和自动重试
- 重试状态持久化，支持服务重启后继续重试

## 配置说明

### 全局重试配置 (appsettings.json)

```json
{
  "RetryConfiguration": {
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
  }
}
```

### 配置参数说明

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| EnableRetry | bool | true | 是否启用全局重试机制 |
| DefaultMaxRetryCount | int | 3 | 默认最大重试次数 |
| DefaultRetryIntervalSeconds | int | 60 | 默认重试间隔（秒） |
| DefaultRetryStrategy | RetryStrategy | Exponential | 默认重试策略 |
| DefaultTimeoutSeconds | int | 300 | 默认任务超时时间（秒） |
| MaxRetryIntervalSeconds | int | 3600 | 最大重试间隔（秒） |
| RetryCooldownSeconds | int | 300 | 重试失败后的冷却时间（秒） |
| DisableTaskOnMaxRetries | bool | false | 是否在达到最大重试次数后禁用任务 |
| SkipFailedNodes | bool | true | 重试时是否跳过失败的节点 |
| EnableRetryLogging | bool | true | 是否启用重试日志记录 |

## 重试策略详解

### 1. 固定间隔重试 (Fixed)
```
第1次重试: 60秒后
第2次重试: 60秒后
第3次重试: 60秒后
```

### 2. 指数退避重试 (Exponential)
```
第1次重试: 60秒后 (60 * 2^0)
第2次重试: 120秒后 (60 * 2^1)
第3次重试: 240秒后 (60 * 2^2)
```

### 3. 线性增长重试 (Linear)
```
第1次重试: 60秒后 (60 * 1)
第2次重试: 120秒后 (60 * 2)
第3次重试: 180秒后 (60 * 3)
```

## API 使用指南

### 1. 创建带重试配置的任务

```http
POST /api/tasks
Content-Type: application/json

{
  "name": "数据备份任务",
  "cronExpression": "0 2 * * *",
  "description": "每日凌晨2点执行数据备份",
  "enableRetry": true,
  "maxRetryCount": 5,
  "retryIntervalSeconds": 120,
  "retryStrategy": "Exponential",
  "timeoutSeconds": 600
}
```

### 2. 获取任务重试信息

```http
GET /api/tasks/{taskId}/retry-info
```

响应示例：
```json
{
  "taskId": "12345678-1234-1234-1234-123456789012",
  "taskName": "数据备份任务",
  "enableRetry": true,
  "maxRetryCount": 5,
  "currentRetryCount": 2,
  "retryIntervalSeconds": 120,
  "retryStrategy": "Exponential",
  "nextRetryTime": "2024-01-15T10:30:00Z",
  "canRetry": true,
  "nextRetryTimeCalculated": "2024-01-15T10:30:00Z"
}
```

### 3. 手动重试任务

```http
POST /api/tasks/{taskId}/retry
```

### 4. 重置任务重试状态

```http
POST /api/tasks/{taskId}/reset-retry
```

### 5. 更新任务重试配置

```http
PUT /api/tasks/{taskId}/retry-config
Content-Type: application/json

{
  "enableRetry": true,
  "maxRetryCount": 3,
  "retryIntervalSeconds": 60,
  "retryStrategy": "Fixed",
  "timeoutSeconds": 300
}
```

### 6. 获取重试配置

```http
GET /api/retryconfiguration
```

### 7. 测试重试间隔计算

```http
GET /api/retryconfiguration/test-intervals?baseInterval=60&strategy=Exponential&maxRetries=3
```

响应示例：
```json
{
  "strategy": "Exponential",
  "baseIntervalSeconds": 60,
  "maxRetries": 3,
  "intervals": [
    {
      "retryAttempt": 1,
      "delaySeconds": 60,
      "delayMinutes": 1.0,
      "nextRetryTime": "2024-01-15T10:31:00Z"
    },
    {
      "retryAttempt": 2,
      "delaySeconds": 120,
      "delayMinutes": 2.0,
      "nextRetryTime": "2024-01-15T10:33:00Z"
    },
    {
      "retryAttempt": 3,
      "delaySeconds": 240,
      "delayMinutes": 4.0,
      "nextRetryTime": "2024-01-15T10:37:00Z"
    }
  ]
}
```

## 重试机制工作流程

### 1. 任务执行失败
当任务执行失败时，系统会：
1. 记录失败信息
2. 检查是否启用重试机制
3. 检查是否达到最大重试次数
4. 计算下次重试时间

### 2. 重试执行
在下次重试时间到达时：
1. 选择健康的节点
2. 跳过之前失败的节点（如果配置了）
3. 重新执行任务
4. 更新重试计数和状态

### 3. 重试成功
当重试成功时：
1. 重置重试状态
2. 记录成功信息
3. 继续正常调度

### 4. 重试失败
当重试失败时：
1. 增加重试计数
2. 计算下次重试时间
3. 如果达到最大重试次数，可选择禁用任务

## 最佳实践

### 1. 重试策略选择
- **临时性故障**: 使用指数退避策略，避免对系统造成压力
- **网络问题**: 使用固定间隔策略，保持稳定的重试频率
- **资源竞争**: 使用线性增长策略，逐步增加重试间隔

### 2. 重试次数配置
- **关键任务**: 设置较高的重试次数（5-10次）
- **非关键任务**: 设置较低的重试次数（1-3次）
- **实时任务**: 设置较短的重试间隔

### 3. 超时时间配置
- 根据任务的实际执行时间设置合理的超时时间
- 考虑网络延迟和系统负载
- 避免设置过长的超时时间，影响重试效率

### 4. 节点管理
- 启用跳过失败节点功能，提高重试成功率
- 定期检查节点健康状态
- 配置合适的节点超时时间

## 监控和日志

### 1. 重试日志
系统会记录详细的重试日志，包括：
- 重试开始和结束时间
- 重试次数和间隔
- 失败原因和节点信息
- 重试成功或最终失败

### 2. 监控指标
可以通过以下方式监控重试情况：
- 任务重试次数统计
- 重试成功率
- 平均重试间隔
- 失败节点分布

### 3. 告警配置
建议配置以下告警：
- 任务重试次数超过阈值
- 重试成功率低于阈值
- 节点频繁失败

## 故障排除

### 1. 常见问题

**Q: 任务一直重试但从未成功**
A: 检查任务逻辑是否正确，网络连接是否正常，节点是否健康

**Q: 重试间隔不符合预期**
A: 检查重试策略配置，确认重试间隔计算逻辑

**Q: 重试状态在服务重启后丢失**
A: 确保任务状态已持久化到数据库

### 2. 调试方法
- 查看重试日志
- 使用重试信息API检查状态
- 测试重试间隔计算
- 检查节点健康状态

## 总结

MonkeyScheduler 的重试机制提供了灵活、可靠的故障恢复能力，通过合理的配置和监控，可以显著提高系统的可用性和稳定性。建议根据实际业务需求选择合适的重试策略和配置参数。 