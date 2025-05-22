using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Logging;
using System;

namespace MonkeySchedulerTest
{
    [TestClass]
    public class DefaultLogFormatterTests
    {
        [TestMethod]
        public void Format_WithDefaultFormat_ReturnsCorrectFormat()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var level = "INFO";
            var message = "Test message";

            // Act
            var result = formatter.Format(level, message);

            // Assert
            // 验证时间戳格式是否正确,但不比较具体时间值
            Assert.IsTrue(result.Contains(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));
            Assert.IsTrue(result.Contains($"[{level}]"));
            Assert.IsTrue(result.Contains(message));
        }

        [TestMethod]
        public void Format_WithCustomFormat_ReturnsCorrectFormat()
        {
            // Arrange
            var customFormat = "[{level}] - {message}";
            var formatter = new DefaultLogFormatter(customFormat, includeTimestamp: false);
            var level = "ERROR";
            var message = "Custom format test";

            // Act
            var result = formatter.Format(level, message);

            // Assert
            Assert.AreEqual($"[{level}] - {message}", result);
        }

        [TestMethod]
        public void Format_WithException_IncludesExceptionDetails()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var level = "ERROR";
            var message = "Exception test";
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = formatter.Format(level, message, exception);

            // Assert
            Assert.IsTrue(result.Contains("Exception:"));
            Assert.IsTrue(result.Contains(exception.GetType().Name));
            Assert.IsTrue(result.Contains(exception.Message));
        }

        [TestMethod]
        public void Format_WithExceptionButExcludeException_DoesNotIncludeException()
        {
            // Arrange
            var formatter = new DefaultLogFormatter(includeException: false);
            var level = "ERROR";
            var message = "Exception test";
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = formatter.Format(level, message, exception);

            // Assert
            Assert.IsFalse(result.Contains("Exception:"));
            Assert.IsFalse(result.Contains(exception.GetType().Name));
        }

        [TestMethod]
        public void Format_WithNullException_ReturnsFormattedMessage()
        {
            // Arrange
            var formatter = new DefaultLogFormatter();
            var level = "INFO";
            var message = "Null exception test";

            // Act
            var result = formatter.Format(level, message, null);

            // Assert
            Assert.IsFalse(result.Contains("Exception:"));
        }
    }
} 