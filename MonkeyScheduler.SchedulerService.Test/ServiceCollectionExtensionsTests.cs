using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService;
using MonkeyScheduler.SchedulerService.Services;
using MonkeyScheduler.SchedulerService.Services.Strategies;
using MonkeyScheduler.Storage;
using Moq;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddLoadBalancer_RegistersLoadBalancerAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddLoadBalancer<LoadBalancer>();

            // Assert
            var serviceDescriptor = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ILoadBalancer) && 
                sd.ImplementationType == typeof(LoadBalancer));

            Assert.IsNotNull(serviceDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }

        [TestMethod]
        public void AddLoadBalancer_WithFactory_RegistersLoadBalancerAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddLoadBalancer<LoadBalancer>(sp => new LoadBalancer(new NodeRegistry(), new LeastConnectionStrategy()));

            // Assert
            var serviceDescriptor = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ILoadBalancer));

            Assert.IsNotNull(serviceDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
            Assert.IsNotNull(serviceDescriptor.ImplementationFactory);
        }

        [TestMethod]
        public void AddTaskRepository_RegistersTaskRepositoryAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTaskRepository<InMemoryTaskRepository>();

            // Assert
            var serviceDescriptor = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ITaskRepository) && 
                sd.ImplementationType == typeof(InMemoryTaskRepository));

            Assert.IsNotNull(serviceDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }

        [TestMethod]
        public void AddTaskRepository_WithFactory_RegistersTaskRepositoryAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTaskRepository<InMemoryTaskRepository>(sp => new InMemoryTaskRepository());

            // Assert
            var serviceDescriptor = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ITaskRepository));

            Assert.IsNotNull(serviceDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
            Assert.IsNotNull(serviceDescriptor.ImplementationFactory);
        }

        [TestMethod]
        public void AddSchedulerService_RegistersAllRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddSchedulerService();

            // Assert
            // 检查基础服务
            var loadBalancerService = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ILoadBalancer) && 
                sd.Lifetime == ServiceLifetime.Singleton &&
                (sd.ImplementationType == typeof(LoadBalancer) || sd.ImplementationFactory != null)
            );
            Assert.IsNotNull(loadBalancerService, "ILoadBalancer service not found or incorrectly configured");

            var nodeRegistryService = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(INodeRegistry) && 
                sd.ImplementationType == typeof(NodeRegistry) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.IsNotNull(nodeRegistryService, "INodeRegistry service not found or incorrectly configured");

            var taskRepositoryService = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ITaskRepository) && 
                sd.ImplementationType == typeof(InMemoryTaskRepository) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.IsNotNull(taskRepositoryService, "ITaskRepository service not found or incorrectly configured");

            var schedulerService = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(Scheduler) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.IsNotNull(schedulerService, "Scheduler service not found or incorrectly configured");

            // 检查任务调度相关服务
            var taskDispatcherService = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ITaskDispatcher) && 
                sd.ImplementationType == typeof(TaskDispatcher) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.IsNotNull(taskDispatcherService, "ITaskDispatcher service not found or incorrectly configured");

            var taskRetryManagerService = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(ITaskRetryManager) && 
                sd.ImplementationType == typeof(TaskRetryManager) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.IsNotNull(taskRetryManagerService, "ITaskRetryManager service not found or incorrectly configured");

            // 检查HTTP客户端
            var httpClientFactoryService = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(IHttpClientFactory) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.IsNotNull(httpClientFactoryService, "IHttpClientFactory service not found or incorrectly configured");

            // 检查健康检查
            var healthCheckService = services.FirstOrDefault(sd => 
                sd.ServiceType == typeof(HealthCheckService) &&
                sd.Lifetime == ServiceLifetime.Singleton);
            Assert.IsNotNull(healthCheckService, "HealthCheckService service not found or incorrectly configured");
        }

        [TestMethod]
        public void UseSchedulerService_ConfiguresHealthChecks()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // 创建必需的依赖项
            var taskRepository = new InMemoryTaskRepository();
            var nodeRegistry = new NodeRegistry();
            var loadBalancer = new LoadBalancer(nodeRegistry, new LeastConnectionStrategy());
            var httpClient = new HttpClient();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var mockRetryManager = new Mock<IEnhancedTaskRetryManager>();
            var loggerFactory = LoggerFactory.Create(builder => { });
            var taskDispatcher = new TaskDispatcher(nodeRegistry, httpClientFactory.Object, loadBalancer, mockRetryManager.Object, loggerFactory.CreateLogger<TaskDispatcher>(), Microsoft.Extensions.Options.Options.Create(new RetryConfiguration()));
            var logger = loggerFactory.CreateLogger<Scheduler>();
            
            // 注册基础服务
            services.AddSingleton<ILoadBalancer>(loadBalancer);
            services.AddSingleton<INodeRegistry>(nodeRegistry);
            services.AddSingleton<ITaskRepository>(taskRepository);
            services.AddSingleton<ITaskDispatcher>(taskDispatcher);
            services.AddSingleton<ILogger<Scheduler>>(logger);
            
            // 注册DAG相关服务
            var mockDagDependencyChecker = new Mock<IDagDependencyChecker>();
            var mockDagExecutionManager = new Mock<IDagExecutionManager>();
            services.AddSingleton<IDagDependencyChecker>(mockDagDependencyChecker.Object);
            services.AddSingleton<IDagExecutionManager>(mockDagExecutionManager.Object);
            services.AddSingleton<Scheduler>();
            
            // 添加其他服务
            services.AddHttpClient();
            services.AddHealthChecks()
                .AddCheck<SchedulerHealthCheck>("scheduler_health_check");
            
            var serviceProvider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(serviceProvider);

            // Act
            var result = ServiceCollectionExtensions.UseSchedulerService(appBuilder);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(appBuilder, result);
        }
    }
} 