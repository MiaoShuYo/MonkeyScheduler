using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services.Handlers
{
    /// <summary>
    /// HTTP任务处理器
    /// 支持发送HTTP请求的任务类型
    /// </summary>
    public class HttpTaskHandler : ITaskHandler
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HttpTaskHandler> _logger;

        public string TaskType => "http";
        public string Description => "HTTP请求任务处理器，支持GET、POST、PUT、DELETE等HTTP方法";

        public HttpTaskHandler(IHttpClientFactory httpClientFactory, ILogger<HttpTaskHandler> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<TaskExecutionResult> HandleAsync(ScheduledTask task, object? parameters = null)
        {
            var startTime = DateTime.UtcNow;
            var result = new TaskExecutionResult
            {
                TaskId = task.Id,
                StartTime = startTime,
                Status = ExecutionStatus.Running
            };

            try
            {
                var httpParams = ParseHttpParameters(parameters);
                var client = _httpClientFactory.CreateClient();

                _logger.LogInformation("执行HTTP任务: {TaskName}, URL: {Url}, Method: {Method}", 
                    task.Name, httpParams.Url, httpParams.Method);

                var request = CreateHttpRequest(httpParams);
                var response = await client.SendAsync(request);

                result.Status = ExecutionStatus.Completed;
                result.EndTime = DateTime.UtcNow;
                result.Success = response.IsSuccessStatusCode;
                result.Result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP请求失败: {response.StatusCode}";
                    result.Status = ExecutionStatus.Failed;
                }

                _logger.LogInformation("HTTP任务执行完成: {TaskName}, 状态码: {StatusCode}", 
                    task.Name, response.StatusCode);
            }
            catch (Exception ex)
            {
                result.Status = ExecutionStatus.Failed;
                result.EndTime = DateTime.UtcNow;
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;

                _logger.LogError(ex, "HTTP任务执行失败: {TaskName}", task.Name);
            }

            return result;
        }

        public async Task<bool> ValidateParametersAsync(object? parameters)
        {
            try
            {
                var httpParams = ParseHttpParameters(parameters);
                return !string.IsNullOrEmpty(httpParams.Url) && !string.IsNullOrEmpty(httpParams.Method);
            }
            catch
            {
                return false;
            }
        }

        public TaskHandlerConfiguration GetConfiguration()
        {
            return new TaskHandlerConfiguration
            {
                TaskType = TaskType,
                Description = Description,
                SupportsRetry = true,
                SupportsTimeout = true,
                DefaultTimeoutSeconds = 30,
                DefaultParameters = new Dictionary<string, object>
                {
                    ["method"] = "GET",
                    ["timeout"] = 30,
                    ["headers"] = new Dictionary<string, string>()
                }
            };
        }

        private HttpTaskParameters ParseHttpParameters(object? parameters)
        {
            if (parameters is HttpTaskParameters httpParams)
                return httpParams;

            if (parameters is string jsonString)
            {
                return JsonSerializer.Deserialize<HttpTaskParameters>(jsonString) 
                    ?? throw new ArgumentException("无效的HTTP任务参数");
            }

            if (parameters is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<HttpTaskParameters>(jsonElement.GetRawText()) 
                    ?? throw new ArgumentException("无效的HTTP任务参数");
            }

            throw new ArgumentException("无效的任务参数类型");
        }

        private HttpRequestMessage CreateHttpRequest(HttpTaskParameters parameters)
        {
            var request = new HttpRequestMessage(new HttpMethod(parameters.Method), parameters.Url);

            // 添加请求头
            if (parameters.Headers != null)
            {
                foreach (var header in parameters.Headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            // 添加请求体
            if (!string.IsNullOrEmpty(parameters.Body) && (parameters.Method == "POST" || parameters.Method == "PUT" || parameters.Method == "PATCH"))
            {
                request.Content = new StringContent(parameters.Body, Encoding.UTF8, parameters.ContentType ?? "application/json");
            }

            return request;
        }
    }

    /// <summary>
    /// HTTP任务参数
    /// </summary>
    public class HttpTaskParameters
    {
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string? Body { get; set; }
        public string? ContentType { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public int Timeout { get; set; } = 30;
    }
}
