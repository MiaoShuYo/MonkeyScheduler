using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.WorkerService.Services
{
    public class StatusReporterService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _schedulerUrl;
        private readonly string _workerUrl;

        public StatusReporterService(IHttpClientFactory httpClientFactory, string schedulerUrl, string workerUrl)
        {
            _httpClientFactory = httpClientFactory;
            _schedulerUrl = schedulerUrl;
            _workerUrl = workerUrl;
        }

        public async Task ReportStatusAsync(TaskExecutionResult result)
        {
            var httpClient = _httpClientFactory.CreateClient();
            result.WorkerNodeUrl = _workerUrl;

            try
            {
                var response = await httpClient.PostAsJsonAsync($"{_schedulerUrl}/api/task/status", result);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"状态上报失败: {ex.Message}");
                throw;
            }
        }
    }
} 