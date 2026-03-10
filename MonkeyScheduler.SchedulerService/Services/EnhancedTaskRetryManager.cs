using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 增强的任务重试管理器接口
    /// </summary>
    public interface IEnhancedTaskRetryManager
    {
        /// <summary>
        /// 重试执行任务
        /// </summary>
        /// <param name="task">要重试的任务</param>
        /// <param name="failedNode">失败的节点URL</param>
        /// <param name="exception">失败异常</param>
        /// <returns>异步任务</returns>
        Task<bool> RetryTaskAsync(ScheduledTask task, string failedNode, Exception? exception = null);
        
        /// <summary>
        /// 检查任务是否应该重试
        /// </summary>
        /// <param name="task">任务</param>
        /// <returns>是否应该重试</returns>
        bool ShouldRetryTask(ScheduledTask task);
        
        /// <summary>
        /// 计算下次重试时间
        /// </summary>
        /// <param name="task">任务</param>
        /// <returns>下次重试时间</returns>
        DateTime CalculateNextRetryTime(ScheduledTask task);
        
        /// <summary>
        /// 重置任务的重试状态
        /// </summary>
        /// <param name="task">任务</param>
        void ResetRetryState(ScheduledTask task);
    }

    /// <summary>
    /// 增强的任务重试管理器，处理任务执行失败后的重试逻辑。
    /// 支持多种重试策略、节点跳过、详细日志等高级特性。
    /// </summary>
    public class EnhancedTaskRetryManager : IEnhancedTaskRetryManager
    {
        private readonly INodeRegistry _nodeRegistry;
        private readonly ILoadBalancer _loadBalancer;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EnhancedTaskRetryManager> _logger;
        private readonly RetryConfiguration _retryConfig;

        /// <summary>
        /// 初始化增强的任务重试管理器
        /// </summary>
        /// <param name="nodeRegistry">节点注册表</param>
        /// <param name="loadBalancer">负载均衡器</param>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="retryConfig">重试配置</param>
        public EnhancedTaskRetryManager(
            INodeRegistry nodeRegistry,
            ILoadBalancer loadBalancer,
            IHttpClientFactory httpClientFactory,
            ILogger<EnhancedTaskRetryManager> logger,
            IOptions<RetryConfiguration> retryConfig)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
            _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryConfig = retryConfig?.Value ?? new RetryConfiguration();
        }

        /// <summary>
        /// 重试执行任务。
        /// </summary>
        /// <param name="task">要重试的任务</param>
        /// <param name="failedNode">失败的节点URL</param>
        /// <param name="exception">失败异常</param>
        /// <returns>异步任务，返回是否重试成功</returns>
        /// <exception cref="InvalidOperationException">无可用节点时抛出</exception>
        /// <remarks>
        /// 1. 支持跳过失败节点，自动选择健康节点。
        /// 2. 支持多种重试策略，重试间隔可配置。
        /// 3. 达到最大重试次数后自动处理任务失败。
        /// </remarks>
        public virtual async Task<bool> RetryTaskAsync(ScheduledTask task, string failedNode, Exception? exception = null)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (string.IsNullOrWhiteSpace(failedNode))
                throw new ArgumentNullException(nameof(failedNode));

            // 检查是否应该重试
            if (!ShouldRetryTask(task))
            {
                _logger.LogWarning("任务 {TaskName} (ID: {TaskId}) 已达到最大重试次数或重试已禁用", task.Name, task.Id);
                return false;
            }

            // 记录重试信息
            if (_retryConfig.EnableRetryLogging)
            {
                _logger.LogInformation("开始重试任务 {TaskName} (ID: {TaskId})，当前重试次数: {RetryCount}/{MaxRetries}，失败节点: {FailedNode}",
                    task.Name, task.Id, task.CurrentRetryCount + 1, task.MaxRetryCount, failedNode);
            }

            try
            {
                // 如果配置了跳过失败节点，则从负载均衡器中移除
                if (_retryConfig.SkipFailedNodes)
                {
                    _nodeRegistry.RemoveNode(failedNode);
                    _loadBalancer.RemoveNode(failedNode);
                    
                    if (_retryConfig.EnableRetryLogging)
                    {
                        _logger.LogDebug("已从负载均衡器中移除失败节点: {FailedNode}", failedNode);
                    }
                }

                // 选择新的节点重试任务
                var selectedNode = _loadBalancer.SelectNode(task);
                if (string.IsNullOrEmpty(selectedNode))
                {
                    throw new InvalidOperationException("没有可用的节点来重试任务");
                }

                // 向新节点发送任务执行请求
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsJsonAsync($"{selectedNode}/api/task/execute", task);
                response.EnsureSuccessStatusCode();

                // 重试成功，重置重试状态
                ResetRetryState(task);
                
                if (_retryConfig.EnableRetryLogging)
                {
                    _logger.LogInformation("任务 {TaskName} (ID: {TaskId}) 重试成功，使用节点: {Node}", 
                        task.Name, task.Id, selectedNode);
                }

                return true;
            }
            catch (Exception ex)
            {
                // 如果是 InvalidOperationException（如"没有可用的节点来重试任务"），直接重新抛出
                if (ex is InvalidOperationException)
                {
                    throw;
                }

                // 增加重试计数
                task.CurrentRetryCount++;
                
                // 计算下次重试时间
                task.NextRetryTime = CalculateNextRetryTime(task);

                if (_retryConfig.EnableRetryLogging)
                {
                    _logger.LogError(ex, "任务 {TaskName} (ID: {TaskId}) 重试失败，重试次数: {RetryCount}/{MaxRetries}，下次重试时间: {NextRetryTime}",
                        task.Name, task.Id, task.CurrentRetryCount, task.MaxRetryCount, task.NextRetryTime);
                }

                // 如果达到最大重试次数，处理最终失败
                if (task.CurrentRetryCount >= task.MaxRetryCount)
                {
                    HandleMaxRetriesReached(task, exception ?? ex);
                }

                return false;
            }
        }

        /// <summary>
        /// 检查任务是否应该重试。
        /// </summary>
        /// <param name="task">任务</param>
        /// <returns>是否应该重试</returns>
        /// <remarks>
        /// 1. 检查全局和任务级重试开关。
        /// 2. 检查最大重试次数和下次重试时间。
        /// </remarks>
        public virtual bool ShouldRetryTask(ScheduledTask task)
        {
            if (task == null)
                return false;

            // 检查全局重试是否启用
            if (!_retryConfig.EnableRetry)
                return false;

            // 检查任务是否启用重试
            if (!task.EnableRetry)
                return false;

            // 检查是否达到最大重试次数
            if (task.CurrentRetryCount >= task.MaxRetryCount)
                return false;

            // 检查是否到了重试时间
            if (task.NextRetryTime.HasValue && task.NextRetryTime.Value > DateTime.UtcNow)
                return false;

            return true;
        }

        /// <summary>
        /// 计算下次重试时间。
        /// </summary>
        /// <param name="task">任务</param>
        /// <returns>下次重试时间</returns>
        /// <remarks>
        /// 支持指数、线性、固定等多种重试策略。
        /// </remarks>
        public virtual DateTime CalculateNextRetryTime(ScheduledTask task)
        {
            if (task == null)
                return DateTime.UtcNow;

            var baseInterval = task.RetryIntervalSeconds > 0 ? task.RetryIntervalSeconds : _retryConfig.DefaultRetryIntervalSeconds;
            var retryCount = task.CurrentRetryCount;
            var strategy = task.RetryStrategy;

            int delaySeconds;
            switch (strategy)
            {
                case RetryStrategy.Fixed:
                    delaySeconds = baseInterval;
                    break;
                    
                case RetryStrategy.Exponential:
                    delaySeconds = baseInterval * (int)Math.Pow(2, retryCount - 1);
                    break;
                    
                case RetryStrategy.Linear:
                    delaySeconds = baseInterval * retryCount;
                    break;
                    
                default:
                    delaySeconds = baseInterval;
                    break;
            }

            // 限制最大重试间隔
            delaySeconds = Math.Min(delaySeconds, _retryConfig.MaxRetryIntervalSeconds);

            return DateTime.UtcNow.AddSeconds(delaySeconds);
        }

        /// <summary>
        /// 重置任务的重试状态。
        /// </summary>
        /// <param name="task">任务</param>
        public virtual void ResetRetryState(ScheduledTask task)
        {
            if (task == null)
                return;

            task.CurrentRetryCount = 0;
            task.NextRetryTime = null;
        }

        /// <summary>
        /// 处理达到最大重试次数的情况
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="exception">异常</param>
        private void HandleMaxRetriesReached(ScheduledTask task, Exception exception)
        {
            if (_retryConfig.EnableRetryLogging)
            {
                _logger.LogError(exception, "任务 {TaskName} (ID: {TaskId}) 已达到最大重试次数 {MaxRetries}，任务最终失败",
                    task.Name, task.Id, task.MaxRetryCount);
            }

            // 如果配置了在达到最大重试次数后禁用任务
            if (_retryConfig.DisableTaskOnMaxRetries)
            {
                task.Enabled = false;
                task.LastModifiedAt = DateTime.UtcNow;
                
                if (_retryConfig.EnableRetryLogging)
                {
                    _logger.LogWarning("任务 {TaskName} (ID: {TaskId}) 已禁用，因为已达到最大重试次数", task.Name, task.Id);
                }
            }

            // 设置冷却时间
            task.NextRetryTime = DateTime.UtcNow.AddSeconds(_retryConfig.RetryCooldownSeconds);
        }
    }
} 