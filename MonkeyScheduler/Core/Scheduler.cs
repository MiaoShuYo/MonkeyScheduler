using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.Core
{
    public class Scheduler
    {
        private readonly ITaskRepository _repo;
        private readonly ITaskExecutor _executor;
        private readonly CancellationTokenSource _cts = new();

        public Scheduler(ITaskRepository repo, ITaskExecutor executor)
        {
            _repo = repo;
            _executor = executor;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var now = DateTime.UtcNow;
                        var tasks = _repo.GetAllTasks()
                                         .Where(t => t.Enabled && t.NextRunTime <= now)
                                         .ToList();

                        foreach (var task in tasks)
                        {
                            try
                            {
                                // 先执行任务
                                await _executor.ExecuteAsync(task);
                                
                                // 然后更新下次执行时间
                                task.NextRunTime = CronParser.GetNextOccurrence(task.CronExpression, now);
                                _repo.UpdateTask(task);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"执行任务 {task.Name} 时发生错误: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"调度器发生错误: {ex.Message}");
                    }

                    await Task.Delay(1000, _cts.Token);
                }
            }, _cts.Token);
        }

        public void Stop() => _cts.Cancel();
    }
} 