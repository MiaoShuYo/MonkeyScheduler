using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;
using MonkeyScheduler.Logging;

namespace MonkeyScheduler.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 创建日志记录器
            var logger = new Logger(
                dbPath: "sample_logs.db",
                maxLogCount: 1000,
                maxLogAge: TimeSpan.FromDays(3)
            );

            try
            {
                await logger.LogInfoAsync("示例程序启动");

                // 创建任务存储
                var repo = new InMemoryTaskRepository();

                // 创建任务执行器
                var executor = new SampleTaskExecutor(logger);

                // 创建调度器
                var scheduler = new Scheduler(repo, executor);

                // 添加示例任务
                var task = new ScheduledTask
                {
                    Name = "示例任务",
                    CronExpression = "*/10 * * * * *", // 每10秒执行一次
                    NextRunTime = DateTime.UtcNow
                };
                repo.AddTask(task);

                await logger.LogInfoAsync($"添加任务: {task.Name}");

                // 启动调度器
                scheduler.Start();
                await logger.LogInfoAsync("调度器已启动");

                // 等待用户输入退出
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();

                // 停止调度器
                scheduler.Stop();
                await logger.LogInfoAsync("调度器已停止");

                // 执行日志清理
                await logger.CleanupLogsAsync();
                var count = await logger.GetLogCountAsync();
                await logger.LogInfoAsync($"清理后剩余日志数量: {count}");
            }
            catch (Exception ex)
            {
                await logger.LogErrorAsync("程序运行出错", ex);
            }
            finally
            {
                await logger.LogInfoAsync("示例程序结束");
            }
        }
    }

    public class SampleTaskExecutor : ITaskExecutor
    {
        private readonly Logger _logger;

        public SampleTaskExecutor(Logger logger)
        {
            _logger = logger;
        }

        public async Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? onCompleted = null)
        {
            try
            {
                await _logger.LogInfoAsync($"开始执行任务: {task.Name}");
                
                // 模拟任务执行
                await Task.Delay(1000);
                
                // 随机生成一些日志
                var random = new Random();
                if (random.Next(100) < 10) // 10% 的概率生成警告
                {
                    await _logger.LogWarningAsync($"任务 {task.Name} 执行较慢");
                }
                
                await _logger.LogInfoAsync($"任务执行完成: {task.Name}");

                // 调用完成回调
                if (onCompleted != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Success = true,
                        EndTime = DateTime.UtcNow
                    };
                    await onCompleted(result);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"任务执行失败: {task.Name}", ex);
                
                // 调用完成回调，报告失败
                if (onCompleted != null)
                {
                    var result = new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Success = false,
                        ErrorMessage = ex.Message,
                        EndTime = DateTime.UtcNow
                    };
                    await onCompleted(result);
                }
                
                throw;
            }
        }
    }
}
