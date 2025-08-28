using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Models
{
    /// <summary>
    /// 创建任务的请求模型
    /// </summary>
    public class CreateTaskRequest
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Cron表达式，用于定义任务执行时间
        /// </summary>
        public string CronExpression { get; set; } = string.Empty;

        /// <summary>
        /// 任务描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 任务类型
        /// </summary>
        public string? TaskType { get; set; }

        /// <summary>
        /// 任务参数（JSON格式）
        /// </summary>
        public string? TaskParameters { get; set; }

        /// <summary>
        /// 是否启用重试机制
        /// </summary>
        public bool? EnableRetry { get; set; }

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int? MaxRetryCount { get; set; }

        /// <summary>
        /// 重试间隔（秒）
        /// </summary>
        public int? RetryIntervalSeconds { get; set; }

        /// <summary>
        /// 重试策略
        /// </summary>
        public RetryStrategy? RetryStrategy { get; set; }

        /// <summary>
        /// 任务超时时间（秒）
        /// </summary>
        public int? TimeoutSeconds { get; set; }
    }
} 