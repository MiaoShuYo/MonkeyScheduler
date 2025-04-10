using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MonkeyScheduler.WorkerService.Services
{
    public class NodeHeartbeatService : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _schedulerUrl;
        private readonly string _workerUrl;
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(5);

        public NodeHeartbeatService(IHttpClientFactory httpClientFactory, string schedulerUrl, string workerUrl)
        {
            _httpClientFactory = httpClientFactory;
            _schedulerUrl = schedulerUrl;
            _workerUrl = workerUrl;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var httpClient = _httpClientFactory.CreateClient();

            // 首先注册节点
            await RegisterNodeAsync(httpClient);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendHeartbeatAsync(httpClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"心跳发送失败: {ex.Message}");
                }

                await Task.Delay(_heartbeatInterval, stoppingToken);
            }
        }

        private async Task RegisterNodeAsync(HttpClient httpClient)
        {
            var response = await httpClient.PostAsJsonAsync($"{_schedulerUrl}/api/worker/register", _workerUrl);
            response.EnsureSuccessStatusCode();
        }

        private async Task SendHeartbeatAsync(HttpClient httpClient)
        {
            var response = await httpClient.PostAsJsonAsync($"{_schedulerUrl}/api/worker/heartbeat", _workerUrl);
            response.EnsureSuccessStatusCode();
        }
    }
} 