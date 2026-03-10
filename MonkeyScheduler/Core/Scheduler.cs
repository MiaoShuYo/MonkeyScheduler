using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Models;

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
        private readonly IDagDependencyChecker _dagDependencyChecker;
        private readonly IDagExecutionManager _dagExecutionManager;
        private readonly ILogger<Scheduler> _logger;
        private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="repo">任务仓储</param>
        /// <param name="dispatcher">任务分发器</param>
        /// <param name="dagDependencyChecker">DAG依赖检查器</param>
        /// <param name="dagExecutionManager">DAG执行管理器</param>
        /// <param name="logger">日志记录器</param>
        public Scheduler(
            ITaskRepository repo, 
            ITaskDispatcher dispatcher, 
            IDagDependencyChecker dagDependencyChecker,
            IDagExecutionManager dagExecutionManager,
            ILogger<Scheduler> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _dagDependencyChecker = dagDependencyChecker ?? throw new ArgumentNullException(nameof(dagDependencyChecker));
            _dagExecutionManager = dagExecutionManager ?? throw new ArgumentNullException(nameof(dagExecutionManager));
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
                        
                        // 异步获取需要执行的任务（包括重试任务）
                        var allTasks = await _repo.GetAllTasksAsync();
                        var tasks = allTasks
                            .Where(t => t.Enabled && ShouldExecuteTask(t, now))
                            .ToList();

                        _logger.LogDebug("检查到 {TaskCount} 个待执行任务", tasks.Count);

                        // 并行处理任务，提高吞吐量
                        var taskExecutionTasks = tasks.Select(async task =>
                        {
                            try
                            {
                                // 对于DAG任务，需要检查依赖是否满足
                                if (task.IsDagTask)
                                {
                                    if (!await _dagExecutionManager.CanExecuteTaskAsync(task, allTasks))
                                    {
                                        _logger.LogDebug("DAG任务 {TaskName} (ID: {TaskId}) 依赖未满足，跳过执行",
                                            task.Name, task.Id);
                                        return;
                                    }
                                }

                                _logger.LogInformation("开始执行任务: {TaskName} (ID: {TaskId})，重试次数: {RetryCount}",
                                    task.Name, task.Id, task.CurrentRetryCount);
                                
                                await _dispatcher.DispatchTaskAsync(task, async result =>
                                {
                                    // 处理任务执行结果
                                    await HandleTaskExecutionResult(task, result, allTasks);
                                });
                            
                                // 异步更新任务状态
                                UpdateTaskAfterExecution(task, now);
                                await _repo.UpdateTaskAsync(task);
                                
                                _logger.LogInformation("任务执行完成: {TaskName} (ID: {TaskId})", task.Name, task.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "执行任务 {TaskName} (ID: {TaskId}) 时发生错误", task.Name, task.Id);
                                
                                // 处理任务执行异常
                                HandleTaskExecutionException(task, ex);
                                await _repo.UpdateTaskAsync(task);
                            }
                        });

                        // 等待所有任务执行完成
                        await Task.WhenAll(taskExecutionTasks);
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
        /// 判断任务是否应该执行
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="now">当前时间</param>
        /// <returns>是否应该执行</returns>
        private bool ShouldExecuteTask(ScheduledTask task, DateTime now)
        {
            // 检查是否是重试任务
            if (task.NextRetryTime.HasValue && task.NextRetryTime.Value <= now)
            {
                return true;
            }
            
            // 检查是否是正常调度的任务
            if (task.NextRunTime <= now)
            {
                return true;
            }
            
            // 检查是否是DAG任务且状态为Ready
            if (task.IsDagTask && task.DagStatus == DagExecutionStatus.Ready)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 处理任务执行结果
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="result">执行结果</param>
        /// <param name="allTasks">所有任务列表</param>
        private async Task HandleTaskExecutionResult(ScheduledTask task, TaskExecutionResult result, IEnumerable<ScheduledTask> allTasks)
        {
            if (result.Success)
            {
                _logger.LogInformation("任务 {TaskName} (ID: {TaskId}) 执行成功", task.Name, task.Id);
                
                // 任务执行成功，重置重试状态
                task.CurrentRetryCount = 0;
                task.NextRetryTime = null;

                // 如果是DAG任务，处理后续任务触发
                if (task.IsDagTask)
                {
                    var triggeredTasks = await _dagExecutionManager.OnTaskCompletedAsync(task.Id, result, allTasks);
                    
                    // 触发后续任务
                    foreach (var triggeredTask in triggeredTasks)
                    {
                        _logger.LogInformation("触发DAG后续任务: {TaskName} (ID: {TaskId})", triggeredTask.Name, triggeredTask.Id);
                        
                        // 将触发任务加入执行队列
                        await _dispatcher.DispatchTaskAsync(triggeredTask, async triggeredResult =>
                        {
                            await HandleTaskExecutionResult(triggeredTask, triggeredResult, allTasks);
                        });
                        
                        // 更新任务状态
                        await _repo.UpdateTaskAsync(triggeredTask);
                    }
                }
            }
            else
            {
                _logger.LogWarning("任务 {TaskName} (ID: {TaskId}) 执行失败: {ErrorMessage}",
                    task.Name, task.Id, result.ErrorMessage);
                
                // 如果任务还在重试中，不更新下次执行时间
                if (result.Status == ExecutionStatus.Retrying)
                {
                    _logger.LogInformation("任务 {TaskName} (ID: {TaskId}) 将在下次重试", task.Name, task.Id);
                }

                // 如果是DAG任务且执行失败，可能需要跳过后续任务
                if (task.IsDagTask)
                {
                    await _dagExecutionManager.OnTaskCompletedAsync(task.Id, result, allTasks);
                }
            }
        }

        /// <summary>
        /// 更新任务执行后的状态
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="now">当前时间</param>
        private void UpdateTaskAfterExecution(ScheduledTask task, DateTime now)
        {
            // 如果任务还有重试机会，不更新下次执行时间
            if (task.NextRetryTime.HasValue && task.CurrentRetryCount < task.MaxRetryCount)
            {
                _logger.LogDebug("任务 {TaskName} (ID: {TaskId}) 等待重试，不更新下次执行时间", task.Name, task.Id);
                return;
            }
            
            // 对于DAG任务，不更新下次执行时间（由DAG管理器控制）
            if (task.IsDagTask)
            {
                _logger.LogDebug("DAG任务 {TaskName} (ID: {TaskId}) 不更新下次执行时间", task.Name, task.Id);
                return;
            }
            
            // 任务执行完成或达到最大重试次数，更新下次执行时间
            task.NextRunTime = CronParser.GetNextOccurrence(task.CronExpression, now);
            task.LastModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 处理任务执行异常
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="exception">异常</param>
        private void HandleTaskExecutionException(ScheduledTask task, Exception exception)
        {
            _logger.LogError(exception, "任务 {TaskName} (ID: {TaskId}) 执行异常", task.Name, task.Id);
            
            // 如果任务启用了重试，增加重试计数
            if (task.EnableRetry && task.CurrentRetryCount < task.MaxRetryCount)
            {
                task.CurrentRetryCount++;
                _logger.LogWarning("任务 {TaskName} (ID: {TaskId}) 将进行第 {RetryCount} 次重试",
                    task.Name, task.Id, task.CurrentRetryCount);
            }
            else
            {
                _logger.LogError("任务 {TaskName} (ID: {TaskId}) 已达到最大重试次数或重试已禁用",
                    task.Name, task.Id);
            }
        }

        /// <summary>
        /// 启动DAG工作流
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>执行结果</returns>
        public async Task<DagExecutionResult> StartDagWorkflowAsync(Guid workflowId)
        {
            try
            {
                var allTasks = await _repo.GetAllTasksAsync();
                return await _dagExecutionManager.StartWorkflowAsync(workflowId, allTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动DAG工作流 {WorkflowId} 失败", workflowId);
                return new DagExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    WorkflowId = workflowId
                };
            }
        }

        /// <summary>
        /// 获取DAG工作流状态
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>工作流状态</returns>
        public async Task<WorkflowExecutionStatus> GetDagWorkflowStatusAsync(Guid workflowId)
        {
            return await _dagExecutionManager.GetWorkflowStatusAsync(workflowId);
        }

        /// <summary>
        /// 验证DAG工作流
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>验证结果</returns>
        public async Task<WorkflowValidationResult> ValidateDagWorkflowAsync(Guid workflowId)
        {
            try
            {
                var allTasks = await _repo.GetAllTasksAsync();
                var workflowTasks = allTasks.Where(t => t.DagWorkflowId == workflowId);
                return await _dagDependencyChecker.ValidateWorkflowAsync(workflowId, workflowTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证DAG工作流 {WorkflowId} 失败", workflowId);
                return new WorkflowValidationResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
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