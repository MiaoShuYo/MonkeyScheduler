using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.Data.MySQL.Data;
using MonkeyScheduler.Data.MySQL.Models;
using MonkeyScheduler.Data.MySQL.Repositories;

namespace MonkeyScheduler.Data.MySQL.Tests
{
    [TestClass]
    public class LogRepositoryTests
    {
        private Mock<ILogger<MySqlDbContext>> _loggerMock;
        private Mock<IDbConnection> _connectionMock;
        private Mock<MySqlDbContext> _dbContextMock;
        private LogRepository _logRepository;
        private TestDbConnection _testConnection;
        private Mock<IDapperWrapper> _dapperWrapperMock;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<MySqlDbContext>>();
            _connectionMock = new Mock<IDbConnection>();
            _testConnection = new TestDbConnection(_connectionMock.Object);
            _dbContextMock =
                new Mock<MySqlDbContext>(MockBehavior.Loose, "dummy_connection_string", _loggerMock.Object);

            // 设置DbContext的Connection属性返回我们的测试连接
            _dbContextMock.Setup(x => x.Connection).Returns(_testConnection);

            _dapperWrapperMock = new Mock<IDapperWrapper>();
            _logRepository = new LogRepository(_dbContextMock.Object, _dapperWrapperMock.Object);
        }

        [TestMethod]
        public async Task AddLogAsync_WithValidLogEntry_ReturnsNewId()
        {
            // Arrange
            var logEntry = new LogEntry
            {
                Level = "Info",
                Message = "Test log message",
                Exception = null,
                Timestamp = DateTime.UtcNow,
                Source = "Test",
                Category = "TestCategory",
                EventId = "12345"
            };

            // 模拟 ExecuteScalarAsync 返回值为 1
            _dapperWrapperMock.Setup(x =>
                    x.ExecuteScalarAsync<int>(_testConnection, It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(1);

            // Act
            var result = await _logRepository.AddLogAsync(logEntry);

            // Assert
            Assert.IsTrue(result > 0, "The returned ID should be greater than 0.");
            _dapperWrapperMock.Verify(
                x => x.ExecuteScalarAsync<int>(_testConnection, It.IsAny<string>(), It.IsAny<object>(), null, null, null),
                Times.Once);
        }

        [TestMethod]
        public async Task GetLogsAsync_WithValidParameters_ReturnsLogs()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-1);
            var endTime = DateTime.UtcNow;
            var level = "Info";

            var expectedLogs = new List<LogEntry>
            {
                new LogEntry
                {
                    Id = 1,
                    Level = level,
                    Message = "Test log 1",
                    Timestamp = DateTime.UtcNow,
                    Source = "Test"
                }
            };

            // 模拟 IDapperWrapper 的 QueryAsync 方法
            _dapperWrapperMock.Setup(x =>
                    x.QueryAsync<LogEntry>(
                        It.IsAny<IDbConnection>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        null,
                        null,
                        null))
                .ReturnsAsync(expectedLogs);

            // Act
            var result = await _logRepository.GetLogsAsync(startTime, endTime, level);

            // Assert
            Assert.IsNotNull(result, "The result should not be null.");
            Assert.AreEqual(1, result.Count(), "The number of logs returned does not match the expected value.");
            _dapperWrapperMock.Verify(
                x => x.QueryAsync<LogEntry>(
                    It.IsAny<IDbConnection>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    null,
                    null,
                    null),
                Times.Once);
        }

        [TestMethod]
        public async Task GetLogsAsync_WithoutLevel_ReturnsAllLogs()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddDays(-1);
            var endTime = DateTime.UtcNow;

            var expectedLogs = new List<LogEntry>
            {
                new LogEntry
                {
                    Id = 1,
                    Level = "Info",
                    Message = "Test log 1",
                    Timestamp = DateTime.UtcNow,
                    Source = "Test"
                },
                new LogEntry
                {
                    Id = 2,
                    Level = "Error",
                    Message = "Test log 2",
                    Timestamp = DateTime.UtcNow,
                    Source = "Test"
                }
            };

            // 模拟 IDapperWrapper 的 QueryAsync 方法
            _dapperWrapperMock.Setup(x =>
                    x.QueryAsync<LogEntry>(_testConnection, It.IsAny<string>(), It.IsAny<object>(), null, null, null))
                .ReturnsAsync(expectedLogs);

            // Act
            var result = await _logRepository.GetLogsAsync(startTime, endTime);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            _dapperWrapperMock.Verify(
                x => x.QueryAsync<LogEntry>(_testConnection, It.IsAny<string>(), It.IsAny<object>(), null, null, null),
                Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddLogAsync_WithNullLogEntry_ThrowsArgumentNullException()
        {
            try
            {
                // Act
                await _logRepository.AddLogAsync(null);
            }
            catch (ArgumentNullException ex)
            {
                // Assert
                Assert.AreEqual("logEntry", ex.ParamName);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetLogsAsync_WithInvalidTimeRange_ThrowsArgumentException()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = DateTime.UtcNow.AddDays(-1); // 结束时间早于开始时间

            try
            {
                // Act
                await _logRepository.GetLogsAsync(startTime, endTime);
            }
            catch (ArgumentException ex)
            {
                // Assert
                Assert.AreEqual("End time must be greater than start time.", ex.Message);
                throw;
            }
        }
    }
}
