namespace MonkeyScheduler.Data.MySQL.Data
{
    /// <summary>
    /// MySQL连接配置选项
    /// </summary>
    public class MySqlConnectionOptions
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// 重试延迟
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 是否启用连接池
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// 连接池最小大小
        /// </summary>
        public int MinPoolSize { get; set; } = 1;

        /// <summary>
        /// 连接池最大大小
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// 连接生命周期（秒）
        /// </summary>
        public int ConnectionLifetime { get; set; } = 300;

        /// <summary>
        /// 是否在连接返回池时重置连接
        /// </summary>
        public bool ConnectionReset { get; set; } = true;

        /// <summary>
        /// 是否自动参与分布式事务
        /// </summary>
        public bool AutoEnlist { get; set; } = false;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// 命令超时时间（秒）
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// 是否启用连接健康检查
        /// </summary>
        public bool EnableHealthCheck { get; set; } = true;

        /// <summary>
        /// 健康检查超时时间（秒）
        /// </summary>
        public int HealthCheckTimeout { get; set; } = 5;
    }
} 