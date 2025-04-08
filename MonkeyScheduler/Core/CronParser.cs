using System;
using Cronos;

namespace MonkeyScheduler.Core
{
    public static class CronParser
    {
        public static DateTime GetNextOccurrence(string cronExpression, DateTime from)
        {
            var expression = CronExpression.Parse(cronExpression);
            return expression.GetNextOccurrence(from, TimeZoneInfo.Local) ?? DateTime.MaxValue;
        }
    }
} 