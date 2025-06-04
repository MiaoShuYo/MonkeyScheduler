using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService;
using MonkeyScheduler.SchedulerService.Services;
using MonkeyScheduler.Storage;

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
            services.AddLoadBalancer<LoadBalancer>(sp => new LoadBalancer(new NodeRegistry()));

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
            Assert.IsTrue(services.Any(sd => 
                sd.ServiceType == typeof(ILoadBalancer) && 
                sd.ImplementationType == typeof(LoadBalancer) &&
                sd.Lifetime == ServiceLifetime.Singleton));

            Assert.IsTrue(services.Any(sd => 
                sd.ServiceType == typeof(INodeRegistry) && 
                sd.ImplementationType == typeof(NodeRegistry) &&
                sd.Lifetime == ServiceLifetime.Singleton));

            Assert.IsTrue(services.Any(sd => 
                sd.ServiceType == typeof(ITaskRepository) && 
                sd.ImplementationType == typeof(InMemoryTaskRepository) &&
                sd.Lifetime == ServiceLifetime.Singleton));

            Assert.IsTrue(services.Any(sd => 
                sd.ServiceType == typeof(Scheduler) &&
                sd.Lifetime == ServiceLifetime.Singleton));

            // 检查任务调度相关服务
            Assert.IsTrue(services.Any(sd => 
                sd.ServiceType == typeof(ITaskDispatcher) && 
                sd.ImplementationType == typeof(TaskDispatcher) &&
                sd.Lifetime == ServiceLifetime.Singleton));

            Assert.IsTrue(services.Any(sd => 
                sd.ServiceType == typeof(ITaskRetryManager) && 
                sd.ImplementationType == typeof(TaskRetryManager) &&
                sd.Lifetime == ServiceLifetime.Singleton));

            // 检查HTTP客户端
            Assert.IsTrue(services.Any(sd => 
                sd.ServiceType == typeof(IHttpClientFactory) &&
                sd.Lifetime == ServiceLifetime.Singleton));

            // 检查健康检查
            Assert.IsTrue(services.Any(sd => 
                sd.ServiceType == typeof(HealthCheckService) &&
                sd.Lifetime == ServiceLifetime.Singleton));
        }

        [TestMethod]
        public void UseSchedulerService_ConfiguresHealthChecks()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // 创建必需的依赖项
            var taskRepository = new InMemoryTaskRepository();
            var nodeRegistry = new NodeRegistry();
            var loadBalancer = new LoadBalancer(nodeRegistry);
            var httpClient = new HttpClient();
            var taskDispatcher = new TaskDispatcher(nodeRegistry, httpClient, loadBalancer);
            var loggerFactory = LoggerFactory.Create(builder => { });
            var logger = loggerFactory.CreateLogger<Scheduler>();
            
            // 注册基础服务
            services.AddSingleton<ILoadBalancer>(loadBalancer);
            services.AddSingleton<INodeRegistry>(nodeRegistry);
            services.AddSingleton<ITaskRepository>(taskRepository);
            services.AddSingleton<ITaskDispatcher>(taskDispatcher);
            services.AddSingleton<ILogger<Scheduler>>(logger);
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