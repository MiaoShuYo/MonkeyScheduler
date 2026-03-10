namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// MonkeyScheduler 根配置类
    /// </summary>
    public class MonkeySchedulerConfiguration
    {
        /// <summary>
        /// 数据库配置
        /// </summary>
        public DatabaseConfiguration Database { get; set; } = new();

        /// <summary>
        /// 重试配置
        /// </summary>
        public RetryConfiguration Retry { get; set; } = new();

        /// <summary>
        /// 调度器配置
        /// </summary>
        public SchedulerConfiguration Scheduler { get; set; } = new();

        /// <summary>
        /// Worker 配置
        /// </summary>
        public WorkerConfiguration Worker { get; set; } = new();

        /// <summary>
        /// 负载均衡器配置
        /// </summary>
        public LoadBalancerConfiguration LoadBalancer { get; set; } = new();

        /// <summary>
        /// 日志配置
        /// </summary>
        public LoggingConfiguration Logging { get; set; } = new();

        /// <summary>
        /// 安全配置
        /// </summary>
        public SecurityConfiguration Security { get; set; } = new();
    }

    /// <summary>
    /// 日志配置选项
    /// </summary>
    public class LoggingConfiguration
    {
        /// <summary>
        /// 日志级别
        /// </summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// 是否启用结构化日志
        /// </summary>
        public bool EnableStructuredLogging { get; set; } = true;

        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string LogFilePath { get; set; } = "logs/monkeyscheduler.log";

        /// <summary>
        /// 最大日志文件大小（MB）
        /// </summary>
        public int MaxLogFileSizeMB { get; set; } = 100;

        /// <summary>
        /// 保留的日志文件数量
        /// </summary>
        public int RetainedLogFileCount { get; set; } = 30;

        /// <summary>
        /// 是否启用控制台日志
        /// </summary>
        public bool EnableConsoleLogging { get; set; } = true;

        /// <summary>
        /// 是否启用文件日志
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;
    }

    /// <summary>
    /// 安全配置选项
    /// </summary>
    public class SecurityConfiguration
    {
        /// <summary>
        /// 是否启用身份验证
        /// </summary>
        public bool EnableAuthentication { get; set; } = false;

        /// <summary>
        /// 是否启用授权
        /// </summary>
        public bool EnableAuthorization { get; set; } = false;

        /// <summary>
        /// JWT 密钥
        /// </summary>
        public string JwtSecret { get; set; } = string.Empty;

        /// <summary>
        /// JWT 过期时间（小时）
        /// </summary>
        public int JwtExpirationHours { get; set; } = 24;

        /// <summary>
        /// 是否启用 HTTPS
        /// </summary>
        public bool EnableHttps { get; set; } = false;

        /// <summary>
        /// 允许的 CORS 源
        /// </summary>
        public string[] AllowedCorsOrigins { get; set; } = Array.Empty<string>();

        /// <summary>
        /// API 密钥
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }
} 