using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services;

/// <summary>
/// 任务执行结果仓储类
/// 负责任务执行记录的持久化和查询操作
/// </summary>
public interface ITaskExecutionResult
{
    Task<int> AddExecutionResultAsync(TaskExecutionResult result);
    Task UpdateExecutionResultAsync(TaskExecutionResult result);

    Task<IEnumerable<TaskExecutionResult>> GetTaskExecutionResultsAsync(int taskId, DateTime startTime,
        DateTime endTime);

    Task<TaskExecutionResult> GetLastExecutionResultAsync(int taskId);
}