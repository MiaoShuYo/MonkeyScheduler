using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.WorkerService.Services;

namespace WorkerService.Tasks;

/// <summary>
/// 自定义任务执行器
/// 实现具体的任务执行逻辑
/// </summary>
public class CustomTaskExecutor : ITaskExecutor
{
    private readonly ILogger<CustomTaskExecutor> _logger;
    private readonly IStatusReporterService _statusReporter;

    public CustomTaskExecutor(
        ILogger<CustomTaskExecutor> logger,
        IStatusReporterService statusReporter)
    {
        _logger = logger;
        _statusReporter = statusReporter;
    }

    public async Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? statusCallback = null)
    {
        // Sanitize task.Name to prevent log forging
        var sanitizedTaskName = (task.Name ?? string.Empty)
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace(Environment.NewLine, "");

        var startTime = DateTime.UtcNow;
        var result = new TaskExecutionResult
        {
            TaskId = task.Id,
            StartTime = startTime,
            Status = ExecutionStatus.Running
        };

        try
        {
            _logger.LogInformation("开始执行任务: {TaskName}", sanitizedTaskName);

            // 根据任务名称执行不同的逻辑
            switch (task.Name.ToLower())
            {
                case "数据备份":
                    await ExecuteBackupTask(task);
                    break;
                case "数据清理":
                    await ExecuteCleanupTask(task);
                    break;
                case "系统检查":
                    await ExecuteSystemCheckTask(task);
                    break;
                default:
                    throw new NotImplementedException($"未实现的任务类型: {sanitizedTaskName}");
            }

            result.Status = ExecutionStatus.Completed;
            result.EndTime = DateTime.UtcNow;
            result.Success = true;
            result.StackTrace = string.Empty;
            _logger.LogInformation("任务执行完成: {TaskName}", sanitizedTaskName);
        }
        catch (Exception ex)
        {
            result.Status = ExecutionStatus.Failed;
            result.EndTime = DateTime.UtcNow;
            result.ErrorMessage = ex.Message;
            result.Success = false;
            result.StackTrace = ex.StackTrace ?? string.Empty;
            _logger.LogError(ex, "任务执行失败: {TaskName}", sanitizedTaskName);
        }
        finally
        {
            // 上报执行结果
            await _statusReporter.ReportStatusAsync(result);
            
            // 如果有回调，执行回调
            if (statusCallback != null)
            {
                await statusCallback(result);
            }
        }
    }

    private async Task ExecuteBackupTask(ScheduledTask task)
    {
        _logger.LogInformation("执行备份任务");
        // 模拟任务执行
        await Task.Delay(1000);
    }

    private async Task ExecuteCleanupTask(ScheduledTask task)
    {
        _logger.LogInformation("执行清理任务");
        // 模拟任务执行
        await Task.Delay(1000);
    }

    private async Task ExecuteSystemCheckTask(ScheduledTask task)
    {
        _logger.LogInformation("执行系统检查任务");
        // 模拟任务执行
        await Task.Delay(1000);
    }
}