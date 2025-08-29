# 负载均衡策略系统

## 概述

MonkeyScheduler 提供了可插拔的负载均衡策略系统，支持多种负载均衡算法，并允许用户自定义策略。系统默认提供基于请求计数的轮询策略，同时支持自定义实现。

## 内置策略

### 1. 最少连接数策略 (LeastConnection)

**策略名称**: `LeastConnection`

**描述**: 选择当前连接数最少的节点来执行任务，确保负载均匀分布。

**适用场景**: 
- 节点性能相近
- 任务执行时间差异较大
- 需要最大化资源利用率

**配置参数**:
```json
{
  "MaxConnectionsPerNode": 100,
  "EnableStickySessions": false,
  "StickySessionTimeout": 300
}
```

### 2. 轮询策略 (RoundRobin)

**策略名称**: `RoundRobin`

**描述**: 按顺序轮流选择节点来执行任务，确保任务均匀分布。

**适用场景**:
- 节点性能相近
- 任务执行时间相对稳定
- 需要简单的负载分配

**配置参数**:
```json
{
  "EnableWeightedRoundRobin": false,
  "NodeWeights": {
    "node1": 3,
    "node2": 2,
    "node3": 1
  },
  "MaxConnectionsPerNode": 100
}
```

### 3. 随机策略 (Random)

**策略名称**: `Random`

**描述**: 随机选择节点来执行任务，适合节点性能相近且任务执行时间差异不大的场景。

**适用场景**:
- 节点性能相近
- 任务执行时间差异不大
- 需要避免热点问题

**配置参数**:
```json
{
  "MaxConnectionsPerNode": 100,
  "EnableWeightedRandom": false,
  "NodeWeights": {
    "node1": 3,
    "node2": 2,
    "node3": 1
  },
  "Seed": 12345
}
```

## API 使用

### 获取可用策略列表

```http
GET /api/loadbalancing/strategies
```

**响应示例**:
```json
{
  "availableStrategies": ["LeastConnection", "RoundRobin", "Random"],
  "strategyDetails": [
    {
      "name": "LeastConnection",
      "description": "最少连接数策略：选择当前连接数最少的节点来执行任务，确保负载均匀分布",
      "configuration": {
        "MaxConnectionsPerNode": 100,
        "EnableStickySessions": false,
        "StickySessionTimeout": 300
      }
    }
  ]
}
```

### 获取策略详细信息

```http
GET /api/loadbalancing/strategies/{strategyName}
```

### 获取负载均衡器状态

```http
GET /api/loadbalancing/status
```

**响应示例**:
```json
{
  "currentStrategy": {
    "name": "LeastConnection",
    "description": "最少连接数策略：选择当前连接数最少的节点来执行任务，确保负载均匀分布",
    "configuration": {
      "MaxConnectionsPerNode": 100,
      "EnableStickySessions": false,
      "StickySessionTimeout": 300
    }
  },
  "nodeLoads": {
    "http://node1:5001": 5,
    "http://node2:5001": 3,
    "http://node3:5001": 7
  },
  "totalNodes": 3,
  "totalLoad": 15
}
```

### 更新策略配置

```http
PUT /api/loadbalancing/configuration
Content-Type: application/json

{
  "MaxConnectionsPerNode": 150,
  "EnableStickySessions": true,
  "StickySessionTimeout": 600
}
```

### 获取节点负载统计

```http
GET /api/loadbalancing/node-loads
```

**响应示例**:
```json
{
  "nodeLoads": [
    {
      "nodeUrl": "http://node1:5001",
      "currentLoad": 5,
      "loadPercentage": 33.33
    },
    {
      "nodeUrl": "http://node2:5001",
      "currentLoad": 3,
      "loadPercentage": 20.0
    },
    {
      "nodeUrl": "http://node3:5001",
      "currentLoad": 7,
      "loadPercentage": 46.67
    }
  ],
  "statistics": {
    "totalNodes": 3,
    "totalLoad": 15,
    "averageLoad": 5.0,
    "maxLoad": 7,
    "minLoad": 3
  }
}
```

## 自定义策略实现

### 1. 实现策略接口

创建自定义策略类，实现 `ILoadBalancingStrategy` 接口：

```csharp
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;

namespace YourNamespace
{
    public class CustomLoadBalancingStrategy : ILoadBalancingStrategy
    {
        private readonly Dictionary<string, object> _configuration;

        public CustomLoadBalancingStrategy()
        {
            _configuration = new Dictionary<string, object>
            {
                ["CustomParameter"] = "defaultValue"
            };
        }

        public string StrategyName => "Custom";

        public string StrategyDescription => "自定义策略描述";

        public string SelectNode(IEnumerable<string> availableNodes, ScheduledTask task, IDictionary<string, int> nodeLoads)
        {
            if (availableNodes == null || !availableNodes.Any())
                throw new InvalidOperationException("没有可用的节点");

            // 实现你的负载均衡逻辑
            var maxConnections = (int)_configuration["MaxConnectionsPerNode"];
            
            // 过滤掉已达到最大连接数的节点
            var eligibleNodes = availableNodes
                .Where(node => !nodeLoads.ContainsKey(node) || nodeLoads[node] < maxConnections)
                .ToList();

            if (!eligibleNodes.Any())
                throw new InvalidOperationException("所有节点都已达到最大连接数限制");

            // 你的选择逻辑
            var selectedNode = eligibleNodes.First(); // 示例：选择第一个节点
            
            return selectedNode;
        }

        public IDictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>(_configuration);
        }

        public void UpdateConfiguration(IDictionary<string, object> configuration)
        {
            foreach (var kvp in configuration)
            {
                _configuration[kvp.Key] = kvp.Value;
            }
        }
    }
}
```

### 2. 注册自定义策略

#### 方法一：通过 API 注册

```http
POST /api/loadbalancing/register-strategy
Content-Type: application/json

{
  "strategyName": "MyCustomStrategy",
  "strategyTypeName": "YourNamespace.CustomLoadBalancingStrategy, YourAssembly",
  "description": "我的自定义负载均衡策略"
}
```

#### 方法二：在代码中注册

```csharp
// 在 Startup.cs 或 Program.cs 中
var strategyFactory = services.GetRequiredService<LoadBalancingStrategyFactory>();
strategyFactory.RegisterStrategy("MyCustomStrategy", typeof(CustomLoadBalancingStrategy));
```

### 3. 使用自定义策略

```csharp
// 创建策略实例
var strategy = strategyFactory.CreateStrategy("MyCustomStrategy");

// 配置策略
strategy.UpdateConfiguration(new Dictionary<string, object>
{
    ["CustomParameter"] = "customValue"
});
```

## 高级功能

### 1. 加权负载均衡

支持为不同节点设置权重，实现加权负载均衡：

```csharp
// 配置加权轮询
var config = new Dictionary<string, object>
{
    ["EnableWeightedRoundRobin"] = true,
    ["NodeWeights"] = new Dictionary<string, int>
    {
        ["node1"] = 3,  // 高性能节点，权重更高
        ["node2"] = 2,  // 中等性能节点
        ["node3"] = 1   // 低性能节点，权重更低
    }
};
strategy.UpdateConfiguration(config);
```

### 2. 任务类型路由

根据任务类型选择最适合的节点：

```csharp
public string SelectNodeByTaskType(List<string> eligibleNodes, ScheduledTask task, IDictionary<string, int> nodeLoads)
{
    switch (task.TaskType.ToLower())
    {
        case "cpu-intensive":
            return SelectBestPerformanceNode(eligibleNodes, "cpu");
        case "io-intensive":
            return SelectBestPerformanceNode(eligibleNodes, "io");
        case "memory-intensive":
            return SelectBestPerformanceNode(eligibleNodes, "memory");
        default:
            return SelectNodeByPerformance(eligibleNodes, nodeLoads);
    }
}
```

### 3. 性能权重选择

结合节点性能和当前负载进行智能选择：

```csharp
public string SelectNodeByPerformance(List<string> eligibleNodes, IDictionary<string, int> nodeLoads)
{
    var performanceWeight = 0.7;
    var loadWeight = 0.3;

    var nodeScores = eligibleNodes.Select(node =>
    {
        var performance = GetNodePerformance(node);
        var load = nodeLoads.GetValueOrDefault(node, 0);
        var maxLoad = 100;

        var performanceScore = performance.GetOverallScore();
        var loadScore = 1.0 - (double)load / maxLoad;
        var totalScore = performanceScore * performanceWeight + loadScore * loadWeight;

        return new { Node = node, Score = totalScore };
    }).ToList();

    return nodeScores.OrderByDescending(x => x.Score).First().Node;
}
```

## 配置示例

### appsettings.json 配置

```json
{
  "LoadBalancing": {
    "DefaultStrategy": "LeastConnection",
    "MaxConnectionsPerNode": 100,
    "EnableStickySessions": false,
    "StickySessionTimeout": 300,
    "NodeWeights": {
      "http://node1:5001": 3,
      "http://node2:5001": 2,
      "http://node3:5001": 1
    }
  }
}
```

### 服务注册

```csharp
// 在 Startup.cs 中
services.AddSchedulerService(configuration);

// 或者手动注册
services.AddSingleton<LoadBalancingStrategyFactory>();
services.AddSingleton<ILoadBalancingStrategy, LeastConnectionStrategy>();
services.AddSingleton<ILoadBalancer>(sp =>
{
    var nodeRegistry = sp.GetRequiredService<INodeRegistry>();
    var strategy = sp.GetRequiredService<ILoadBalancingStrategy>();
    return new LoadBalancer(nodeRegistry, strategy);
});
```

## 最佳实践

### 1. 策略选择建议

- **最少连接数策略**: 适合节点性能相近，任务执行时间差异较大的场景
- **轮询策略**: 适合节点性能相近，任务执行时间相对稳定的场景
- **随机策略**: 适合节点性能相近，需要避免热点问题的场景
- **自定义策略**: 适合有特殊业务需求的场景

### 2. 性能优化

- 合理设置 `MaxConnectionsPerNode` 参数
- 定期监控节点负载情况
- 根据实际业务需求调整策略配置
- 使用加权负载均衡来充分利用高性能节点

### 3. 故障处理

- 实现节点健康检查
- 自动移除故障节点
- 支持节点动态添加和移除
- 提供降级策略

### 4. 监控和日志

- 记录节点选择过程
- 监控负载分布情况
- 统计策略效果
- 提供性能指标

## 故障排除

### 常见问题

1. **策略注册失败**
   - 检查策略类型是否正确实现了 `ILoadBalancingStrategy` 接口
   - 确认程序集名称和命名空间正确

2. **节点选择异常**
   - 检查可用节点列表是否为空
   - 确认节点负载信息是否正确
   - 验证策略配置参数

3. **负载不均衡**
   - 检查策略配置是否正确
   - 确认节点性能差异
   - 调整权重配置

### 调试技巧

1. 启用详细日志记录
2. 使用 API 接口查看当前状态
3. 监控节点负载变化
4. 分析策略选择结果

## 总结

MonkeyScheduler 的负载均衡策略系统提供了灵活、可扩展的负载均衡解决方案。通过内置策略和自定义策略的支持，可以满足各种复杂的业务需求。系统设计遵循开闭原则，易于扩展和维护。 