using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.Tests.Core
{
    public class SchedulerTests
    {
        private readonly Mock<ITaskRepository> _mockRepo;
        private readonly Mock<ITaskExecutor> _mockExecutor;
        private readonly ScheduledTask _testTask;

        public SchedulerTests()
        {
            _mockRepo = new Mock<ITaskRepository>();
            _mockExecutor = new Mock<ITaskExecutor>();
            _testTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "测试任务",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };
        }

        [Fact]
        public async Task Start_EnabledTaskDueForExecution_ShouldExecuteTask()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllTasks())
                    .Returns(new[] { _testTask });
            
            _mockExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ScheduledTask>()))
                        .Returns(Task.CompletedTask);

            var scheduler = new Scheduler(_mockRepo.Object, _mockExecutor.Object);

            // Act
            scheduler.Start();
            await Task.Delay(2000); // 等待足够的时间让调度器运行
            scheduler.Stop();

            // Assert
            _mockExecutor.Verify(e => e.ExecuteAsync(It.Is<ScheduledTask>(t => t.Id == _testTask.Id)), 
                               Times.AtLeastOnce);
            _mockRepo.Verify(r => r.UpdateTask(It.Is<ScheduledTask>(t => t.Id == _testTask.Id)), 
                            Times.AtLeastOnce);
        }

        [Fact]
        public async Task Start_DisabledTask_ShouldNotExecuteTask()
        {
            // Arrange
            var disabledTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "禁用的任务",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = false
            };

            _mockRepo.Setup(r => r.GetAllTasks())
                    .Returns(new[] { disabledTask });

            var scheduler = new Scheduler(_mockRepo.Object, _mockExecutor.Object);

            // Act
            scheduler.Start();
            await Task.Delay(2000); // 等待足够的时间让调度器运行
            scheduler.Stop();

            // Assert
            _mockExecutor.Verify(e => e.ExecuteAsync(It.IsAny<ScheduledTask>()), Times.Never);
        }

        [Fact]
        public async Task Start_TaskExecutionFails_ShouldContinueRunning()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllTasks())
                    .Returns(new[] { _testTask });

            _mockExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ScheduledTask>()))
                        .ThrowsAsync(new Exception("测试异常"));

            var scheduler = new Scheduler(_mockRepo.Object, _mockExecutor.Object);

            // Act
            scheduler.Start();
            await Task.Delay(2000); // 等待足够的时间让调度器运行
            scheduler.Stop();

            // Assert
            // 验证调度器在任务执行失败后仍然继续运行
            _mockExecutor.Verify(e => e.ExecuteAsync(It.IsAny<ScheduledTask>()), 
                               Times.AtLeastOnce);
        }

        [Fact]
        public async Task Stop_ShouldStopExecutingTasks()
        {
            // Arrange
            int executionCount = 0;
            _mockRepo.Setup(r => r.GetAllTasks())
                    .Returns(new[] { _testTask });

            _mockExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ScheduledTask>()))
                        .Callback(() => executionCount++)
                        .Returns(Task.CompletedTask);

            var scheduler = new Scheduler(_mockRepo.Object, _mockExecutor.Object);

            // Act
            scheduler.Start();
            await Task.Delay(1000); // 让调度器运行一段时间
            scheduler.Stop();
            int countAfterStop = executionCount;
            await Task.Delay(1000); // 再等待一段时间

            // Assert
            Assert.Equal(countAfterStop, executionCount); // 验证停止后没有新的执行
        }

        [Fact]
        public async Task Start_MultipleTasksDue_ShouldExecuteAllTasks()
        {
            // Arrange
            var task1 = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "任务1",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            var task2 = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "任务2",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            _mockRepo.Setup(r => r.GetAllTasks())
                    .Returns(new[] { task1, task2 });

            _mockExecutor.Setup(e => e.ExecuteAsync(It.IsAny<ScheduledTask>()))
                        .Returns(Task.CompletedTask);

            var scheduler = new Scheduler(_mockRepo.Object, _mockExecutor.Object);

            // Act
            scheduler.Start();
            await Task.Delay(2000); // 等待足够的时间让调度器运行
            scheduler.Stop();

            // Assert
            _mockExecutor.Verify(e => e.ExecuteAsync(It.Is<ScheduledTask>(t => t.Id == task1.Id)), 
                               Times.AtLeastOnce);
            _mockExecutor.Verify(e => e.ExecuteAsync(It.Is<ScheduledTask>(t => t.Id == task2.Id)), 
                               Times.AtLeastOnce);
        }
    }
} 