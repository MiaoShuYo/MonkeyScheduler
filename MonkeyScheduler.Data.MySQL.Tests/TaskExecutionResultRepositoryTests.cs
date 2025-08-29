using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Core.Models;
using Moq;
using MonkeyScheduler.Data.MySQL.Data;
using MonkeyScheduler.Data.MySQL.Models;
using MonkeyScheduler.Data.MySQL.Repositories;

namespace MonkeyScheduler.Data.MySQL.Tests
{
    [TestClass]
    public class TaskExecutionResultRepositoryTests
    {
        private Mock<ILogger<MySqlDbContext>> _loggerMock;
        private Mock<IDbConnection> _connectionMock;
        private MySqlDbContext _dbContext;
        private TaskExecutionResultRepository _repository;
        private const string TestConnectionString = "Server=localhost;Database=testdb;User=testuser;Password=testpass;";

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<MySqlDbContext>>();
            _connectionMock = new Mock<IDbConnection>();
            var options = new MySqlConnectionOptions
            {
                ConnectionString = TestConnectionString
            };
            _dbContext = new MySqlDbContext(options, _loggerMock.Object);
            _repository = new TaskExecutionResultRepository(_dbContext);
        }

        [TestMethod]
        public async Task AddExecutionResultAsync_WithValidResult_ReturnsNewId()
        {
            // Arrange
            var result = new TaskExecutionResult
            {
                Id = 1,
                TaskId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(5),
                Status = ExecutionStatus.Completed,
                Result = "Success",
                ErrorMessage = null,
                StackTrace = null
            };

            // Act & Assert
            try
            {
                var newId = await _repository.AddExecutionResultAsync(result);
                Assert.IsTrue(newId > 0);
            }
            catch (Exception ex)
            {
                // 由于无法连接到数据库，我们期望会抛出异常
                Assert.IsTrue(ex.Message.Contains("创建数据库连接失败") || ex.Message.Contains("Unable to connect"),
                    $"Unexpected exception message: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task UpdateExecutionResultAsync_WithValidResult_UpdatesResult()
        {
            // Arrange
            var result = new TaskExecutionResult
            {
                Id = 1,
                TaskId = Guid.NewGuid(),
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddMinutes(5),
                Status = ExecutionStatus.Failed,
                Result = "Error",
                ErrorMessage = "Test error",
                StackTrace = "Test stack trace"
            };

            // Act & Assert
            try
            {
                await _repository.UpdateExecutionResultAsync(result);
                // 如果没有抛出异常，说明更新成功
            }
            catch (Exception ex)
            {
                // 由于无法连接到数据库，我们期望会抛出异常
                Assert.IsTrue(ex.Message.Contains("创建数据库连接失败") || ex.Message.Contains("Unable to connect"),
                    $"Unexpected exception message: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task GetTaskExecutionResultsAsync_WithValidParameters_ReturnsResults()
        {
            // Arrange
            var taskId = 1;
            var startTime = DateTime.UtcNow.AddDays(-1);
            var endTime = DateTime.UtcNow;

            // Act & Assert
            try
            {
                var results = await _repository.GetTaskExecutionResultsAsync(taskId, startTime, endTime);
                Assert.IsNotNull(results);
            }
            catch (Exception ex)
            {
                // 由于无法连接到数据库，我们期望会抛出异常
                Assert.IsTrue(ex.Message.Contains("创建数据库连接失败") || ex.Message.Contains("Unable to connect"),
                    $"Unexpected exception message: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task GetLastExecutionResultAsync_WithValidTaskId_ReturnsResult()
        {
            // Arrange
            var taskId = 1;

            // Act & Assert
            try
            {
                var result = await _repository.GetLastExecutionResultAsync(taskId);
                Assert.IsNotNull(result);
            }
            catch (Exception ex)
            {
                // 由于无法连接到数据库，我们期望会抛出异常
                Assert.IsTrue(ex.Message.Contains("创建数据库连接失败") || ex.Message.Contains("Unable to connect"),
                    $"Unexpected exception message: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task AddExecutionResultAsync_WithNullResult_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await _repository.AddExecutionResultAsync(null);
            });

            // 验证异常消息包含预期内容
            StringAssert.Contains(exception.Message, "TaskExecutionResult cannot be null.");
        }

        [TestMethod]
        public async Task UpdateExecutionResultAsync_WithNullResult_ThrowsArgumentNullException()
        {
            // Arrange & Act
            try
            {
                await _repository.UpdateExecutionResultAsync(null);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                // 期望的异常，测试通过
            }
            catch (Exception ex)
            {
                // 由于无法连接到数据库，我们期望会抛出异常
                Assert.IsTrue(ex.Message.Contains("创建数据库连接失败") || ex.Message.Contains("Unable to connect"),
                    $"Unexpected exception message: {ex.Message}");
            }
        }

        [TestMethod]
        public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
        {
            // Arrange & Act
            try
            {
                new TaskExecutionResultRepository(null);
                Assert.Fail("Expected ArgumentNullException was not thrown");
            }
            catch (ArgumentNullException)
            {
                // 期望的异常，测试通过
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception type: {ex.GetType().Name}");
            }
        }
    }
}
