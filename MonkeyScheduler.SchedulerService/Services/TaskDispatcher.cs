using System.Net.Http.Json;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;

namespace MonkeyScheduler.SchedulerService.Services
{
    public class TaskDispatcher : ITaskDispatcher
    {
        private readonly INodeRegistry _nodeRegistry;
        private readonly HttpClient _httpClient;
        private readonly ILoadBalancer _loadBalancer;

        public TaskDispatcher(
            INodeRegistry nodeRegistry, 
            HttpClient httpClient,
            ILoadBalancer loadBalancer)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
        }

        public async Task DispatchTaskAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? onCompleted = null)
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
                        WorkerNodeUrl = selectedNode,
                        Success = true
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
                        WorkerNodeUrl = selectedNode ?? string.Empty,
                        Success = false
                    };
                    await onCompleted(result);
                }
                
                throw; // 重新抛出异常，让调用者决定如何处理重试
            }
        }
    }
}