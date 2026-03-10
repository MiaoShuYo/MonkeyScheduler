using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;
using MonkeyScheduler.WorkerService.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using MonkeyScheduler.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace MonkeyScheduler.WorkerService.Tests
{
    // 创建一个简单的ITaskRepository实现类用于测试
    public class TestTaskRepository : ITaskRepository
    {
        private readonly List<ScheduledTask> _tasks = new();

        public void AddTask(ScheduledTask task)
        {
            _tasks.Add(task);
        }

        public Task AddTaskAsync(ScheduledTask task)
        {
            _tasks.Add(task);
            return Task.CompletedTask;
        }

        public void UpdateTask(ScheduledTask task)
        {
            var index = _tasks.FindIndex(t => t.Id == task.Id);
            if (index != -1)
            {
                _tasks[index] = task;
            }
        }

        public Task UpdateTaskAsync(ScheduledTask task)
        {
            var index = _tasks.FindIndex(t => t.Id == task.Id);
            if (index != -1)
            {
                _tasks[index] = task;
            }
            return Task.CompletedTask;
        }

        public void DeleteTask(Guid taskId)
        {
            _tasks.RemoveAll(t => t.Id == taskId);
        }

        public Task DeleteTaskAsync(Guid taskId)
        {
            _tasks.RemoveAll(t => t.Id == taskId);
            return Task.CompletedTask;
        }

        public ScheduledTask? GetTask(Guid taskId)
        {
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        public Task<ScheduledTask?> GetTaskAsync(Guid taskId)
        {
            return Task.FromResult(_tasks.FirstOrDefault(t => t.Id == taskId));
        }

        public IEnumerable<ScheduledTask> GetAllTasks()
        {
            return _tasks;
        }

        public Task<IEnumerable<ScheduledTask>> GetAllTasksAsync()
        {
            return Task.FromResult<IEnumerable<ScheduledTask>>(_tasks);
        }
    }

    // 创建一个简单的ITaskExecutor实现类用于测试
    public class TestTaskExecutor : ITaskExecutor
    {
        public Task ExecuteAsync(ScheduledTask task, Func<TaskExecutionResult, Task>? statusCallback = null)
        {
            return Task.CompletedTask;
        }
    }

    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        private IServiceCollection _services;
        private Mock<IConfiguration> _configurationMock;
        private const string WorkerUrl = "http://test-worker";
        private const string SchedulerUrl = "http://test-scheduler";

        [TestInitialize]
        public void Initialize()
        {
            _services = new ServiceCollection();
            _configurationMock = new Mock<IConfiguration>();

            // 创建配置节Mock
            var configurationSectionMock = new Mock<IConfigurationSection>();
            configurationSectionMock.Setup(x => x["SchedulerUrl"]).Returns(SchedulerUrl);
            configurationSectionMock.Setup(x => x["WorkerUrl"]).Returns(WorkerUrl);
            configurationSectionMock.Setup(x => x.Value).Returns((string)null);
            configurationSectionMock.Setup(x => x.Path).Returns("MonkeyScheduler:Worker");
            configurationSectionMock.Setup(x => x.Key).Returns("Worker");

            // 设置配置Mock
            _configurationMock.Setup(x => x["MonkeyScheduler:SchedulingServer:Url"])
                .Returns(SchedulerUrl);
            _configurationMock.Setup(x => x.GetSection("MonkeyScheduler:Worker"))
                .Returns(configurationSectionMock.Object);

            _services.AddSingleton(_configurationMock.Object);
            _services.AddSingleton(Mock.Of<ILogger<NodeHeartbeatService>>());
            _services.AddSingleton(Mock.Of<ILogger<StatusReporterService>>());
            _services.AddHealthChecks();
            _services.AddSingleton<WorkerHealthCheck>();
        }


        [TestMethod]
        public void AddWorkerService_RegistersRequiredServices()
        {
            // Act
            _services.AddWorkerService(WorkerUrl);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();

            // 验证 ITaskRepository 注册
            var taskRepository = serviceProvider.GetService<ITaskRepository>();
            Assert.IsNotNull(taskRepository, "ITaskRepository 未正确注册");
            Assert.IsInstanceOfType(taskRepository, typeof(InMemoryTaskRepository));

            // 验证 IHttpClientFactory 注册
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            Assert.IsNotNull(httpClientFactory, "IHttpClientFactory 未正确注册");

            // 验证 StatusReporterService 注册
            var statusReporter = serviceProvider.GetService<IStatusReporterService>();
            Assert.IsNotNull(statusReporter, "StatusReporterService 未正确注册");
            Assert.IsInstanceOfType(statusReporter, typeof(StatusReporterService));

            // 验证 NodeHeartbeatService 注册
            var hostedServices = serviceProvider.GetServices<IHostedService>();
            var heartbeatService = hostedServices.OfType<NodeHeartbeatService>().FirstOrDefault();
            Assert.IsNotNull(heartbeatService, "NodeHeartbeatService 未正确注册");

            // 验证健康检查服务注册
            var healthCheckService = serviceProvider.GetService<WorkerHealthCheck>();
            Assert.IsNotNull(healthCheckService, "WorkerHealthCheck 未正确注册");
        }

        [TestMethod]
        public void AddWorkerService_UsesDefaultSchedulerUrl_WhenNotConfigured()
        {
            // Arrange
            _configurationMock.Setup(x => x["MonkeyScheduler:SchedulingServer:Url"])
                .Returns((string)null);

            // 新增：Mock Worker 配置节，避免 null
            var workerSectionMock = new Mock<IConfigurationSection>();
            workerSectionMock.Setup(x => x.Value).Returns((string)null);
            workerSectionMock.Setup(x => x.Path).Returns("MonkeyScheduler:Worker");
            workerSectionMock.Setup(x => x.Key).Returns("Worker");
            _configurationMock.Setup(x => x.GetSection("MonkeyScheduler:Worker"))
                .Returns(workerSectionMock.Object);

            // Act
            _services.AddWorkerService(WorkerUrl);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var statusReporter = serviceProvider.GetService<IStatusReporterService>();
            Assert.IsNotNull(statusReporter);

            // 通过反射获取私有字段 _schedulerUrl 的值
            var schedulerUrlField = typeof(StatusReporterService).GetField("_schedulerUrl", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var schedulerUrl = schedulerUrlField?.GetValue(statusReporter) as string;
            
            // 验证是否使用了默认的调度器 URL
            Assert.AreEqual("http://localhost:4057", schedulerUrl);
        }

        [TestMethod]
        public void AddTaskRepository_RegistersCustomRepository()
        {
            // Arrange
            var customRepository = new Mock<ITaskRepository>().Object;

            // Act
            _services.AddTaskRepository<ITaskRepository>(_ => customRepository);

            // Assert
            var registeredRepository = _services.BuildServiceProvider().GetService<ITaskRepository>();
            Assert.IsNotNull(registeredRepository);
            Assert.AreSame(customRepository, registeredRepository);
        }

        [TestMethod]
        public void AddTaskExecutor_RegistersCustomExecutor()
        {
            // Arrange
            var customExecutor = new Mock<ITaskExecutor>().Object;

            // Act
            _services.AddTaskExecutor<ITaskExecutor>(_ => customExecutor);

            // Assert
            var registeredExecutor = _services.BuildServiceProvider().GetService<ITaskExecutor>();
            Assert.IsNotNull(registeredExecutor);
            Assert.AreSame(customExecutor, registeredExecutor);
        }

        [TestMethod]
        public void AddTaskRepository_RegistersGenericRepository()
        {
            // Act
            // 使用泛型方法注册任务仓库
            _services.AddTaskRepository<TestTaskRepository>();

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var registeredRepository = serviceProvider.GetService<ITaskRepository>();
            Assert.IsNotNull(registeredRepository);
            Assert.IsInstanceOfType(registeredRepository, typeof(TestTaskRepository));
        }

        [TestMethod]
        public void AddTaskExecutor_RegistersGenericExecutor()
        {
            // Act
            // 使用泛型方法注册任务执行器
            _services.AddTaskExecutor<TestTaskExecutor>();

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var registeredExecutor = serviceProvider.GetService<ITaskExecutor>();
            Assert.IsNotNull(registeredExecutor);
            Assert.IsInstanceOfType(registeredExecutor, typeof(TestTaskExecutor));
        }

        [TestMethod]
        public void UseWorkerService_ConfiguresHealthChecks()
        {
            // Arrange
            var services = new ServiceCollection();

            // 添加日志服务
            var loggerFactory = new Mock<ILoggerFactory>();
            var logger = new Mock<ILogger>();
            loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
            services.AddLogging(builder =>
            {
                builder.Services.AddSingleton<ILoggerFactory>(loggerFactory.Object);
                builder.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            });

            services.AddHealthChecks();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHealthCheck, WorkerHealthCheck>());
            services.Configure<HealthCheckOptions>(options => { });
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            ServiceCollectionExtensions.UseWorkerService(appBuilder);

            // Assert
            var middleware = appBuilder.Build();
            Assert.IsNotNull(middleware);
        }
    }
}