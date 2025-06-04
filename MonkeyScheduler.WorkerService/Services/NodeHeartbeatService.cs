using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.WorkerService.Services
{
    /// <summary>
    /// Worker节点心跳服务
    /// 负责定期向调度器发送心跳包，保持节点活跃状态
    /// 继承自BackgroundService以作为后台服务运行
    /// </summary>
    public class NodeHeartbeatService : BackgroundService
    {
        /// <summary>
        /// HTTP客户端工厂，用于创建HTTP客户端实例
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 调度器服务地址
        /// </summary>
        private readonly string _schedulerUrl;

        /// <summary>
        /// 当前Worker节点的地址
        /// </summary>
        private readonly string _workerUrl;

        /// <summary>
        /// 心跳发送间隔时间
        /// </summary>
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 最大重试次数
        /// </summary>
        private readonly int _maxRetries = 5;

        /// <summary>
        /// 重试间隔基础时间（毫秒）
        /// </summary>
        private readonly int _retryDelayBase = 1000;

        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<NodeHeartbeatService> _logger;

        /// <summary>
        /// 初始化心跳服务
        /// </summary>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="schedulerUrl">调度器服务地址</param>
        /// <param name="workerUrl">当前Worker节点地址</param>
        /// <param name="logger">日志记录器</param>
        public NodeHeartbeatService(
            IHttpClientFactory httpClientFactory, 
            string schedulerUrl, 
            string workerUrl,
            ILogger<NodeHeartbeatService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _schedulerUrl = schedulerUrl;
            _workerUrl = workerUrl;
            _logger = logger;
        }

        /// <summary>
        /// 执行后台服务
        /// 首先注册节点，然后定期发送心跳
        /// </summary>
        /// <param name="stoppingToken">取消令牌</param>
        /// <returns>异步任务</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 创建HTTP客户端
            var httpClient = _httpClientFactory.CreateClient();

            // 首先向调度器注册当前节点
            await RegisterNodeWithRetryAsync(httpClient, stoppingToken);

            // 循环发送心跳，直到服务停止
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 发送心跳包
                    await SendHeartbeatWithRetryAsync(httpClient, stoppingToken);
                }
                catch (Exception ex)
                {
                    // 记录心跳发送失败的错误
                    _logger.LogError(ex, "心跳发送失败: {Message}", ex.Message);
                    throw new Exception("心跳发送失败", ex);
                }

                // 等待下一次心跳间隔
                await Task.Delay(_heartbeatInterval, stoppingToken);
                _logger.LogInformation("心跳发送成功");
            }
        }

        /// <summary>
        /// 向调度器注册当前Worker节点（带重试）
        /// </summary>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        private async Task RegisterNodeWithRetryAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < _maxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RegisterNodeAsync(httpClient);
                    success = true;
                    _logger.LogInformation("节点注册成功");
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "节点注册失败，尝试次数: {RetryCount}/{MaxRetries}", retryCount, _maxRetries);

                    if (retryCount < _maxRetries)
                    {
                        // 使用指数退避策略计算等待时间
                        int delay = _retryDelayBase * (int)Math.Pow(2, retryCount - 1);
                        await Task.Delay(delay, cancellationToken);
                    }
                    else
                    {
                        _logger.LogError("节点注册失败，已达到最大重试次数");
                        throw new Exception("节点注册失败，已达到最大重试次数", ex);
                    }
                }
            }
        }

        /// <summary>
        /// 向调度器发送心跳包（带重试）
        /// </summary>
        /// <param name="httpClient">HTTP客户端</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>异步任务</returns>
        private async Task SendHeartbeatWithRetryAsync(HttpClient httpClient, CancellationToken cancellationToken)
        {
            int retryCount = 0;
            bool success = false;

            while (!success && retryCount < _maxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SendHeartbeatAsync(httpClient);
                    success = true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogWarning(ex, "心跳发送失败，尝试次数: {RetryCount}/{MaxRetries}", retryCount, _maxRetries);

                    if (retryCount < _maxRetries)
                    {
                        // 使用指数退避策略计算等待时间
                        int delay = _retryDelayBase * (int)Math.Pow(2, retryCount - 1);
                        await Task.Delay(delay, cancellationToken);
                    }
                    else
                    {
                        _logger.LogError("心跳发送失败，已达到最大重试次数");
                        throw new Exception("心跳发送失败，已达到最大重试次数", ex);
                    }
                }
            }
        }

        /// <summary>
        /// 向调度器注册当前Worker节点
        /// </summary>
        /// <param name="httpClient">HTTP客户端</param>
        /// <returns>异步任务</returns>
        private async Task RegisterNodeAsync(HttpClient httpClient)
        {
            var response = await httpClient.PostAsJsonAsync($"{_schedulerUrl}/api/worker/register", _workerUrl);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 向调度器发送心跳包
        /// </summary>
        /// <param name="httpClient">HTTP客户端</param>
        /// <returns>异步任务</returns>
        private async Task SendHeartbeatAsync(HttpClient httpClient)
        {
            var response = await httpClient.PostAsJsonAsync($"{_schedulerUrl}/api/worker/heartbeat", _workerUrl);
            response.EnsureSuccessStatusCode();
        }
    }
} 