using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.WorkerService.Services
{
    /// <summary>
    /// 状态上报服务
    /// 负责将任务执行结果上报给调度器
    /// </summary>
    public class StatusReporterService
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
        /// 初始化状态上报服务
        /// </summary>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="schedulerUrl">调度器服务地址</param>
        /// <param name="workerUrl">当前Worker节点地址</param>
        public StatusReporterService(IHttpClientFactory httpClientFactory, string schedulerUrl, string workerUrl)
        {
            _httpClientFactory = httpClientFactory;
            _schedulerUrl = schedulerUrl;
            _workerUrl = workerUrl;
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
            result.WorkerNodeUrl = _workerUrl;

            try
            {
                // 向调度器发送任务执行结果
                var response = await httpClient.PostAsJsonAsync($"{_schedulerUrl}/api/task/status", result);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // 记录状态上报失败的错误并重新抛出异常
                Console.WriteLine($"状态上报失败: {ex.Message}");
                throw;
            }
        }
    }
} 