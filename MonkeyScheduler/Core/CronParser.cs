using System;
using Cronos;

namespace MonkeyScheduler.Core
{
    public static class CronParser
    {
        public static DateTime GetNextOccurrence(string cronExpression, DateTime from)
        {
            if (cronExpression == null)
            {
                throw new ArgumentNullException(nameof(cronExpression));
            }

            try
            {
                var parts = cronExpression.Split(' ');
                if (parts.Length == 6 && parts[0].StartsWith("*/"))
                {
                    // 处理秒级调度
                    var seconds = int.Parse(parts[0].Substring(2));
                    // 直接返回当前时间加上指定的秒数
                    return from.AddSeconds(seconds);
                }
                else
                {
                    // 处理标准 CRON 表达式
                    var standardExpression = parts.Length == 6
                        ? string.Join(" ", parts.Skip(1).Take(5))
                        : cronExpression;

                    var expression = CronExpression.Parse(standardExpression);
                    var next = expression.GetNextOccurrence(from.ToUniversalTime(), TimeZoneInfo.Utc);
                    return next ?? DateTime.MaxValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析 CRON 表达式 '{cronExpression}' 时发生错误: {ex.Message}");
                // 如果解析失败，返回一个默认值（比如1分钟后）
                return from.AddMinutes(1);
            }
        }
    }
}