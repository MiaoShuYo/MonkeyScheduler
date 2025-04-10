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
    }

    public enum ExecutionStatus
    {
        Running,
        Completed,
        Failed,
        Retrying
    }
} 