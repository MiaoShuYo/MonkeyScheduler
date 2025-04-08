using System;

namespace MonkeyScheduler.Core.Models
{
    public class TaskExecutionLog
    {
        public Guid TaskId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Result { get; set; }
        public bool Success { get; set; }
    }
} 