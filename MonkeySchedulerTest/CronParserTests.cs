using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using System;

namespace MonkeySchedulerTest
{
    [TestClass]
    public class CronParserTests
    {
        private ILogger _logger;

        [TestInitialize]
        public void Setup()
        {
            // 创建一个简单的测试日志记录器
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<CronParserTests>();
            CronParser.SetLogger(_logger);
        }

        [TestMethod]
        public void GetNextOccurrence_WithStandardCronExpression_ReturnsCorrectNextTime()
        {
            // Arrange
            var cronExpression = "0 0 * * *"; // 每天午夜
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            var result = CronParser.GetNextOccurrence(cronExpression, from);

            // Assert
            Assert.AreEqual(new DateTime(2024, 1, 2, 0, 0, 0), result);
        }

        [TestMethod]
        public void GetNextOccurrence_WithSecondLevelExpression_ReturnsCorrectNextTime()
        {
            // Arrange
            var cronExpression = "*/30 * * * * *"; // 每30秒
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            var result = CronParser.GetNextOccurrence(cronExpression, from);

            // Assert
            Assert.AreEqual(from.AddSeconds(30), result);
        }

        [TestMethod]
        [ExpectedException(typeof(CronParseException))]
        public void GetNextOccurrence_WithInvalidCronExpression_ThrowsCronParseException()
        {
            // Arrange
            var invalidCronExpression = "invalid expression";
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            CronParser.GetNextOccurrence(invalidCronExpression, from);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetNextOccurrence_WithNullCronExpression_ThrowsArgumentNullException()
        {
            // Arrange
            string nullCronExpression = null;
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            CronParser.GetNextOccurrence(nullCronExpression, from);
        }

        [TestMethod]
        [ExpectedException(typeof(CronParseException))]
        public void GetNextOccurrence_WithEmptyCronExpression_ThrowsCronParseException()
        {
            // Arrange
            var emptyCronExpression = "";
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            CronParser.GetNextOccurrence(emptyCronExpression, from);
        }

        [TestMethod]
        [ExpectedException(typeof(CronParseException))]
        public void GetNextOccurrence_WithWhitespaceCronExpression_ThrowsCronParseException()
        {
            // Arrange
            var whitespaceCronExpression = "   ";
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            CronParser.GetNextOccurrence(whitespaceCronExpression, from);
        }

        [TestMethod]
        [ExpectedException(typeof(CronParseException))]
        public void GetNextOccurrence_WithInvalidSecondLevelExpression_ThrowsCronParseException()
        {
            // Arrange
            var invalidSecondLevelExpression = "*/0 * * * * *"; // 间隔为0
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            CronParser.GetNextOccurrence(invalidSecondLevelExpression, from);
        }

        [TestMethod]
        [ExpectedException(typeof(CronParseException))]
        public void GetNextOccurrence_WithInvalidSecondLevelFormat_ThrowsCronParseException()
        {
            // Arrange
            var invalidSecondLevelFormat = "*/abc * * * * *"; // 非数字间隔
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            CronParser.GetNextOccurrence(invalidSecondLevelFormat, from);
        }

        [TestMethod]
        public void GetNextOccurrence_WithSixPartCronExpression_HandlesCorrectly()
        {
            // Arrange
            var cronExpression = "0 0 0 * * *"; // 每天午夜（6部分格式）
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            var result = CronParser.GetNextOccurrence(cronExpression, from);

            // Assert
            Assert.AreEqual(new DateTime(2024, 1, 2, 0, 0, 0), result);
        }

        [TestMethod]
        public void IsValid_WithValidStandardCronExpression_ReturnsTrue()
        {
            // Arrange
            var validCronExpression = "0 0 * * *";

            // Act
            var result = CronParser.IsValid(validCronExpression);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsValid_WithValidSecondLevelExpression_ReturnsTrue()
        {
            // Arrange
            var validSecondLevelExpression = "*/30 * * * * *";

            // Act
            var result = CronParser.IsValid(validSecondLevelExpression);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsValid_WithInvalidCronExpression_ReturnsFalse()
        {
            // Arrange
            var invalidCronExpression = "invalid expression";

            // Act
            var result = CronParser.IsValid(invalidCronExpression);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValid_WithNullCronExpression_ReturnsFalse()
        {
            // Arrange
            string nullCronExpression = null;

            // Act
            var result = CronParser.IsValid(nullCronExpression);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValid_WithEmptyCronExpression_ReturnsFalse()
        {
            // Arrange
            var emptyCronExpression = "";

            // Act
            var result = CronParser.IsValid(emptyCronExpression);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValid_WithInvalidSecondLevelExpression_ReturnsFalse()
        {
            // Arrange
            var invalidSecondLevelExpression = "*/0 * * * * *";

            // Act
            var result = CronParser.IsValid(invalidSecondLevelExpression);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsValid_WithInvalidSecondLevelFormat_ReturnsFalse()
        {
            // Arrange
            var invalidSecondLevelFormat = "*/abc * * * * *";

            // Act
            var result = CronParser.IsValid(invalidSecondLevelFormat);

            // Assert
            Assert.IsFalse(result);
        }
    }
} 