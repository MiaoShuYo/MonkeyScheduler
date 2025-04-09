using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MonkeyScheduler.Logging;

namespace MonkeyScheduler.Tests.Logging
{
    public class LoggerTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly Mock<ILogFormatter> _mockFormatter;
        private readonly Logger _logger;

        public LoggerTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_logs_{Guid.NewGuid()}.db");
            _mockFormatter = new Mock<ILogFormatter>();
            _mockFormatter.Setup(f => f.Format(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()))
                         .Returns((string level, string message, Exception ex) => $"{level}: {message}");

            _logger = new Logger(
                dbPath: _testDbPath,
                maxLogCount: 100,
                maxLogAge: TimeSpan.FromDays(1),
                formatter: _mockFormatter.Object
            );
        }

        public void Dispose()
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }

        [Fact]
        public async Task LogAsync_ValidMessage_ShouldCreateLogEntry()
        {
            // Arrange
            var level = "INFO";
            var message = "测试消息";

            // Act
            await _logger.LogAsync(level, message);

            // Assert
            var count = await _logger.GetLogCountAsync();
            Assert.Equal(1, count);
            _mockFormatter.Verify(f => f.Format(level, message, null), Times.Once);
        }

        [Fact]
        public async Task LogAsync_WithException_ShouldCreateLogEntry()
        {
            // Arrange
            var level = "ERROR";
            var message = "错误消息";
            var exception = new Exception("测试异常");

            // Act
            await _logger.LogAsync(level, message, exception);

            // Assert
            var count = await _logger.GetLogCountAsync();
            Assert.Equal(1, count);
            _mockFormatter.Verify(f => f.Format(level, message, exception), Times.Once);
        }

        [Fact]
        public async Task LogInfoAsync_ShouldCreateInfoLogEntry()
        {
            // Arrange
            var message = "信息消息";

            // Act
            await _logger.LogInfoAsync(message);

            // Assert
            _mockFormatter.Verify(f => f.Format("INFO", message, null), Times.Once);
        }

        [Fact]
        public async Task LogWarningAsync_ShouldCreateWarningLogEntry()
        {
            // Arrange
            var message = "警告消息";

            // Act
            await _logger.LogWarningAsync(message);

            // Assert
            _mockFormatter.Verify(f => f.Format("WARNING", message, null), Times.Once);
        }

        [Fact]
        public async Task LogErrorAsync_ShouldCreateErrorLogEntry()
        {
            // Arrange
            var message = "错误消息";
            var exception = new Exception("测试异常");

            // Act
            await _logger.LogErrorAsync(message, exception);

            // Assert
            _mockFormatter.Verify(f => f.Format("ERROR", message, exception), Times.Once);
        }

        [Fact]
        public async Task CleanupLogsAsync_ShouldRemoveOldLogs()
        {
            // Arrange
            for (int i = 0; i < 150; i++) // 超过maxLogCount(100)的日志数量
            {
                await _logger.LogInfoAsync($"测试消息 {i}");
            }

            // Act
            await _logger.CleanupLogsAsync();

            // Assert
            var count = await _logger.GetLogCountAsync();
            Assert.True(count <= 100); // 验证日志数量已被清理到最大值以下
        }

        [Fact]
        public async Task GetOldestLogDateAsync_NoLogs_ShouldReturnNull()
        {
            // Act
            var oldestDate = await _logger.GetOldestLogDateAsync();

            // Assert
            Assert.Null(oldestDate);
        }

        [Fact]
        public async Task GetOldestLogDateAsync_WithLogs_ShouldReturnDate()
        {
            // Arrange
            var beforeInsert = DateTime.UtcNow;
            await _logger.LogInfoAsync("测试消息");
            var afterInsert = DateTime.UtcNow;

            // Act
            var oldestDate = await _logger.GetOldestLogDateAsync();

            // Assert
            Assert.NotNull(oldestDate);
            Assert.True(oldestDate >= beforeInsert.AddSeconds(-1) && oldestDate <= afterInsert.AddSeconds(1));
        }
    }
} 