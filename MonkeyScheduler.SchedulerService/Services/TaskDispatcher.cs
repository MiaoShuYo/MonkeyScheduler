using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 任务分发器，负责将调度任务分配到合适的 Worker 节点，并处理重试与异常。
    /// </summary>
    /// <remarks>
    /// 设计要点：
    /// 1. 通过负载均衡策略选择节点，支持多种策略扩展。
    /// 2. 支持任务失败后的自动重试，重试策略可配置。
    /// 3. 节点异常时自动移除，保证系统健壮性。
    /// </remarks>
    public class TaskDispatcher : ITaskDispatcher
    {
        private readonly INodeRegistry _nodeRegistry;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoadBalancer _loadBalancer;
        private readonly IEnhancedTaskRetryManager _retryManager;
        private readonly ILogger<TaskDispatcher> _logger;
        private readonly RetryConfiguration _retryConfig;

        public TaskDispatcher(
            INodeRegistry nodeRegistry, 
            IHttpClientFactory httpClientFactory,
            ILoadBalancer loadBalancer,
            IEnhancedTaskRetryManager retryManager,
            ILogger<TaskDispatcher> logger,
            IOptions<RetryConfiguration> retryConfig)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
            _retryManager = retryManager ?? throw new ArgumentNullException(nameof(retryManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryConfig = retryConfig?.Value ?? new RetryConfiguration();
        }

        /// <summary>
        /// 分发任务到 Worker 节点，支持回调和异常处理。
        /// </summary>
        /// <param name="task">待分发的调度任务</param>
        /// <param name="onCompleted">任务完成后的回调，可为 null</param>
        /// <exception cref="InvalidOperationException">无可用节点时抛出</exception>
        /// <exception cref="Exception">任务执行及重试均失败时抛出</exception>
        /// <remarks>
        /// 1. 先通过负载均衡策略选择节点。
        /// 2. 任务执行失败时，若启用重试则自动重试，否则移除节点。
        /// 3. 所有异常均通过日志记录，回调始终在抛出异常前执行。
        /// </remarks>
        public async Task DispatchTaskAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? onCompleted = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var startTime = DateTime.UtcNow;
            string? selectedNode = null;
            Exception? lastException = null;
            
            try
            {
                // 检查任务是否应该重试
                if (_retryManager.ShouldRetryTask(task))
                {
                    _logger.LogInformation("任务 {TaskName} (ID: {TaskId}) 正在重试，当前重试次数: {RetryCount}",
                        task.Name, task.Id, task.CurrentRetryCount);
                }

                selectedNode = _loadBalancer.SelectNode(task);
                
                // 检查选中的节点是否有效
                if (string.IsNullOrEmpty(selectedNode))
                {
                    throw new InvalidOperationException("没有可用的Worker节点");
                }
                
                // 向选中的节点发送任务执行请求
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsJsonAsync($"{selectedNode}/api/task/execute", task);
                response.EnsureSuccessStatusCode();

                // 任务执行成功，重置重试状态
                _retryManager.ResetRetryState(task);

                // 调用完成回调
                if (onCompleted != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = ExecutionStatus.Completed,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        WorkerNodeUrl = selectedNode,
                        Success = true
                    };
                    await onCompleted(result);
                }

                _logger.LogInformation("任务 {TaskName} (ID: {TaskId}) 执行成功，使用节点: {Node}",
                    task.Name, task.Id, selectedNode);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex, "任务 {TaskName} (ID: {TaskId}) 执行失败，节点: {Node}",
                    task.Name, task.Id, selectedNode ?? "未知");

                // 尝试重试任务
                if (!string.IsNullOrEmpty(selectedNode) && task.EnableRetry)
                {
                    try
                    {
                        var retrySuccess = await _retryManager.RetryTaskAsync(task, selectedNode, ex);
                        if (retrySuccess)
                {
                            // 重试成功，调用完成回调
                            if (onCompleted != null)
                            {
                                var result = new TaskExecutionResult
                                {
                                    TaskId = task.Id,
                                    Status = ExecutionStatus.Completed,
                                    StartTime = startTime,
                                    EndTime = DateTime.UtcNow,
                                    WorkerNodeUrl = selectedNode,
                                    Success = true,
                                    Result = "任务通过重试成功执行"
                                };
                                await onCompleted(result);
                            }
                            return; // 重试成功，直接返回
                        }
                        else
                        {
                            // 重试失败，移除失败的节点
                            _nodeRegistry.RemoveNode(selectedNode);
                            _loadBalancer.RemoveNode(selectedNode);
                            _logger.LogWarning("任务 {TaskName} (ID: {TaskId}) 重试失败，已移除失败节点: {Node}",
                                task.Name, task.Id, selectedNode);
                        }
                    }
                    catch (Exception retryEx)
                    {
                        _logger.LogError(retryEx, "任务 {TaskName} (ID: {TaskId}) 重试过程中发生错误",
                            task.Name, task.Id);
                        lastException = retryEx;
                        
                        // 重试过程中发生异常，也移除失败的节点
                        _nodeRegistry.RemoveNode(selectedNode);
                        _loadBalancer.RemoveNode(selectedNode);
                        _logger.LogWarning("任务 {TaskName} (ID: {TaskId}) 重试过程中发生异常，已移除失败节点: {Node}",
                            task.Name, task.Id, selectedNode);
                    }
                }
                else if (!string.IsNullOrEmpty(selectedNode))
                {
                    // 未启用重试，直接移除失败的节点
                    _nodeRegistry.RemoveNode(selectedNode);
                    _loadBalancer.RemoveNode(selectedNode);
                    _logger.LogWarning("任务 {TaskName} (ID: {TaskId}) 未启用重试，已移除失败节点: {Node}",
                        task.Name, task.Id, selectedNode);
                }

                // 如果任务还有重试机会，不抛出异常，让调度器继续处理
                if (task.CurrentRetryCount < task.MaxRetryCount && task.EnableRetry)
                {
                    _logger.LogWarning("任务 {TaskName} (ID: {TaskId}) 将在 {NextRetryTime} 重试",
                        task.Name, task.Id, task.NextRetryTime);
                    return;
                }

                // 达到最大重试次数或未启用重试，先执行回调，再抛出异常
                if (onCompleted != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = task.CurrentRetryCount >= task.MaxRetryCount ? ExecutionStatus.Failed : ExecutionStatus.Retrying,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        ErrorMessage = lastException?.Message,
                        WorkerNodeUrl = selectedNode ?? string.Empty,
                        Success = false,
                        StackTrace = lastException?.StackTrace ?? string.Empty
                    };
                    await onCompleted(result);
                }
                
                // 抛出原始异常
                if (lastException != null)
                {
                    throw lastException;
                }
                else
                {
                    throw new Exception($"任务 {task.Name} 执行失败，已重试 {task.CurrentRetryCount} 次");
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(selectedNode))
                {
                    _loadBalancer.DecreaseLoad(selectedNode);
                }
            }
        }
    }
}