using System;
using System.Threading.Tasks;
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;
using Moq;
using Xunit;

namespace MonkeyScheduler.Tests.Core
{
    public class SchedulerTests : IDisposable
    {
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly Mock<ITaskExecutor> _taskExecutorMock;
        private readonly Scheduler _scheduler;

        public SchedulerTests()
        {
            _taskRepositoryMock = new Mock<ITaskRepository>();
            _taskExecutorMock = new Mock<ITaskExecutor>();
            _scheduler = new Scheduler(_taskRepositoryMock.Object, _taskExecutorMock.Object);
        }

        [Fact]
        public async Task Start_EnabledTaskDueForExecution_ShouldExecuteTask()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "Test Task",
                Enabled = true,
                NextRunTime = DateTime.UtcNow.AddMinutes(-10),
                CronExpression = "*/5 * * * *"
            };

            _taskRepositoryMock.Setup(r => r.GetAllTasks())
                .Returns(new[] { task });

            _taskExecutorMock.Setup(e => e.ExecuteAsync(It.IsAny<ScheduledTask>(), null))
                .Returns(Task.CompletedTask);

            // Act
            _scheduler.Start();
            await Task.Delay(2000); // 等待任务执行

            // Assert
            _taskExecutorMock.Verify(e => e.ExecuteAsync(
                It.Is<ScheduledTask>(t => t.Id == task.Id),
                null), 
                Times.AtLeast(1));

            _taskRepositoryMock.Verify(r => r.UpdateTask(
                It.Is<ScheduledTask>(t => t.Id == task.Id)),
                Times.AtLeast(1));
        }

        [Fact]
        public async Task Start_DisabledTask_ShouldNotExecute()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "Disabled Task",
                Enabled = false,
                NextRunTime = DateTime.UtcNow.AddMinutes(-10),
                CronExpression = "*/5 * * * *"
            };

            _taskRepositoryMock.Setup(r => r.GetAllTasks())
                .Returns(new[] { task });

            // Act
            _scheduler.Start();
            await Task.Delay(2000);

            // Assert
            _taskExecutorMock.Verify(e => e.ExecuteAsync(
                It.IsAny<ScheduledTask>(),
                null), 
                Times.Never);
        }

        [Fact]
        public async Task Start_TaskNotDueForExecution_ShouldNotExecute()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "Future Task",
                Enabled = true,
                NextRunTime = DateTime.UtcNow.AddMinutes(10),
                CronExpression = "*/5 * * * *"
            };

            _taskRepositoryMock.Setup(r => r.GetAllTasks())
                .Returns(new[] { task });

            // Act
            _scheduler.Start();
            await Task.Delay(2000);

            // Assert
            _taskExecutorMock.Verify(e => e.ExecuteAsync(
                It.IsAny<ScheduledTask>(),
                null), 
                Times.Never);
        }

        public void Dispose()
        {
            _scheduler.Stop();
        }
    }
}