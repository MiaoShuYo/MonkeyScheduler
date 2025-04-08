using System;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services
{
    public class SimulatedTaskExecutor : ITaskExecutor
    {
        public async Task ExecuteAsync(ScheduledTask task)
        {
            var log = new TaskExecutionLog
            {
                TaskId = task.Id,
                StartTime = DateTime.UtcNow
            };

            try
            {
                Console.WriteLine($"[INFO] Executing: {task.Name}");
                log.Result = "Success";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Task execution failed: {ex.Message}");
                log.Result = "Failed";
            }
            finally
            {
                log.EndTime = DateTime.UtcNow;
                Console.WriteLine($"[INFO] Task completed: {task.Name}, Result: {log.Result}");
            }
        }
    }
} 