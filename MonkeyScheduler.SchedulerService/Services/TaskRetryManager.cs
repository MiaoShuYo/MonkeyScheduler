using System;
using System.Threading.Tasks;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 任务重试管理器接口
    /// </summary>
    public interface ITaskRetryManager
    {
        /// <summary>
        /// 重试执行任务
        /// </summary>
        /// <param name="task">要重试的任务</param>
        /// <param name="failedNode">失败的节点URL</param>
        /// <returns>异步任务</returns>
        Task RetryTaskAsync(ScheduledTask task, string failedNode);
    }

    /// <summary>
    /// 任务重试管理器，处理任务执行失败后的重试逻辑
    /// </summary>
    public class TaskRetryManager : ITaskRetryManager
    {
        private readonly TaskDispatcher _taskDispatcher;

        /// <summary>
        /// 初始化任务重试管理器
        /// </summary>
        /// <param name="taskDispatcher">任务分发器</param>
        public TaskRetryManager(TaskDispatcher taskDispatcher)
        {
            _taskDispatcher = taskDispatcher ?? throw new ArgumentNullException(nameof(taskDispatcher));
        }

        /// <summary>
        /// 重试执行任务
        /// </summary>
        /// <param name="task">要重试的任务</param>
        /// <param name="failedNode">失败的节点URL</param>
        /// <returns>异步任务</returns>
        public virtual async Task RetryTaskAsync(ScheduledTask task, string failedNode)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (string.IsNullOrWhiteSpace(failedNode))
                throw new ArgumentNullException(nameof(failedNode));

            // 在这里可以添加重试策略，例如延迟重试、最大重试次数等
            // 目前简单地直接重试一次
            await _taskDispatcher.ExecuteAsync(task);
        }
    }
}