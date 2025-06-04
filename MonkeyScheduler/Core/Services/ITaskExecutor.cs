using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services
{
    public interface ITaskExecutor
    {
        Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? statusCallback = null);
    }
} 