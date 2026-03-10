using System;

namespace MonkeyScheduler.Core.Models
{
    /// <summary>
    /// CRON表达式解析异常
    /// </summary>
    public class CronParseException : Exception
    {
        /// <summary>
        /// 导致解析失败的CRON表达式
        /// </summary>
        public string CronExpression { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cronExpression">导致解析失败的CRON表达式</param>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public CronParseException(string cronExpression, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            CronExpression = cronExpression;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cronExpression">导致解析失败的CRON表达式</param>
        /// <param name="innerException">内部异常</param>
        public CronParseException(string cronExpression, Exception innerException)
            : base($"解析CRON表达式 '{cronExpression}' 时发生错误: {innerException.Message}", innerException)
        {
            CronExpression = cronExpression;
        }
    }
} 