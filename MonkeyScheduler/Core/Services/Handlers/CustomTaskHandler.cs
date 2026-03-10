using Microsoft.Extensions.Logging;
using System.Text.Json;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services.Handlers
{
    /// <summary>
    /// 自定义任务处理器示例
    /// 演示如何创建自定义的任务处理器
    /// </summary>
    public class CustomTaskHandler : ITaskHandler
    {
        private readonly ILogger<CustomTaskHandler> _logger;

        public string TaskType => "custom";
        public string Description => "自定义任务处理器示例，演示如何扩展任务类型";

        public CustomTaskHandler(ILogger<CustomTaskHandler> logger)
        {
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
                var customParams = ParseCustomParameters(parameters);
                
                _logger.LogInformation("执行自定义任务: {TaskName}, 操作: {Operation}", 
                    task.Name, customParams.Operation);

                // 模拟任务执行
                await Task.Delay(customParams.DelayMilliseconds);

                // 根据操作类型执行不同的逻辑
                string operationResult = customParams.Operation.ToLower() switch
                {
                    "echo" => $"回显消息: {customParams.Message}",
                    "calculate" => $"计算结果: {customParams.Number1 + customParams.Number2}",
                    "format" => $"格式化时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    _ => $"未知操作: {customParams.Operation}"
                };

                result.Status = ExecutionStatus.Completed;
                result.EndTime = DateTime.UtcNow;
                result.Success = true;
                result.Result = operationResult;

                _logger.LogInformation("自定义任务执行完成: {TaskName}, 结果: {Result}", 
                    task.Name, operationResult);
            }
            catch (Exception ex)
            {
                result.Status = ExecutionStatus.Failed;
                result.EndTime = DateTime.UtcNow;
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;

                _logger.LogError(ex, "自定义任务执行失败: {TaskName}", task.Name);
            }

            return result;
        }

        public async Task<bool> ValidateParametersAsync(object? parameters)
        {
            try
            {
                var customParams = ParseCustomParameters(parameters);
                return !string.IsNullOrEmpty(customParams.Operation);
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
                DefaultTimeoutSeconds = 60,
                DefaultParameters = new Dictionary<string, object>
                {
                    ["operation"] = "echo",
                    ["message"] = "Hello World",
                    ["delayMilliseconds"] = 1000,
                    ["number1"] = 0,
                    ["number2"] = 0
                }
            };
        }

        private CustomTaskParameters ParseCustomParameters(object? parameters)
        {
            if (parameters is CustomTaskParameters customParams)
                return customParams;

            if (parameters is string jsonString)
            {
                return JsonSerializer.Deserialize<CustomTaskParameters>(jsonString) 
                    ?? throw new ArgumentException("无效的自定义任务参数");
            }

            if (parameters is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<CustomTaskParameters>(jsonElement.GetRawText()) 
                    ?? throw new ArgumentException("无效的自定义任务参数");
            }

            throw new ArgumentException("无效的任务参数类型");
        }
    }

    /// <summary>
    /// 自定义任务参数
    /// </summary>
    public class CustomTaskParameters
    {
        public string Operation { get; set; } = "echo";
        public string Message { get; set; } = "Hello World";
        public int DelayMilliseconds { get; set; } = 1000;
        public int Number1 { get; set; } = 0;
        public int Number2 { get; set; } = 0;
    }
}
