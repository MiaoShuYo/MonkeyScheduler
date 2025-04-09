using System;
using System.Linq;
using Xunit;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.Tests.Storage
{
    public class InMemoryTaskRepositoryTests
    {
        [Fact]
        public void AddTask_NewTask_ShouldBeAddedToRepository()
        {
            // Arrange
            var repo = new InMemoryTaskRepository();
            var task = new ScheduledTask
            {
                Name = "测试任务",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow
            };

            // Act
            repo.AddTask(task);

            // Assert
            var savedTask = repo.GetTask(task.Id);
            Assert.NotNull(savedTask);
            Assert.Equal(task.Name, savedTask.Name);
            Assert.Equal(task.CronExpression, savedTask.CronExpression);
            Assert.Equal(task.NextRunTime, savedTask.NextRunTime);
        }

        [Fact]
        public void UpdateTask_ExistingTask_ShouldUpdateTask()
        {
            // Arrange
            var repo = new InMemoryTaskRepository();
            var task = new ScheduledTask
            {
                Name = "原始任务",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow
            };
            repo.AddTask(task);

            // Act
            task.Name = "更新后的任务";
            task.CronExpression = "*/10 * * * * *";
            repo.UpdateTask(task);

            // Assert
            var updatedTask = repo.GetTask(task.Id);
            Assert.NotNull(updatedTask);
            Assert.Equal("更新后的任务", updatedTask.Name);
            Assert.Equal("*/10 * * * * *", updatedTask.CronExpression);
        }

        [Fact]
        public void DeleteTask_ExistingTask_ShouldRemoveTask()
        {
            // Arrange
            var repo = new InMemoryTaskRepository();
            var task = new ScheduledTask
            {
                Name = "要删除的任务",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow
            };
            repo.AddTask(task);

            // Act
            repo.DeleteTask(task.Id);

            // Assert
            var deletedTask = repo.GetTask(task.Id);
            Assert.Null(deletedTask);
        }

        [Fact]
        public void GetAllTasks_MultipleTasksAdded_ShouldReturnAllTasks()
        {
            // Arrange
            var repo = new InMemoryTaskRepository();
            var task1 = new ScheduledTask { Name = "任务1" };
            var task2 = new ScheduledTask { Name = "任务2" };
            var task3 = new ScheduledTask { Name = "任务3" };

            // Act
            repo.AddTask(task1);
            repo.AddTask(task2);
            repo.AddTask(task3);

            // Assert
            var allTasks = repo.GetAllTasks().ToList();
            Assert.Equal(3, allTasks.Count);
            Assert.Contains(allTasks, t => t.Name == "任务1");
            Assert.Contains(allTasks, t => t.Name == "任务2");
            Assert.Contains(allTasks, t => t.Name == "任务3");
        }

        [Fact]
        public void UpdateTask_NonExistingTask_ShouldNotThrowException()
        {
            // Arrange
            var repo = new InMemoryTaskRepository();
            var task = new ScheduledTask
            {
                Name = "不存在的任务",
                CronExpression = "*/5 * * * * *"
            };

            // Act & Assert
            var exception = Record.Exception(() => repo.UpdateTask(task));
            Assert.Null(exception);
        }

        [Fact]
        public void DeleteTask_NonExistingTask_ShouldNotThrowException()
        {
            // Arrange
            var repo = new InMemoryTaskRepository();
            var nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = Record.Exception(() => repo.DeleteTask(nonExistingId));
            Assert.Null(exception);
        }
    }
} 