using MonkeyScheduler.Storage;
using MonkeyScheduler.Core.Models;

namespace MonkeySchedulerTest
{
    [TestClass]
    public class InMemoryTaskRepositoryTests
    {
        private InMemoryTaskRepository _repository;
        private ScheduledTask _testTask;

        [TestInitialize]
        public void Initialize()
        {
            _repository = new InMemoryTaskRepository();
            _testTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "Test Task",
                CronExpression = "0 * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };
        }

        [TestMethod]
        public void AddTask_ShouldAddTaskSuccessfully()
        {
            // Act
            _repository.AddTask(_testTask);

            // Assert
            var retrievedTask = _repository.GetTask(_testTask.Id);
            Assert.IsNotNull(retrievedTask);
            Assert.AreEqual(_testTask.Id, retrievedTask.Id);
            Assert.AreEqual(_testTask.Name, retrievedTask.Name);
        }

        [TestMethod]
        public void UpdateTask_ShouldUpdateExistingTask()
        {
            // Arrange
            _repository.AddTask(_testTask);
            var updatedTask = new ScheduledTask
            {
                Id = _testTask.Id,
                Name = "Updated Task",
                CronExpression = "0 * * * *",
                NextRunTime = DateTime.UtcNow.AddHours(1),
                Enabled = false
            };

            // Act
            _repository.UpdateTask(updatedTask);

            // Assert
            var retrievedTask = _repository.GetTask(_testTask.Id);
            Assert.IsNotNull(retrievedTask);
            Assert.AreEqual(updatedTask.Name, retrievedTask.Name);
            Assert.AreEqual(updatedTask.CronExpression, retrievedTask.CronExpression);
            Assert.AreEqual(updatedTask.Enabled, retrievedTask.Enabled);
            Assert.AreEqual(updatedTask.NextRunTime, retrievedTask.NextRunTime);
        }

        [TestMethod]
        public void DeleteTask_ShouldRemoveTask()
        {
            // Arrange
            _repository.AddTask(_testTask);

            // Act
            _repository.DeleteTask(_testTask.Id);

            // Assert
            var retrievedTask = _repository.GetTask(_testTask.Id);
            Assert.IsNull(retrievedTask);
        }

        [TestMethod]
        public void GetTask_ShouldReturnNullForNonExistentTask()
        {
            // Act
            var retrievedTask = _repository.GetTask(Guid.NewGuid());

            // Assert
            Assert.IsNull(retrievedTask);
        }

        [TestMethod]
        public void GetAllTasks_ShouldReturnAllTasks()
        {
            // Arrange
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task 1", CronExpression = "* * * * *", Enabled = true };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task 2", CronExpression = "* * * * *", Enabled = true };
            _repository.AddTask(task1);
            _repository.AddTask(task2);

            // Act
            var allTasks = _repository.GetAllTasks().ToList();

            // Assert
            Assert.AreEqual(2, allTasks.Count);
            Assert.IsTrue(allTasks.Any(t => t.Id == task1.Id));
            Assert.IsTrue(allTasks.Any(t => t.Id == task2.Id));
        }

        [TestMethod]
        public void UpdateTask_ShouldNotThrowForNonExistentTask()
        {
            // Arrange
            var nonExistentTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "Non Existent Task",
                CronExpression = "* * * * *",
                Enabled = true
            };

            // Act & Assert
            try
            {
                _repository.UpdateTask(nonExistentTask);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception, but got: {ex.Message}");
            }
        }
    }
} 