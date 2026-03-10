using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;

namespace MonkeyScheduler.WorkerService.Services
{
    /// <summary>
    /// 默认任务执行器配置
    /// </summary>
    public class DefaultTaskExecutorOptions
    {
        /// <summary>
        /// 调度器服务地址
        /// </summary>
        public string SchedulerUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// 默认任务执行器，负责执行任务并上报结果
    /// </summary>
    public class DefaultTaskExecutor : ITaskExecutor
    {
        private readonly IStatusReporterService _statusReporterService;
        private readonly ILogger<DefaultTaskExecutor> _logger;
        private readonly DefaultTaskExecutorOptions _options;

        /// <summary>
        /// 初始化默认任务执行器
        /// </summary>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        /// <param name="options">配置选项</param>
        public DefaultTaskExecutor(IStatusReporterService statusReporterService, IOptions<DefaultTaskExecutorOptions> options, ILogger<DefaultTaskExecutor> logger)
        {
            _statusReporterService = statusReporterService;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <param name="statusCallback">任务完成时的回调函数</param>
        /// <returns>异步任务</returns>
        public async Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? statusCallback = null)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // 执行本地任务逻辑（此处为示例：简单等待模拟任务耗时）
                _logger.LogInformation("开始执行任务: {TaskName}", task.Name);
                await Task.Delay(500);
                _logger.LogInformation("任务执行完成: {TaskName}", task.Name);

                // 调用完成回调
                if (statusCallback != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = ExecutionStatus.Completed,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        Success = true
                    };
                    await statusCallback(result);
                }

                // 上报成功状态
                await _statusReporterService.ReportStatusAsync(new TaskExecutionResult
                {
                    TaskId = task.Id,
                    Status = ExecutionStatus.Completed,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                // 如果执行失败，调用回调并上报失败状态
                if (statusCallback != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = ExecutionStatus.Failed,
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                    await statusCallback(result);
                }

                await _statusReporterService.ReportStatusAsync(new TaskExecutionResult
                {
                    TaskId = task.Id,
                    Status = ExecutionStatus.Failed,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    Success = false,
                    ErrorMessage = ex.Message
                });
                throw new Exception($"任务执行失败: {ex.Message}", ex);
            }
        }
    }
} 