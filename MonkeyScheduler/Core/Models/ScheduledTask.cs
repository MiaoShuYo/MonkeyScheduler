using System;

namespace MonkeyScheduler.Core.Models
{
    public class ScheduledTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public DateTime NextRunTime { get; set; }
        public bool Enabled { get; set; } = true;
    }
} 