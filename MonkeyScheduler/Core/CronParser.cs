using System;
using System.Globalization;
using Cronos;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Logging;

namespace MonkeyScheduler.Core
{
    public static class CronParser
    {
        private static ILogger? _logger;

        /// <summary>
        /// 设置日志记录器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取下一个执行时间
        /// </summary>
        /// <param name="cronExpression">CRON表达式</param>
        /// <param name="from">起始时间</param>
        /// <returns>下一个执行时间</returns>
        /// <exception cref="ArgumentNullException">当cronExpression为null时抛出</exception>
        /// <exception cref="CronParseException">当CRON表达式解析失败时抛出</exception>
        public static DateTime GetNextOccurrence(string cronExpression, DateTime from)
        {
            if (cronExpression == null)
            {
                throw new ArgumentNullException(nameof(cronExpression));
            }

            if (string.IsNullOrWhiteSpace(cronExpression))
            {
                var errorMessage = "CRON表达式不能为空或只包含空白字符";
                _logger?.LogError(errorMessage);
                throw new CronParseException(cronExpression, errorMessage);
            }

            try
            {
                var parts = cronExpression.Split(' ');
                
                // 处理秒级调度
                if (parts.Length == 6 && parts[0].StartsWith("*/"))
                {
                    try
                    {
                        var seconds = int.Parse(parts[0].Substring(2), CultureInfo.InvariantCulture);
                        if (seconds <= 0)
                        {
                            var errorMessage = $"秒级调度间隔必须大于0，当前值: {seconds}";
                            _logger?.LogError(errorMessage);
                            throw new CronParseException(cronExpression, errorMessage);
                        }
                        
                        _logger?.LogDebug("解析秒级CRON表达式成功: {CronExpression}, 间隔: {Seconds}秒", cronExpression, seconds);
                        return from.AddSeconds(seconds);
                    }
                    catch (FormatException ex)
                    {
                        var errorMessage = $"秒级调度间隔格式错误: {parts[0]}";
                        _logger?.LogError(ex, errorMessage);
                        throw new CronParseException(cronExpression, errorMessage, ex);
                    }
                    catch (OverflowException ex)
                    {
                        var errorMessage = $"秒级调度间隔值超出范围: {parts[0]}";
                        _logger?.LogError(ex, errorMessage);
                        throw new CronParseException(cronExpression, errorMessage, ex);
                    }
                }
                else
                {
                    // 处理标准 CRON 表达式
                    var standardExpression = parts.Length == 6
                        ? string.Join(" ", parts.Skip(1).Take(5))
                        : cronExpression;

                    try
                    {
                        var expression = CronExpression.Parse(standardExpression);
                        var next = expression.GetNextOccurrence(from.ToUniversalTime(), TimeZoneInfo.Utc);
                        
                        if (next == null)
                        {
                            var errorMessage = $"无法计算下一个执行时间，CRON表达式可能无效: {cronExpression}";
                            _logger?.LogWarning(errorMessage);
                            throw new CronParseException(cronExpression, errorMessage);
                        }

                        _logger?.LogDebug("解析标准CRON表达式成功: {CronExpression}, 下一个执行时间: {NextOccurrence}", 
                            cronExpression, next.Value);
                        return next.Value;
                    }
                    catch (CronFormatException ex)
                    {
                        var errorMessage = $"CRON表达式格式错误: {standardExpression}";
                        _logger?.LogError(ex, errorMessage);
                        throw new CronParseException(cronExpression, errorMessage, ex);
                    }
                }
            }
            catch (CronParseException)
            {
                // 重新抛出自定义异常
                throw;
            }
            catch (Exception ex)
            {
                // 捕获其他未预期的异常
                var errorMessage = $"解析CRON表达式时发生未预期的错误: {cronExpression}";
                _logger?.LogError(ex, errorMessage);
                throw new CronParseException(cronExpression, errorMessage, ex);
            }
        }

        /// <summary>
        /// 验证CRON表达式是否有效
        /// </summary>
        /// <param name="cronExpression">CRON表达式</param>
        /// <returns>是否有效</returns>
        public static bool IsValid(string cronExpression)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
            {
                return false;
            }

            try
            {
                var parts = cronExpression.Split(' ');
                
                // 验证秒级调度格式
                if (parts.Length == 6 && parts[0].StartsWith("*/"))
                {
                    if (!int.TryParse(parts[0].Substring(2), out var seconds) || seconds <= 0)
                    {
                        return false;
                    }
                    return true;
                }
                
                // 验证标准CRON表达式
                var standardExpression = parts.Length == 6
                    ? string.Join(" ", parts.Skip(1).Take(5))
                    : cronExpression;

                CronExpression.Parse(standardExpression);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}