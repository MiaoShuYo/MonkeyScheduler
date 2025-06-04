using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService.Controllers;
using MonkeyScheduler.SchedulerService.Models;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.SchedulerService.Test.Controllers
{
    [TestClass]
    public class TasksControllerTests
    {
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<ITaskExecutionResult> _mockTaskExecutionResult;
        private TasksController _tasksController;
        private ScheduledTask _testTask;
        private Guid _testTaskId;

        [TestInitialize]
        public void Setup()
        {
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockTaskExecutionResult = new Mock<ITaskExecutionResult>(); // 初始化 _mockTaskExecutionResult
            _tasksController = new TasksController(_mockTaskRepository.Object, _mockTaskExecutionResult.Object);
    
            _testTaskId = Guid.NewGuid();
            _testTask = new ScheduledTask
            {
                Id = _testTaskId,
                Name = "Test Task",
                CronExpression = "0 * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };
        }

        [TestMethod]
        public void CreateTask_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new CreateTaskRequest
            {
                Name = "New Task",
                CronExpression = "0 * * * *"
            };

            // Act
            var result = _tasksController.CreateTask(request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            var task = okResult.Value as ScheduledTask;
            Assert.IsNotNull(task);
            Assert.AreEqual(request.Name, task.Name);
            Assert.AreEqual(request.CronExpression, task.CronExpression);
            Assert.IsTrue(task.Enabled);
            _mockTaskRepository.Verify(repo => repo.AddTask(It.IsAny<ScheduledTask>()), Times.Once);
        }

        [TestMethod]
        public void CreateTask_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = _tasksController.CreateTask(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.AreEqual("请求体不能为空", badRequestResult.Value);
        }

        [TestMethod]
        public void CreateTask_WithEmptyName_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateTaskRequest
            {
                Name = string.Empty,
                CronExpression = "0 * * * *"
            };

            // Act
            var result = _tasksController.CreateTask(request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.AreEqual("任务名称不能为空", badRequestResult.Value);
        }

        [TestMethod]
        public void CreateTask_WithEmptyCronExpression_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateTaskRequest
            {
                Name = "New Task",
                CronExpression = string.Empty
            };

            // Act
            var result = _tasksController.CreateTask(request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.AreEqual("Cron表达式不能为空", badRequestResult.Value);
        }

        [TestMethod]
        public void EnableTask_WithExistingTask_ReturnsOkResult()
        {
            // Arrange
            _mockTaskRepository.Setup(repo => repo.GetTask(_testTaskId)).Returns(_testTask);

            // Act
            var result = _tasksController.EnableTask(_testTaskId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkResult));
            Assert.IsTrue(_testTask.Enabled);
            _mockTaskRepository.Verify(repo => repo.UpdateTask(_testTask), Times.Once);
        }

        [TestMethod]
        public void EnableTask_WithNonExistingTask_ReturnsNotFound()
        {
            // Arrange
            _mockTaskRepository.Setup(repo => repo.GetTask(_testTaskId)).Returns((ScheduledTask)null);

            // Act
            var result = _tasksController.EnableTask(_testTaskId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundResult = result as NotFoundObjectResult;
            Assert.AreEqual($"任务 {_testTaskId} 不存在", notFoundResult.Value);
        }

        [TestMethod]
        public void DisableTask_WithExistingTask_ReturnsOkResult()
        {
            // Arrange
            _mockTaskRepository.Setup(repo => repo.GetTask(_testTaskId)).Returns(_testTask);

            // Act
            var result = _tasksController.DisableTask(_testTaskId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkResult));
            Assert.IsFalse(_testTask.Enabled);
            _mockTaskRepository.Verify(repo => repo.UpdateTask(_testTask), Times.Once);
        }

        [TestMethod]
        public void DisableTask_WithNonExistingTask_ReturnsNotFound()
        {
            // Arrange
            _mockTaskRepository.Setup(repo => repo.GetTask(_testTaskId)).Returns((ScheduledTask)null);

            // Act
            var result = _tasksController.DisableTask(_testTaskId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundResult = result as NotFoundObjectResult;
            Assert.AreEqual($"任务 {_testTaskId} 不存在", notFoundResult.Value);
        }

        [TestMethod]
        public void GetTasks_ReturnsAllTasks()
        {
            // Arrange
            var tasks = new List<ScheduledTask> { _testTask };
            _mockTaskRepository.Setup(repo => repo.GetAllTasks()).Returns(tasks);

            // Act
            var result = _tasksController.GetTasks();

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            var returnedTasks = okResult.Value as List<ScheduledTask>;
            Assert.IsNotNull(returnedTasks);
            Assert.AreEqual(1, returnedTasks.Count);
            Assert.AreEqual(_testTask.Id, returnedTasks[0].Id);
        }

        [TestMethod]
        public void GetTask_WithExistingTask_ReturnsTask()
        {
            // Arrange
            _mockTaskRepository.Setup(repo => repo.GetTask(_testTaskId)).Returns(_testTask);

            // Act
            var result = _tasksController.GetTask(_testTaskId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            var task = okResult.Value as ScheduledTask;
            Assert.IsNotNull(task);
            Assert.AreEqual(_testTask.Id, task.Id);
        }

        [TestMethod]
        public void GetTask_WithNonExistingTask_ReturnsNotFound()
        {
            // Arrange
            _mockTaskRepository.Setup(repo => repo.GetTask(_testTaskId)).Returns((ScheduledTask)null);

            // Act
            var result = _tasksController.GetTask(_testTaskId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundResult = result as NotFoundObjectResult;
            Assert.AreEqual($"任务 {_testTaskId} 不存在", notFoundResult.Value);
        }

        [TestMethod]
        public void DeleteTask_WithExistingTask_ReturnsOkResult()
        {
            // Arrange
            _mockTaskRepository.Setup(repo => repo.GetTask(_testTaskId)).Returns(_testTask);

            // Act
            var result = _tasksController.DeleteTask(_testTaskId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkResult));
            _mockTaskRepository.Verify(repo => repo.DeleteTask(_testTaskId), Times.Once);
        }

        [TestMethod]
        public void DeleteTask_WithNonExistingTask_ReturnsNotFound()
        {
            // Arrange
            _mockTaskRepository.Setup(repo => repo.GetTask(_testTaskId)).Returns((ScheduledTask)null);

            // Act
            var result = _tasksController.DeleteTask(_testTaskId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
            var notFoundResult = result as NotFoundObjectResult;
            Assert.AreEqual($"任务 {_testTaskId} 不存在", notFoundResult.Value);
        }

        [TestMethod]
        public void ReportTaskStatus_WithValidResult_ReturnsOkResult()
        {
            // Arrange
            var result = new TaskExecutionResult
            {
                TaskId = _testTaskId,
                Status = ExecutionStatus.Completed,
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                EndTime = DateTime.UtcNow,
                WorkerNodeUrl = "http://worker:5000",
                Success = true
            };

            // Act
            var response = _tasksController.ReportTaskStatus(result);

            // Assert
            Assert.IsInstanceOfType(response, typeof(OkResult));
        }

        [TestMethod]
        public void ReportTaskStatus_WithNullResult_ReturnsBadRequest()
        {
            // Act
            var result = _tasksController.ReportTaskStatus(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            var badRequestResult = result as BadRequestObjectResult;
            Assert.AreEqual("请求体不能为空", badRequestResult.Value);
        }
    }
} 