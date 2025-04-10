using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

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
        /// 初始化心跳服务
        /// </summary>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="schedulerUrl">调度器服务地址</param>
        /// <param name="workerUrl">当前Worker节点地址</param>
        public NodeHeartbeatService(IHttpClientFactory httpClientFactory, string schedulerUrl, string workerUrl)
        {
            _httpClientFactory = httpClientFactory;
            _schedulerUrl = schedulerUrl;
            _workerUrl = workerUrl;
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
            await RegisterNodeAsync(httpClient);

            // 循环发送心跳，直到服务停止
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 发送心跳包
                    await SendHeartbeatAsync(httpClient);
                }
                catch (Exception ex)
                {
                    // 记录心跳发送失败的错误
                    Console.WriteLine($"心跳发送失败: {ex.Message}");
                }

                // 等待下一次心跳间隔
                await Task.Delay(_heartbeatInterval, stoppingToken);
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