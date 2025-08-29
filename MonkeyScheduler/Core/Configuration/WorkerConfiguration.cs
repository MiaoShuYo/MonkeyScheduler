namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// Worker 配置选项
    /// </summary>
    public class WorkerConfiguration
    {
        /// <summary>
        /// Worker 服务 URL
        /// </summary>
        public string WorkerUrl { get; set; } = string.Empty;

        /// <summary>
        /// 调度器服务 URL
        /// </summary>
        public string SchedulerUrl { get; set; } = "http://localhost:4057";

        /// <summary>
        /// 心跳间隔（秒）
        /// </summary>
        public int HeartbeatIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// 状态上报间隔（秒）
        /// </summary>
        public int StatusReportIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 任务执行超时时间（秒）
        /// </summary>
        public int TaskExecutionTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 最大并发执行任务数
        /// </summary>
        public int MaxConcurrentTasks { get; set; } = 5;

        /// <summary>
        /// 是否启用任务执行日志
        /// </summary>
        public bool EnableTaskExecutionLogging { get; set; } = true;

        /// <summary>
        /// 是否启用健康检查
        /// </summary>
        public bool EnableHealthCheck { get; set; } = true;

        /// <summary>
        /// 健康检查间隔（秒）
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// 是否自动注册到调度器
        /// </summary>
        public bool AutoRegisterToScheduler { get; set; } = true;

        /// <summary>
        /// 注册重试间隔（秒）
        /// </summary>
        public int RegistrationRetryIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 最大注册重试次数
        /// </summary>
        public int MaxRegistrationRetryCount { get; set; } = 10;
    }
} 