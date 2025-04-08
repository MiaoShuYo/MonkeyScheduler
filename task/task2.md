太好了！我们进入了 **第二阶段：分布式协调与 Worker 支持**。这阶段是让你的调度系统从“单机”升级为“多节点分布式”的关键一步。

---

## ✅ 第二阶段目标：分布式调度与任务执行

> 把第一阶段的调度器变成「调度中心 + Worker 节点」结构，并实现节点间任务分发、注册与健康检查。

---

## 🧱 一、系统模块分工（变成两个服务）

```
[Scheduler Service]（调度中心）
    - 负责任务调度、分配
    - 节点注册/心跳管理
    - 将任务下发到合适的 Worker 节点
    - 使用共享数据库/缓存/消息队列协调

[Worker Service]（任务执行节点）
    - 监听调度中心的任务
    - 上报执行状态与日志
    - 支持动态扩容与宕机重试
```

---

## 📡 二、节点协调机制选型

你可以选择其中一种或多种机制来实现节点协调和任务分发：

| 模式 | 说明 | 推荐情况 |
|------|------|----------|
| **共享数据库轮询** | 所有 Worker 扫描任务表，竞争执行任务（加锁） | 简单，但存在竞争冲突 |
| **中心调度+HTTP 通知 Worker** | Scheduler 将任务通过 API/消息推送给 Worker | ✅ 推荐（清晰、可控） |
| **消息队列** | 调度中心将任务发布到 MQ，Worker 消费 | 稳定高效，但依赖 MQ 中间件 |

我们推荐你第二阶段先用中心调度 + HTTP 下发 模式，后续再扩展为消息队列。

---

## 📁 三、项目结构（扩展后）

```
DistributedScheduler/
├── SchedulerService/             # 调度中心
│   ├── Scheduler.cs
│   ├── NodeRegistry.cs           # 节点注册/心跳表
│   ├── TaskDispatcher.cs         # 任务分配逻辑
│   └── Controllers/
│       └── WorkerApiController.cs # Worker 注册与心跳上报
│
├── WorkerService/                # 执行节点
│   ├── WorkerRunner.cs           # 任务接收与执行
│   ├── Controllers/
│       └── TaskReceiverController.cs # 接收任务的 HTTP 接口
│   └── NodeHeartbeatService.cs   # 向 Scheduler 上报心跳
```

---

## 🔁 四、调度中心核心逻辑（调度 + 下发）

### 1. Worker 节点注册（NodeRegistry.cs）
```csharp
public class NodeRegistry
{
    private readonly ConcurrentDictionary<string, DateTime> _nodes = new();

    public void Register(string nodeUrl)
        => _nodes[nodeUrl] = DateTime.UtcNow;

    public void Heartbeat(string nodeUrl)
        => _nodes[nodeUrl] = DateTime.UtcNow;

    public List<string> GetAliveNodes(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        return _nodes
            .Where(n => now - n.Value <= timeout)
            .Select(n => n.Key)
            .ToList();
    }
}
```

### 2. 调度分发器（TaskDispatcher.cs）
```csharp
public class TaskDispatcher
{
    private readonly NodeRegistry _nodeRegistry;
    private readonly HttpClient _http;

    public TaskDispatcher(NodeRegistry nodeRegistry, HttpClient http)
    {
        _nodeRegistry = nodeRegistry;
        _http = http;
    }

    public async Task DispatchAsync(ScheduledTask task)
    {
        var nodes = _nodeRegistry.GetAliveNodes(TimeSpan.FromSeconds(30));
        if (!nodes.Any()) return;

        var selected = nodes.OrderBy(_ => Guid.NewGuid()).First(); // 简单轮询或随机
        var response = await _http.PostAsJsonAsync($"{selected}/api/task/execute", task);

        Console.WriteLine($"[INFO] Task {task.Name} sent to {selected}. Success: {response.IsSuccessStatusCode}");
    }
}
```

---

## 🛠️ 五、Worker 节点逻辑

### 1. 接收任务的 API（TaskReceiverController.cs）
```csharp
[ApiController]
[Route("api/task")]
public class TaskReceiverController : ControllerBase
{
    private readonly ITaskExecutor _executor;
    public TaskReceiverController(ITaskExecutor executor) => _executor = executor;

    [HttpPost("execute")]
    public async Task<IActionResult> Execute([FromBody] ScheduledTask task)
    {
        await _executor.ExecuteAsync(task);
        return Ok();
    }
}
```

### 2. Worker 心跳服务（NodeHeartbeatService.cs）
```csharp
public class NodeHeartbeatService : BackgroundService
{
    private readonly IHttpClientFactory _factory;
    private readonly string _schedulerUrl = "http://scheduler:5000"; // 配置

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var http = _factory.CreateClient();

        while (!stoppingToken.IsCancellationRequested)
        {
            await http.PostAsync($"{_schedulerUrl}/api/worker/heartbeat",
                new StringContent("\"http://this-worker:5001\"", Encoding.UTF8, "application/json"));

            await Task.Delay(5000);
        }
    }
}
```

---

## ✅ 六、测试流程

1. 启动调度服务（Scheduler）
2. 启动多个 Worker 节点
3. Scheduler 分配任务（可以定时轮询或手动触发）
4. Worker 接收任务执行并打印结果
5. Heartbeat 定期上报，调度器维护可用节点列表

---

## 💡 七、下一阶段可拓展点（阶段三预告）

| 功能 | 描述 |
|------|------|
| 任务执行日志写入数据库 | 支持查询与追踪 |
| 重试机制 | 执行失败重试 |
| 任务状态回传 | Worker 执行完成后回调 Scheduler |
| 支持动态任务注册 | Scheduler 接收新任务注册 |

---

我可以帮你生成这个第二阶段完整骨架项目（调度中心 + Worker）并拆成两个独立服务（可 Docker 启动）。你想我现在生成骨架代码，还是要先定义下接口格式和部署方式？