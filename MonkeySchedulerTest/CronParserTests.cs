using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Core;
using System;

namespace MonkeySchedulerTest
{
    [TestClass]
    public class CronParserTests
    {
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
        public void GetNextOccurrence_WithInvalidCronExpression_ReturnsDefaultValue()
        {
            // Arrange
            var invalidCronExpression = "invalid expression";
            var from = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            var result = CronParser.GetNextOccurrence(invalidCronExpression, from);

            // Assert
            Assert.AreEqual(from.AddMinutes(1), result);
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
    }
} 