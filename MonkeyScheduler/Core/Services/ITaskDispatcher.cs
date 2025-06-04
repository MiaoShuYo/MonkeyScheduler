using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services;

public interface ITaskDispatcher
{
    Task DispatchTaskAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? onCompleted = null);
}