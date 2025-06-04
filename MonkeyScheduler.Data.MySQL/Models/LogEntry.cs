namespace MonkeyScheduler.Data.MySQL.Models
{
    /// <summary>
    /// 日志条目数据模型
    /// 用于在MySQL数据库中存储日志信息
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 日志记录的唯一标识符
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 日志级别
        /// 例如：Debug, Info, Warning, Error, Critical
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// 日志消息内容
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息（如果有）
        /// 存储异常的详细信息，包括堆栈跟踪
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// 日志记录的时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志来源
        /// 记录产生日志的组件或模块名称
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 日志类别
        /// 用于对日志进行分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 事件ID
        /// 用于标识特定事件的唯一标识符
        /// </summary>
        public string EventId { get; set; } = string.Empty;
    }
} 