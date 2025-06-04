using System;

namespace MonkeyScheduler.Core.Models
{
    public class TaskExecutionResult
    {
        public Guid TaskId { get; set; }
        public ExecutionStatus Status { get; set; }
        public string? Result { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string WorkerNodeUrl { get; set; } = string.Empty;
        public bool Success { get; set; }
        /// <summary>
        /// 执行记录的唯一标识符
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 错误堆栈跟踪
        /// 当任务执行失败时，存储详细的异常堆栈信息
        /// </summary>
        public string StackTrace { get; set; }
    }

    public enum ExecutionStatus
    {
        Running,
        Completed,
        Failed,
        Retrying
    }
} 