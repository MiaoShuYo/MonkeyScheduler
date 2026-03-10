using Microsoft.Extensions.Logging;
using System.Text.Json;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services
{
    /// <summary>
    /// 插件化任务执行器
    /// 支持多种任务类型的插件化执行
    /// </summary>
    public class PluginTaskExecutor : ITaskExecutor
    {
        private readonly ITaskHandlerFactory _handlerFactory;
        private readonly ILogger<PluginTaskExecutor> _logger;

        public PluginTaskExecutor(ITaskHandlerFactory handlerFactory, ILogger<PluginTaskExecutor> logger)
        {
            _handlerFactory = handlerFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? statusCallback = null)
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
                _logger.LogInformation("开始执行任务: {TaskName}, 类型: {TaskType}", task.Name, task.TaskType);

                // 获取对应的任务处理器
                var handler = _handlerFactory.GetHandler(task.TaskType);
                
                // 解析任务参数
                var parameters = ParseTaskParameters(task.TaskParameters, task.TaskType);
                
                // 验证参数
                if (!await handler.ValidateParametersAsync(parameters))
                {
                    throw new ArgumentException($"任务参数验证失败: {task.Name}");
                }

                // 执行任务
                result = await handler.HandleAsync(task, parameters);

                _logger.LogInformation("任务执行完成: {TaskName}, 成功: {Success}", 
                    task.Name, result.Success);
            }
            catch (Exception ex)
            {
                result.Status = ExecutionStatus.Failed;
                result.EndTime = DateTime.UtcNow;
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.StackTrace = ex.StackTrace;

                _logger.LogError(ex, "任务执行失败: {TaskName}", task.Name);
            }

            // 调用状态回调
            if (statusCallback != null)
            {
                await statusCallback(result);
            }
        }

        private object? ParseTaskParameters(string taskParameters, string taskType)
        {
            if (string.IsNullOrEmpty(taskParameters))
                return null;

            try
            {
                return JsonSerializer.Deserialize<object>(taskParameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析任务参数失败: {TaskType}", taskType);
                throw new ArgumentException($"无效的任务参数格式: {taskParameters}");
            }
        }
    }
}
