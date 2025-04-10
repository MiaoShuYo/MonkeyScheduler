using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService
{
    /// <summary>
    /// 任务分发器，负责将任务分发给Worker节点执行
    /// 集成了负载均衡和重试机制
    /// </summary>
    public class TaskDispatcher
    {
        private readonly NodeRegistry _nodeRegistry;
        private readonly HttpClient _httpClient;
        private readonly LoadBalancer _loadBalancer;
        private readonly TaskRetryManager _retryManager;
        private readonly TimeSpan _nodeTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 初始化任务分发器
        /// </summary>
        /// <param name="nodeRegistry">节点注册表</param>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="loadBalancer">负载均衡器</param>
        /// <param name="retryManager">重试管理器</param>
        public TaskDispatcher(
            NodeRegistry nodeRegistry, 
            HttpClient httpClient,
            LoadBalancer loadBalancer,
            TaskRetryManager retryManager)
        {
            _nodeRegistry = nodeRegistry;
            _httpClient = httpClient;
            _loadBalancer = loadBalancer;
            _retryManager = retryManager;
        }

        /// <summary>
        /// 分发任务到Worker节点
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <returns>异步任务</returns>
        public async Task DispatchAsync(ScheduledTask task)
        {
            // 通过负载均衡器选择节点
            var selectedNode = _loadBalancer.SelectNode(task);
            
            try
            {
                // 发送任务到选中的节点
                var response = await _httpClient.PostAsJsonAsync($"{selectedNode}/api/task/execute", task);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // 处理节点故障
                // 1. 从注册表中移除故障节点
                _nodeRegistry.RemoveNode(selectedNode);
                // 2. 减少该节点的负载计数
                _loadBalancer.DecreaseLoad(selectedNode);
                
                // 3. 尝试在其他节点上重试任务
                await _retryManager.RetryTaskAsync(task, selectedNode);
            }
        }
    }
} 