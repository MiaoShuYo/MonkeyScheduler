namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// 数据库配置选项
    /// </summary>
    public class DatabaseConfiguration
    {
        /// <summary>
        /// MySQL 连接字符串
        /// </summary>
        public string MySQL { get; set; } = string.Empty;

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DatabaseType DatabaseType { get; set; } = DatabaseType.MySQL;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 命令超时时间（秒）
        /// </summary>
        public int CommandTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 是否启用连接池
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// 连接池最大大小
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;

        /// <summary>
        /// 连接池最小大小
        /// </summary>
        public int MinPoolSize { get; set; } = 5;
    }

    /// <summary>
    /// 数据库类型枚举
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// MySQL 数据库
        /// </summary>
        MySQL,

        /// <summary>
        /// SQL Server 数据库
        /// </summary>
        SqlServer,

        /// <summary>
        /// PostgreSQL 数据库
        /// </summary>
        PostgreSQL,

        /// <summary>
        /// SQLite 数据库
        /// </summary>
        Sqlite
    }
} 