using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.WorkerService.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.WorkerService.Tests.Services
{
    [TestClass]
    public class DefaultTaskExecutorTests
    {
        private Mock<IStatusReporterService> _statusReporterMock;
        private Mock<ILogger<DefaultTaskExecutor>> _loggerMock;
        private DefaultTaskExecutor _executor;
        private DefaultTaskExecutorOptions _options;

        [TestInitialize]
        public void Initialize()
        {
            _statusReporterMock = new Mock<IStatusReporterService>();
            _loggerMock = new Mock<ILogger<DefaultTaskExecutor>>();
            _options = new DefaultTaskExecutorOptions
            {
                SchedulerUrl = "http://test-scheduler"
            };

            // 手动创建 IOptions<DefaultTaskExecutorOptions> 实例
            var optionsMock = Mock.Of<IOptions<DefaultTaskExecutorOptions>>(opt => opt.Value == _options);

            // 初始化 _executor
            _executor = new DefaultTaskExecutor(
                _statusReporterMock.Object,
                optionsMock,
                _loggerMock.Object
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_SuccessfulExecution_CallsCallbackWithSuccess()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask"
            };

            TaskExecutionResult? callbackResult = null;
            Func<TaskExecutionResult, Task> callback = result =>
            {
                callbackResult = result;
                return Task.CompletedTask;
            };

            // Act
            await _executor.ExecuteAsync(task, callback);

            // Assert
            Assert.IsNotNull(callbackResult);
            Assert.AreEqual(task.Id, callbackResult.TaskId);
            Assert.AreEqual(ExecutionStatus.Completed, callbackResult.Status);
            Assert.IsTrue(callbackResult.Success);
            Assert.IsNull(callbackResult.ErrorMessage);

            _statusReporterMock.Verify(s => s.ReportStatusAsync(It.Is<TaskExecutionResult>(r => r.TaskId == task.Id && r.Success)), Times.Once());
        }

        [TestMethod]
        public async Task ExecuteAsync_NoCallback_DoesNotThrow()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask"
            };

            // Act & Assert
            await _executor.ExecuteAsync(task);
            _statusReporterMock.Verify(s => s.ReportStatusAsync(It.Is<TaskExecutionResult>(r => r.TaskId == task.Id && r.Success)), Times.Once());
        }
    }
} 
