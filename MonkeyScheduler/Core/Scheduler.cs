using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.Core
{
    /// <summary>
    /// 任务调度器
    /// 负责管理和执行计划任务
    /// </summary>
    public class Scheduler
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskDispatcher _dispatcher;
        private readonly ILogger<Scheduler> _logger;
        private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="repo">任务仓储</param>
        /// <param name="dispatcher">任务分发器</param>
        /// <param name="logger">日志记录器</param>
        public Scheduler(ITaskRepository repo, ITaskDispatcher dispatcher, ILogger<Scheduler> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 启动调度器
        /// </summary>
        public void Start()
        {
            _logger.LogInformation("调度器开始启动");
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        var tasks = _repo.GetAllTasks()
                            .Where(t => t.Enabled && t.NextRunTime <= now)
                            .ToList();

                        _logger.LogDebug("检查到 {TaskCount} 个待执行任务", tasks.Count);

                        foreach (var task in tasks)
                        {
                            try
                            {
                                _logger.LogInformation("开始执行任务: {TaskName} (ID: {TaskId})", task.Name, task.Id);
                                await _dispatcher.DispatchTaskAsync(task);
                            
                                // 然后更新下次执行时间
                                task.NextRunTime = CronParser.GetNextOccurrence(task.CronExpression, now);
                                _repo.UpdateTask(task);
                                _logger.LogInformation("任务执行完成: {TaskName} (ID: {TaskId})", task.Name, task.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "执行任务 {TaskName} (ID: {TaskId}) 时发生错误", task.Name, task.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "调度器发生错误");
                    }

                    await Task.Delay(1000, _cts.Token);
                }
            }, _cts.Token);
            _logger.LogInformation("调度器启动完成");
        }

        /// <summary>
        /// 停止调度器
        /// </summary>
        public void Stop()
        {
            _logger.LogInformation("调度器开始停止");
            _cts.Cancel();
            _logger.LogInformation("调度器已停止");
        }
    }
} 