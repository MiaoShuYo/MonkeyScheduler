using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using Moq;

namespace MonkeyScheduler.SchedulerService.Test
{
    /// <summary>
    /// DAG工作流测试类
    /// </summary>
    [TestClass]
    public class DagWorkflowTests
    {
        private IDagDependencyChecker _dependencyChecker;
        private IDagExecutionManager _executionManager;
        private Mock<ILogger<DagDependencyChecker>> _mockLogger;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<DagDependencyChecker>>();
            _dependencyChecker = new DagDependencyChecker(_mockLogger.Object);
            _executionManager = new DagExecutionManager(_dependencyChecker, new Mock<ILogger<DagExecutionManager>>().Object);
        }

        [TestMethod]
        public async Task TestDependencyCheck_ValidDependencies_ShouldPass()
        {
            // Arrange
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1", Enabled = true };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task2", Enabled = true };
            var task3 = new ScheduledTask 
            { 
                Id = Guid.NewGuid(), 
                Name = "Task3", 
                Enabled = true,
                Dependencies = new List<Guid> { task1.Id, task2.Id }
            };

            var allTasks = new List<ScheduledTask> { task1, task2, task3 };

            // Act
            var result = await _dependencyChecker.CheckDependenciesAsync(task3, allTasks);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsFalse(result.MissingDependencies.Any());
            Assert.IsFalse(result.InvalidDependencies.Any());
        }

        [TestMethod]
        public async Task TestDependencyCheck_MissingDependencies_ShouldFail()
        {
            // Arrange
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1", Enabled = true };
            var task3 = new ScheduledTask 
            { 
                Id = Guid.NewGuid(), 
                Name = "Task3", 
                Enabled = true,
                Dependencies = new List<Guid> { task1.Id, Guid.NewGuid() } // 第二个依赖不存在
            };

            var allTasks = new List<ScheduledTask> { task1, task3 };

            // Act
            var result = await _dependencyChecker.CheckDependenciesAsync(task3, allTasks);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.MissingDependencies.Count);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [TestMethod]
        public async Task TestCycleDetection_NoCycles_ShouldPass()
        {
            // Arrange
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1" };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task2", Dependencies = new List<Guid> { task1.Id } };
            var task3 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task3", Dependencies = new List<Guid> { task2.Id } };

            var tasks = new List<ScheduledTask> { task1, task2, task3 };

            // Act
            var result = await _dependencyChecker.DetectCyclesAsync(tasks);

            // Assert
            Assert.IsFalse(result.HasCycle);
            Assert.AreEqual(0, result.Cycles.Count);
        }

        [TestMethod]
        public async Task TestCycleDetection_WithCycles_ShouldDetect()
        {
            // Arrange
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1" };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task2", Dependencies = new List<Guid> { task1.Id } };
            var task3 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task3", Dependencies = new List<Guid> { task2.Id } };
            // 创建循环依赖
            task1.Dependencies = new List<Guid> { task3.Id };

            var tasks = new List<ScheduledTask> { task1, task2, task3 };

            // Act
            var result = await _dependencyChecker.DetectCyclesAsync(tasks);

            // Assert
            Assert.IsTrue(result.HasCycle);
            Assert.IsTrue(result.Cycles.Count > 0);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [TestMethod]
        public async Task TestWorkflowValidation_ValidWorkflow_ShouldPass()
        {
            // Arrange
            var workflowId = Guid.NewGuid();
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1", DagWorkflowId = workflowId };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task2", DagWorkflowId = workflowId, Dependencies = new List<Guid> { task1.Id } };
            var task3 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task3", DagWorkflowId = workflowId, Dependencies = new List<Guid> { task2.Id } };

            // 设置NextTaskIds以正确标识出口任务
            task1.NextTaskIds = new List<Guid> { task2.Id };
            task2.NextTaskIds = new List<Guid> { task3.Id };
            // task3没有NextTaskIds，所以它是出口任务

            var tasks = new List<ScheduledTask> { task1, task2, task3 };

            // Act
            var result = await _dependencyChecker.ValidateWorkflowAsync(workflowId, tasks);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(1, result.EntryTasks.Count);
            Assert.AreEqual(1, result.ExitTasks.Count); // 只有Task3是出口任务
            Assert.AreEqual(3, result.ExecutionLevels.Count); // 3个层级：Task1(0), Task2(1), Task3(2)
        }

        [TestMethod]
        public async Task TestWorkflowValidation_EmptyWorkflow_ShouldFail()
        {
            // Arrange
            var workflowId = Guid.NewGuid();
            var tasks = new List<ScheduledTask>();

            // Act
            var result = await _dependencyChecker.ValidateWorkflowAsync(workflowId, tasks);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.ErrorMessage);
        }

        [TestMethod]
        public async Task TestWorkflowExecution_StartWorkflow_ShouldSucceed()
        {
            // Arrange
            var workflowId = Guid.NewGuid();
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1", DagWorkflowId = workflowId };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task2", DagWorkflowId = workflowId, Dependencies = new List<Guid> { task1.Id } };
            var task3 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task3", DagWorkflowId = workflowId, Dependencies = new List<Guid> { task2.Id } };

            var tasks = new List<ScheduledTask> { task1, task2, task3 };

            // Act
            var result = await _executionManager.StartWorkflowAsync(workflowId, tasks);

            // Assert
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(workflowId, result.WorkflowId);
            Assert.AreEqual(1, result.StartedTaskCount); // 只有入口任务被启动
        }

        [TestMethod]
        public async Task TestWorkflowExecution_GetStatus_ShouldReturnStatus()
        {
            // Arrange
            var workflowId = Guid.NewGuid();

            // Act
            var status = await _executionManager.GetWorkflowStatusAsync(workflowId);

            // Assert
            Assert.IsNotNull(status);
            Assert.AreEqual(workflowId, status.WorkflowId);
            Assert.AreEqual(WorkflowStatus.NotStarted, status.Status);
        }

        [TestMethod]
        public async Task TestTaskCompletion_ShouldTriggerNextTasks()
        {
            // Arrange
            var workflowId = Guid.NewGuid();
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1", DagWorkflowId = workflowId };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task2", DagWorkflowId = workflowId, Dependencies = new List<Guid> { task1.Id } };
            var task3 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task3", DagWorkflowId = workflowId, Dependencies = new List<Guid> { task2.Id } };

            // 设置任务1的后续任务
            task1.NextTaskIds = new List<Guid> { task2.Id };
            task2.NextTaskIds = new List<Guid> { task3.Id };

            var tasks = new List<ScheduledTask> { task1, task2, task3 };

            // 启动工作流
            await _executionManager.StartWorkflowAsync(workflowId, tasks);

            // 模拟任务1完成
            var result = new TaskExecutionResult
            {
                TaskId = task1.Id,
                Success = true,
                Status = ExecutionStatus.Completed
            };

            // Act
            var triggeredTasks = await _executionManager.OnTaskCompletedAsync(task1.Id, result, tasks);

            // Assert
            Assert.AreEqual(1, triggeredTasks.Count);
            Assert.AreEqual(task2.Id, triggeredTasks[0].Id);
            
            // 验证任务2的状态已更新为Ready
            Assert.AreEqual(DagExecutionStatus.Ready, task2.DagStatus);
        }

        [TestMethod]
        public async Task TestCanExecuteTask_NoDependencies_ShouldReturnTrue()
        {
            // Arrange
            var task = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1" };
            var allTasks = new List<ScheduledTask> { task };

            // Act
            var canExecute = await _executionManager.CanExecuteTaskAsync(task, allTasks);

            // Assert
            Assert.IsTrue(canExecute);
        }

        [TestMethod]
        public async Task TestCanExecuteTask_WithUnmetDependencies_ShouldReturnFalse()
        {
            // Arrange
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1" };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task2", Dependencies = new List<Guid> { task1.Id } };
            var allTasks = new List<ScheduledTask> { task1, task2 };

            // Act
            var canExecute = await _executionManager.CanExecuteTaskAsync(task2, allTasks);

            // Assert
            Assert.IsFalse(canExecute);
        }

        [TestMethod]
        public async Task TestWorkflowControl_PauseResumeCancel_ShouldWork()
        {
            // Arrange
            var workflowId = Guid.NewGuid();

            // Act & Assert
            var pauseResult = await _executionManager.PauseWorkflowAsync(workflowId);
            Assert.IsFalse(pauseResult); // 工作流不存在

            var resumeResult = await _executionManager.ResumeWorkflowAsync(workflowId);
            Assert.IsFalse(resumeResult); // 工作流不存在

            var cancelResult = await _executionManager.CancelWorkflowAsync(workflowId);
            Assert.IsFalse(cancelResult); // 工作流不存在
        }

        [TestMethod]
        public async Task TestDependencyPaths_ShouldReturnCorrectPaths()
        {
            // Arrange
            var task1 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task1" };
            var task2 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task2", Dependencies = new List<Guid> { task1.Id } };
            var task3 = new ScheduledTask { Id = Guid.NewGuid(), Name = "Task3", Dependencies = new List<Guid> { task2.Id } };

            var allTasks = new List<ScheduledTask> { task1, task2, task3 };

            // Act
            var paths = await _dependencyChecker.GetDependencyPathsAsync(task3.Id, allTasks);

            // Assert
            Assert.IsTrue(paths.Count > 0);
            Assert.IsTrue(paths.Any(p => p.Contains(task1.Id) && p.Contains(task2.Id) && p.Contains(task3.Id)));
        }
    }
}
