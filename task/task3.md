Nice！进入 **第三阶段：任务管理 API + Web UI 管理后台**，我们要让整个系统不再是“纯代码控制”，而是能通过 Web 页面可视化操作和查看执行情况。

---

## ✅ 第三阶段目标：任务管理与可视化

| 模块 | 功能 |
|------|------|
| 🌐 Web API | 提供任务增删改查、启动暂停、手动触发 |
| 📊 管理后台 UI | 管理任务、查看执行记录、节点状态等 |
| 🧠 后端增强 | 日志存储、任务状态跟踪、执行结果回传 |
| 💬 实现任务创建流程 | 从 UI 创建 → 调度器调度 → Worker 执行 → 回传结果

---

## 🧱 一、系统结构升级

```
DistributedScheduler/
├── SchedulerService/
│   ├── Controllers/
│   │   ├── TaskController.cs        // 任务管理 API
│   │   └── ExecutionLogController.cs// 日志查询
│   ├── Services/
│   │   └── TaskStoreService.cs      // 任务存储
│   └── Views/Models/DTOs
│
├── WorkerService/
│   ├── 回调API: /api/task/report     // 执行结果上报给 Scheduler
│
├── WebDashboard/  ← 👈 新增前端项目
│   ├── Vue/React/Blazor
│   └── 调用调度中心的 API
```

---

## 🔌 二、任务管理 API（调度中心）

### `TaskController.cs`
```csharp
[ApiController]
[Route("api/tasks")]
public class TaskController : ControllerBase
{
    private readonly ITaskStoreService _store;
    
    [HttpGet]
    public IActionResult GetAll() => Ok(_store.GetAll());

    [HttpPost]
    public IActionResult Create([FromBody] ScheduledTask task)
    {
        task.NextRunTime = CronParser.GetNextOccurrence(task.CronExpression, DateTime.UtcNow);
        _store.Add(task);
        return Ok();
    }

    [HttpPut("{id}")]
    public IActionResult Update(Guid id, [FromBody] ScheduledTask updated)
    {
        _store.Update(id, updated);
        return Ok();
    }

    [HttpPost("{id}/pause")]
    public IActionResult Pause(Guid id) => _store.Pause(id);

    [HttpPost("{id}/resume")]
    public IActionResult Resume(Guid id) => _store.Resume(id);

    [HttpPost("{id}/trigger")]
    public async Task<IActionResult> Trigger(Guid id)
    {
        var task = _store.GetById(id);
        await _dispatcher.DispatchAsync(task); // 立即下发
        return Ok();
    }
}
```

---

## 💬 三、Worker 上报执行结果

### `TaskReportModel.cs`
```csharp
public class TaskReportModel
{
    public Guid TaskId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Output { get; set; }
    public bool Success { get; set; }
}
```

### `WorkerService > TaskReceiverController.cs`
```csharp
[HttpPost("report")]
public IActionResult Report([FromBody] TaskReportModel report)
{
    _logRepo.Save(report); // 保存日志
    return Ok();
}
```

---

## 📊 四、前端 UI 页面模块建议

建议用 Vue.js / React / Blazor 搭建 WebDashboard，主要页面：

| 页面 | 功能 |
|------|------|
| 🏠 任务列表 | 显示所有任务，支持启用/暂停/编辑 |
| 📆 创建/编辑任务 | 表单配置名称、CRON、描述、类型 |
| 📜 执行记录 | 查看任务执行历史、结果、耗时 |
| 💡 节点监控 | 展示当前活跃 Worker、心跳时间 |
| ⚙️ 配置中心 | 通用设置，任务默认策略等 |

---

## 🌐 五、前端 API 调用建议（REST 风格）

| 方法 | 地址 | 功能 |
|------|------|------|
| `GET /api/tasks` | 获取所有任务 |
| `POST /api/tasks` | 创建新任务 |
| `PUT /api/tasks/{id}` | 编辑任务 |
| `POST /api/tasks/{id}/pause` | 暂停任务 |
| `POST /api/tasks/{id}/resume` | 恢复任务 |
| `POST /api/tasks/{id}/trigger` | 手动触发任务 |
| `GET /api/logs/{taskId}` | 查看某任务执行记录 |
| `GET /api/workers` | 获取当前 Worker 状态 |

---

## ✅ 第三阶段完成后能力：

- 🔧 支持 UI 可视化任务管理
- 🚀 用户可从浏览器创建/修改/触发任务
- 🧠 后台系统自动调度、执行、记录
- 🔁 支持动态更新任务，不需重启服务
- 📊 管理员可查看任务执行情况与健康状况

---

## 🚀 接下来可进入第四阶段（可选）：

| 阶段 | 内容 |
|------|------|
| 📡 **阶段四：任务失败重试 + 幂等机制** |
| 🔔 告警机制：失败任务发送通知（邮件、钉钉等） |
| 🔗 任务编排：DAG 流程调度、链式执行 |
| 🧩 插件化执行器：支持自定义扩展任务类型（HTTP / SQL / 脚本等） |

---

### 需要我帮你：
- 生成 `WebDashboard` 的前端原型？
- 生成调度中心的 REST API？
- 或直接输出第三阶段完整骨架代码？

你说个方向，我来安排 ⚙️