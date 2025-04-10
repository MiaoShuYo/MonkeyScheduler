 #nullable enable
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService
{
    public class TaskDispatcher : ITaskExecutor
    {
        private readonly INodeRegistry _nodeRegistry;
        private readonly HttpClient _httpClient;
        private readonly ILoadBalancer _loadBalancer;
        private readonly ITaskRetryManager _retryManager;
        private readonly TimeSpan _nodeTimeout = TimeSpan.FromSeconds(30);

        public TaskDispatcher(
            INodeRegistry nodeRegistry, 
            HttpClient httpClient,
            ILoadBalancer loadBalancer,
            ITaskRetryManager retryManager)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
            _retryManager = retryManager ?? throw new ArgumentNullException(nameof(retryManager));
        }

        public async Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? onCompleted = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var startTime = DateTime.UtcNow;
            string? selectedNode = null;
            
            try
            {
                selectedNode = _loadBalancer.SelectNode(task);
                
                // 向选中的节点发送任务执行请求
                var response = await _httpClient.PostAsJsonAsync($"{selectedNode}/api/task/execute", task);
                response.EnsureSuccessStatusCode();

                // 调用完成回调
                if (onCompleted != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = ExecutionStatus.Completed,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        WorkerNodeUrl = selectedNode
                    };
                    await onCompleted(result);
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(selectedNode))
                {
                    // 节点故障处理流程
                    _nodeRegistry.RemoveNode(selectedNode);
                    _loadBalancer.RemoveNode(selectedNode);
                }

                // 调用完成回调，报告失败
                if (onCompleted != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = ExecutionStatus.Failed,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        ErrorMessage = ex.Message,
                        WorkerNodeUrl = selectedNode ?? string.Empty
                    };
                    await onCompleted(result);
                }
                
                // 通过重试管理器在其他可用节点上重试任务
                if (!string.IsNullOrEmpty(selectedNode))
                {
                    await _retryManager.RetryTaskAsync(task, selectedNode);
                }
            }
        }
    }
}