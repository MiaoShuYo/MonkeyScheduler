using System;
using Xunit;
using MonkeyScheduler.Core;

namespace MonkeyScheduler.Tests.Core
{
    public class CronParserTests
    {
        [Theory]
        [InlineData("*/10 * * * * *", 10)] // 每10秒
        [InlineData("*/30 * * * * *", 30)] // 每30秒
        [InlineData("*/60 * * * * *", 60)] // 每60秒
        public void GetNextOccurrence_SecondsInterval_ReturnsCorrectNextTime(string cronExpression, int expectedSeconds)
        {
            // Arrange
            var now = DateTime.UtcNow;

            // Act
            var next = CronParser.GetNextOccurrence(cronExpression, now);

            // Assert
            Assert.Equal(expectedSeconds, (next - now).TotalSeconds, 1); // 允许1秒的误差
        }

        [Theory]
        [InlineData("0 */5 * * * *")] // 每5分钟
        [InlineData("0 0 * * * *")] // 每小时整点
        [InlineData("0 0 0 * * *")] // 每天午夜
        public void GetNextOccurrence_StandardCron_ReturnsValidDateTime(string cronExpression)
        {
            // Arrange
            var now = DateTime.UtcNow;

            // Act
            var next = CronParser.GetNextOccurrence(cronExpression, now);

            // Assert
            Assert.True(next > now);
        }

        [Theory]
        [InlineData("invalid cron")]
        [InlineData("* * * *")] // 缺少字段
        [InlineData("* * * * * * *")] // 多余字段
        public void GetNextOccurrence_InvalidCron_ReturnsOneMinuteLater(string cronExpression)
        {
            // Arrange
            var now = DateTime.UtcNow;

            // Act
            var next = CronParser.GetNextOccurrence(cronExpression, now);

            // Assert
            Assert.Equal(60, (next - now).TotalSeconds, 1); // 默认返回1分钟后，允许1秒的误差
        }

        [Fact]
        public void GetNextOccurrence_NullCronExpression_ThrowsArgumentNullException()
        {
            // Arrange
            string? cronExpression = null;
            var now = DateTime.UtcNow;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CronParser.GetNextOccurrence(cronExpression!, now));
        }
    }
} 