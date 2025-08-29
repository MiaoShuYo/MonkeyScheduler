namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// 调度器配置选项
    /// </summary>
    public class SchedulerConfiguration
    {
        /// <summary>
        /// 调度器检查间隔（毫秒）
        /// </summary>
        public int CheckIntervalMilliseconds { get; set; } = 1000;

        /// <summary>
        /// 是否在启动时立即执行到期任务
        /// </summary>
        public bool ExecuteDueTasksOnStartup { get; set; } = true;

        /// <summary>
        /// 最大并发执行任务数
        /// </summary>
        public int MaxConcurrentTasks { get; set; } = 10;

        /// <summary>
        /// 任务执行超时时间（秒）
        /// </summary>
        public int TaskExecutionTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 是否启用任务执行日志
        /// </summary>
        public bool EnableTaskExecutionLogging { get; set; } = true;

        /// <summary>
        /// 是否启用任务统计信息
        /// </summary>
        public bool EnableTaskStatistics { get; set; } = true;

        /// <summary>
        /// 统计信息收集间隔（秒）
        /// </summary>
        public int StatisticsCollectionIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 是否启用健康检查
        /// </summary>
        public bool EnableHealthCheck { get; set; } = true;

        /// <summary>
        /// 健康检查间隔（秒）
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 30;
    }
} 