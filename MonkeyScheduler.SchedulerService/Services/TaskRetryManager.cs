using System;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 任务重试管理器，负责处理任务执行失败后的重试逻辑
    /// 支持配置最大重试次数和重试间隔
    /// </summary>
    public class TaskRetryManager
    {
        private readonly TaskDispatcher _dispatcher;
        private readonly int _maxRetries;
        private readonly TimeSpan _retryInterval;

        /// <summary>
        /// 初始化任务重试管理器
        /// </summary>
        /// <param name="dispatcher">任务分发器，用于重新分发任务</param>
        /// <param name="maxRetries">最大重试次数，默认3次</param>
        /// <param name="retryInterval">重试间隔，默认5秒</param>
        public TaskRetryManager(TaskDispatcher dispatcher, int maxRetries = 3, TimeSpan? retryInterval = null)
        {
            _dispatcher = dispatcher;
            _maxRetries = maxRetries;
            _retryInterval = retryInterval ?? TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// 重试执行失败的任务
        /// </summary>
        /// <param name="task">要重试的任务</param>
        /// <param name="failedNodeUrl">失败的节点URL</param>
        /// <returns>重试是否成功</returns>
        public async Task<bool> RetryTaskAsync(ScheduledTask task, string failedNodeUrl)
        {
            int retryCount = 0;
            while (retryCount < _maxRetries)
            {
                try
                {
                    // 尝试重新分发任务
                    await _dispatcher.DispatchAsync(task);
                    return true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= _maxRetries)
                    {
                        // 达到最大重试次数，记录错误并返回失败
                        Console.WriteLine($"任务 {task.Name} 重试 {_maxRetries} 次后仍然失败: {ex.Message}");
                        return false;
                    }
                    // 记录重试失败信息
                    Console.WriteLine($"任务 {task.Name} 第 {retryCount} 次重试失败: {ex.Message}");
                    // 等待重试间隔时间
                    await Task.Delay(_retryInterval);
                }
            }
            return false;
        }
    }
}