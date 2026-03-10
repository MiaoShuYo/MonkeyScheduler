using System.Net.Http.Json;
using MonkeyScheduler.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonkeyScheduler.WorkerService.Options;

namespace MonkeyScheduler.WorkerService.Services
{
    /// <summary>
    /// 状态上报服务
    /// 负责将任务执行结果上报给调度器
    /// </summary>
    public class StatusReporterService : IStatusReporterService
    {
        /// <summary>
        /// HTTP客户端工厂，用于创建HTTP客户端实例
        /// </summary>
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 调度器服务地址
        /// </summary>
        private readonly WorkerOptions _options;

        private readonly string _schedulerUrl;
        private readonly ILogger<StatusReporterService> _logger;

        /// <summary>
        /// 初始化状态上报服务
        /// </summary>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="schedulerUrl">调度器服务地址</param>
        /// <param name="workerUrl">当前Worker节点地址</param>
        public StatusReporterService(IHttpClientFactory httpClientFactory, IOptions<WorkerOptions> options, ILogger<StatusReporterService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _schedulerUrl = _options.SchedulerUrl;
            _logger = logger;
        }

        /// <summary>
        /// 向调度器上报任务执行结果
        /// </summary>
        /// <param name="result">任务执行结果</param>
        /// <returns>异步任务</returns>
        /// <exception cref="Exception">当状态上报失败时抛出</exception>
        public async Task ReportStatusAsync(TaskExecutionResult result)
        {
            // 创建HTTP客户端
            var httpClient = _httpClientFactory.CreateClient();
            
            // 设置执行结果的Worker节点信息
            result.WorkerNodeUrl = _options.WorkerUrl;

            try
            {
                _logger.LogInformation("Sending task execution result to {Url}", $"{_options.SchedulerUrl}/api/tasks/status");
                
                // 向调度器发送任务执行结果
                var response = await httpClient.PostAsJsonAsync($"{_options.SchedulerUrl}/api/tasks/status", result);
                
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Status report failed. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                }
                
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // 记录状态上报失败的错误并重新抛出异常
                _logger.LogError(ex, "状态上报失败");
                throw;
            }
        }
    }
} 