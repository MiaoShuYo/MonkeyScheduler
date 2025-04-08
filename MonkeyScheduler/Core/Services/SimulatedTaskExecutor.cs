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
                await Task.Delay(500); // 模拟耗时任务
                log.Result = "Success";
                log.Success = true;
            }
            catch (Exception ex)
            {
                log.Result = ex.Message;
                log.Success = false;
            }

            log.EndTime = DateTime.UtcNow;
            Console.WriteLine($"[INFO] Task {task.Name} finished. Success: {log.Success}");
        }
    }
} 