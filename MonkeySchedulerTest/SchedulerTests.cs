using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;

namespace MonkeySchedulerTest
{
    [TestClass]
    public class SchedulerTests
    {
        private Mock<ITaskRepository> _mockRepo;
        private Mock<ITaskDispatcher> _mockDispatcher;
        private Mock<ILogger<Scheduler>> _mockLogger;
        private Scheduler _scheduler;
        private List<ScheduledTask> _tasks;

        [TestInitialize]
        public void Initialize()
        {
            _mockRepo = new Mock<ITaskRepository>();
            _mockDispatcher = new Mock<ITaskDispatcher>();
            _mockLogger = new Mock<ILogger<Scheduler>>();
            _tasks = new List<ScheduledTask>();

            // 设置仓储行为
            _mockRepo.Setup(r => r.GetAllTasks()).Returns(_tasks);
            _mockRepo.Setup(r => r.GetAllTasksAsync()).ReturnsAsync(_tasks);
            _mockRepo.Setup(r => r.UpdateTask(It.IsAny<ScheduledTask>()))
                .Callback<ScheduledTask>(task =>
                {
                    var existingTask = _tasks.FirstOrDefault(t => t.Id == task.Id);
                    if (existingTask != null)
                    {
                        existingTask.NextRunTime = task.NextRunTime;
                    }
                });
            _mockRepo.Setup(r => r.UpdateTaskAsync(It.IsAny<ScheduledTask>()))
                .Callback<ScheduledTask>(task =>
                {
                    var existingTask = _tasks.FirstOrDefault(t => t.Id == task.Id);
                    if (existingTask != null)
                    {
                        existingTask.NextRunTime = task.NextRunTime;
                    }
                })
                .Returns(Task.CompletedTask);

            // 设置分发器行为
            _mockDispatcher.Setup(d => d.DispatchTaskAsync(It.IsAny<ScheduledTask>(), It.IsAny<Func<TaskExecutionResult, Task>>()))
                .Returns(Task.CompletedTask);

            // 创建调度器实例
            var mockDagDependencyChecker = new Mock<IDagDependencyChecker>();
            var mockDagExecutionManager = new Mock<IDagExecutionManager>();
            _scheduler = new Scheduler(_mockRepo.Object, _mockDispatcher.Object, mockDagDependencyChecker.Object, mockDagExecutionManager.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task Start_ShouldExecuteDueTasks()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var dueTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "到期任务",
                CronExpression = "* * * * *",
                NextRunTime = now.AddSeconds(-1),
                Enabled = true
            };
            _tasks.Add(dueTask);

            // Act
            _scheduler.Start();
            await Task.Delay(1500); // 等待调度器运行一个周期
            _scheduler.Stop();

            // Assert
            _mockDispatcher.Verify(
                d => d.DispatchTaskAsync(
                    It.Is<ScheduledTask>(t => t.Id == dueTask.Id),
                    It.IsAny<Func<TaskExecutionResult, Task>>()),
                Times.AtLeastOnce());
            _mockRepo.Verify(
                r => r.UpdateTaskAsync(
                    It.Is<ScheduledTask>(t => t.Id == dueTask.Id && t.NextRunTime > now)),
                Times.AtLeastOnce());
        }

        [TestMethod]
        public async Task Start_ShouldNotExecuteNotDueTasks()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var notDueTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "未到期任务",
                CronExpression = "* * * * *",
                NextRunTime = now.AddMinutes(5),
                Enabled = true
            };
            _tasks.Add(notDueTask);

            // Act
            _scheduler.Start();
            await Task.Delay(1500);
            _scheduler.Stop();

            // Assert
            _mockDispatcher.Verify(
                d => d.DispatchTaskAsync(
                    It.Is<ScheduledTask>(t => t.Id == notDueTask.Id),
                    It.IsAny<Func<TaskExecutionResult, Task>>()),
                Times.Never());
        }

        [TestMethod]
        public async Task Start_ShouldNotExecuteDisabledTasks()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var disabledTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "禁用任务",
                CronExpression = "* * * * *",
                NextRunTime = now.AddSeconds(-1),
                Enabled = false
            };
            _tasks.Add(disabledTask);

            // Act
            _scheduler.Start();
            await Task.Delay(1500);
            _scheduler.Stop();

            // Assert
            _mockDispatcher.Verify(
                d => d.DispatchTaskAsync(
                    It.Is<ScheduledTask>(t => t.Id == disabledTask.Id),
                    It.IsAny<Func<TaskExecutionResult, Task>>()),
                Times.Never());
        }

        [TestMethod]
        public async Task Start_ShouldHandleDispatcherException()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "异常任务",
                CronExpression = "* * * * *",
                NextRunTime = now.AddSeconds(-1),
                Enabled = true
            };
            _tasks.Add(task);

            _mockDispatcher.Setup(d => d.DispatchTaskAsync(It.IsAny<ScheduledTask>(), It.IsAny<Func<TaskExecutionResult, Task>>()))
                .ThrowsAsync(new Exception("模拟分发异常"));

            // Act
            _scheduler.Start();
            await Task.Delay(1500);
            _scheduler.Stop();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("执行任务")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce());
        }

        [TestMethod]
        public async Task Start_ShouldHandleRepositoryException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllTasksAsync())
                .ThrowsAsync(new Exception("模拟仓储异常"));

            // Act
            _scheduler.Start();
            await Task.Delay(1500);
            _scheduler.Stop();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("调度器发生错误")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce());
        }

        [TestMethod]
        public async Task Stop_ShouldStopScheduler()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "测试任务",
                CronExpression = "* * * * *",
                NextRunTime = DateTime.UtcNow.AddSeconds(-1),
                Enabled = true
            };
            _tasks.Add(task);

            // Act
            _scheduler.Start();
            _scheduler.Stop();
            await Task.Delay(1500);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("调度器已停止")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce());
        }

        [TestMethod]
        public async Task MultipleTasks_ShouldBeExecutedInOrder()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var task1 = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "任务1",
                CronExpression = "* * * * *",
                NextRunTime = now.AddSeconds(-1),
                Enabled = true
            };
            var task2 = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "任务2",
                CronExpression = "* * * * *",
                NextRunTime = now.AddSeconds(-1),
                Enabled = true
            };
            _tasks.AddRange(new[] { task1, task2 });

            var executionOrder = new List<Guid>();
            _mockDispatcher.Setup(d => d.DispatchTaskAsync(It.IsAny<ScheduledTask>(), It.IsAny<Func<TaskExecutionResult, Task>>()))
                .Callback<ScheduledTask, Func<TaskExecutionResult, Task>>((t, _) => executionOrder.Add(t.Id))
                .Returns(Task.CompletedTask);

            // Act
            _scheduler.Start();
            await Task.Delay(1500); // 增加等待时间，确保调度器有足够时间执行
            _scheduler.Stop();

            // Assert
            Assert.AreEqual(2, executionOrder.Count, "应该只执行2个任务");
            Assert.AreEqual(task1.Id, executionOrder[0], "任务1应该先执行");
            Assert.AreEqual(task2.Id, executionOrder[1], "任务2应该后执行");
        }
    }
} 