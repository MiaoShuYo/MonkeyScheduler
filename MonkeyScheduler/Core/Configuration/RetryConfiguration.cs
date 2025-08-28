using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// 重试配置选项
    /// </summary>
    public class RetryConfiguration
    {
        /// <summary>
        /// 是否启用重试功能
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// 默认最大重试次数
        /// </summary>
        public int DefaultMaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 默认重试间隔（秒）
        /// </summary>
        public int DefaultRetryIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// 默认重试策略
        /// </summary>
        public RetryStrategy DefaultRetryStrategy { get; set; } = RetryStrategy.Exponential;

        /// <summary>
        /// 默认超时时间（秒）
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// 最大重试间隔（秒）
        /// </summary>
        public int MaxRetryIntervalSeconds { get; set; } = 3600;

        /// <summary>
        /// 重试冷却时间（秒）
        /// </summary>
        public int RetryCooldownSeconds { get; set; } = 300;

        /// <summary>
        /// 达到最大重试次数时是否禁用任务
        /// </summary>
        public bool DisableTaskOnMaxRetries { get; set; } = false;

        /// <summary>
        /// 是否跳过失败的节点
        /// </summary>
        public bool SkipFailedNodes { get; set; } = true;

        /// <summary>
        /// 是否启用重试日志记录
        /// </summary>
        public bool EnableRetryLogging { get; set; } = true;
    }
} 