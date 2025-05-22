using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.WorkerService.Controllers;
using MonkeyScheduler.WorkerService.Services;

namespace MonkeyScheduler.WorkerService.Tests.Controllers
{
    [TestClass]
    public class TaskReceiverControllerTests
    {
        private Mock<ITaskExecutor> _executorMock;
        private Mock<IStatusReporterService> _statusReporterMock;
        private Mock<ILogger<TaskReceiverController>> _loggerMock;
        private TaskReceiverController _controller;

        [TestInitialize]
        public void Initialize()
        {
            _executorMock = new Mock<ITaskExecutor>();
            _statusReporterMock = new Mock<IStatusReporterService>();
            _loggerMock = new Mock<ILogger<TaskReceiverController>>();
            _controller = new TaskReceiverController(
                _executorMock.Object,
                _statusReporterMock.Object,
                _loggerMock.Object);
        }

        [TestMethod]
        public async Task Execute_ValidTask_ReturnsOk()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask"
            };

            _executorMock.Setup(x => x.ExecuteAsync(It.IsAny<ScheduledTask>(), It.IsAny<Func<TaskExecutionResult, Task>>()))
                .Callback<ScheduledTask, Func<TaskExecutionResult, Task>>((t, callback) =>
                {
                    callback(new TaskExecutionResult
                    {
                        TaskId = task.Id,
                        Status = ExecutionStatus.Completed,
                        StartTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow
                    });
                });

            // Act
            var result = await _controller.Execute(task);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkResult));
            _executorMock.Verify(x => x.ExecuteAsync(It.IsAny<ScheduledTask>(), It.IsAny<Func<TaskExecutionResult, Task>>()), Times.Once);
            _statusReporterMock.Verify(x => x.ReportStatusAsync(It.IsAny<TaskExecutionResult>()), Times.Once);
        }

        [TestMethod]
        public async Task Execute_ExecutionFails_ReturnsInternalServerError()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask"
            };
            var exception = new Exception("Test exception");

            _executorMock.Setup(x => x.ExecuteAsync(It.IsAny<ScheduledTask>(), It.IsAny<Func<TaskExecutionResult, Task>>()))
                .Throws(exception);

            // Act
            var result = await _controller.Execute(task);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ObjectResult));
            var objectResult = (ObjectResult)result;
            Assert.AreEqual(500, objectResult.StatusCode);
            _statusReporterMock.Verify(x => x.ReportStatusAsync(It.Is<TaskExecutionResult>(r => 
                r.Status == ExecutionStatus.Failed && 
                r.ErrorMessage == exception.Message)), Times.Once);
        }

        [TestMethod]
        public async Task Execute_ValidatesTaskModel()
        {
            // Arrange
            ScheduledTask task = null;

            // Act
            var result = await _controller.Execute(task);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = (BadRequestObjectResult)result;
            Assert.AreEqual("任务不能为空", badRequestResult.Value);
        }
    }
} 