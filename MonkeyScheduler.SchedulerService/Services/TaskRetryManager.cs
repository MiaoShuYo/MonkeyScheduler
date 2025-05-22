using System.Net.Http.Json;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 任务重试管理器接口
    /// </summary>
    public interface ITaskRetryManager
    {
        /// <summary>
        /// 重试执行任务
        /// </summary>
        /// <param name="task">要重试的任务</param>
        /// <param name="failedNode">失败的节点URL</param>
        /// <returns>异步任务</returns>
        Task RetryTaskAsync(ScheduledTask task, string failedNode);
    }

    /// <summary>
    /// 任务重试管理器，处理任务执行失败后的重试逻辑
    /// </summary>
    public class TaskRetryManager : ITaskRetryManager
    {
        private readonly INodeRegistry _nodeRegistry;
        private readonly ILoadBalancer _loadBalancer;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// 初始化任务重试管理器
        /// </summary>
        /// <param name="nodeRegistry">节点注册表</param>
        /// <param name="loadBalancer">负载均衡器</param>
        /// <param name="httpClient">HTTP客户端</param>
        public TaskRetryManager(
            INodeRegistry nodeRegistry,
            ILoadBalancer loadBalancer,
            HttpClient httpClient)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
            _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// 重试执行任务
        /// </summary>
        /// <param name="task">要重试的任务</param>
        /// <param name="failedNode">失败的节点URL</param>
        /// <returns>异步任务</returns>
        public virtual async Task RetryTaskAsync(ScheduledTask task, string failedNode)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (string.IsNullOrWhiteSpace(failedNode))
                throw new ArgumentNullException(nameof(failedNode));

            // 从节点注册表中移除失败的节点
            _nodeRegistry.RemoveNode(failedNode);
            _loadBalancer.RemoveNode(failedNode);

            // 选择新的节点重试任务
            var selectedNode = _loadBalancer.SelectNode(task);
            if (string.IsNullOrEmpty(selectedNode))
            {
                throw new InvalidOperationException("没有可用的节点来重试任务");
            }

            // 向新节点发送任务执行请求
            var response = await _httpClient.PostAsJsonAsync($"{selectedNode}/api/task/execute", task);
            response.EnsureSuccessStatusCode();
        }
    }
}