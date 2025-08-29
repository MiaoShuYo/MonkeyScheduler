using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Core.Services.Handlers;
using MonkeyScheduler.Core.Configuration;
using System.Text.Json;

namespace MonkeySchedulerTest
{
    /// <summary>
    /// 任务处理器测试类
    /// 验证任务类型插件机制的功能
    /// </summary>
    [TestClass]
    public class TaskHandlerTests
    {
        private IServiceProvider _serviceProvider;
        private ITaskHandlerFactory _handlerFactory;

        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // 添加日志服务
            services.AddLogging(builder => builder.AddConsole());
            
            // 添加HTTP客户端工厂
            services.AddHttpClient();
            
            // 添加任务处理器服务
            services.AddTaskHandlers();
            
            // 配置任务处理器
            services.ConfigureTaskHandlers(factory =>
            {
                // 注册内置处理器
                factory.RegisterHandler<HttpTaskHandler>("http");
                factory.RegisterHandler<SqlTaskHandler>("sql");
                factory.RegisterHandler<ShellTaskHandler>("shell");
                factory.RegisterHandler<CustomTaskHandler>("custom");
            });
            
            _serviceProvider = services.BuildServiceProvider();
            _handlerFactory = _serviceProvider.GetRequiredService<ITaskHandlerFactory>();
        }

        [TestMethod]
        public void TestTaskHandlerFactory_GetSupportedTaskTypes()
        {
            // 验证支持的任务类型
            var supportedTypes = _handlerFactory.GetSupportedTaskTypes().ToList();
            
            Assert.IsTrue(supportedTypes.Contains("http"), "应该支持HTTP任务类型");
            Assert.IsTrue(supportedTypes.Contains("sql"), "应该支持SQL任务类型");
            Assert.IsTrue(supportedTypes.Contains("shell"), "应该支持Shell任务类型");
            Assert.IsTrue(supportedTypes.Contains("custom"), "应该支持自定义任务类型");
        }

        [TestMethod]
        public void TestTaskHandlerFactory_IsTaskTypeSupported()
        {
            // 验证任务类型支持检查
            Assert.IsTrue(_handlerFactory.IsTaskTypeSupported("http"));
            Assert.IsTrue(_handlerFactory.IsTaskTypeSupported("sql"));
            Assert.IsTrue(_handlerFactory.IsTaskTypeSupported("shell"));
            Assert.IsTrue(_handlerFactory.IsTaskTypeSupported("custom"));
            Assert.IsFalse(_handlerFactory.IsTaskTypeSupported("unknown"));
        }

        [TestMethod]
        public void TestHttpTaskHandler_Configuration()
        {
            var handler = _handlerFactory.GetHandler("http");
            var config = handler.GetConfiguration();
            
            Assert.AreEqual("http", config.TaskType);
            Assert.IsTrue(config.SupportsRetry);
            Assert.IsTrue(config.SupportsTimeout);
            Assert.AreEqual(30, config.DefaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task TestHttpTaskHandler_ValidateParameters()
        {
            var handler = _handlerFactory.GetHandler("http");
            
            // 有效参数
            var validParams = new HttpTaskParameters
            {
                Url = "https://httpbin.org/get",
                Method = "GET"
            };
            var isValid = await handler.ValidateParametersAsync(validParams);
            Assert.IsTrue(isValid);
            
            // 无效参数
            var invalidParams = new HttpTaskParameters
            {
                Url = "",
                Method = ""
            };
            var isInvalid = await handler.ValidateParametersAsync(invalidParams);
            Assert.IsFalse(isInvalid);
        }

        [TestMethod]
        public void TestCustomTaskHandler_Configuration()
        {
            var handler = _handlerFactory.GetHandler("custom");
            var config = handler.GetConfiguration();
            
            Assert.AreEqual("custom", config.TaskType);
            Assert.IsTrue(config.SupportsRetry);
            Assert.IsTrue(config.SupportsTimeout);
            Assert.AreEqual(60, config.DefaultTimeoutSeconds);
        }

        [TestMethod]
        public async Task TestCustomTaskHandler_ValidateParameters()
        {
            var handler = _handlerFactory.GetHandler("custom");
            
            // 有效参数
            var validParams = new CustomTaskParameters
            {
                Operation = "echo",
                Message = "Hello World"
            };
            var isValid = await handler.ValidateParametersAsync(validParams);
            Assert.IsTrue(isValid);
            
            // 无效参数
            var invalidParams = new CustomTaskParameters
            {
                Operation = "",
                Message = "Hello World"
            };
            var isInvalid = await handler.ValidateParametersAsync(invalidParams);
            Assert.IsFalse(isInvalid);
        }

        [TestMethod]
        public async Task TestPluginTaskExecutor_ExecuteCustomTask()
        {
            var executor = _serviceProvider.GetRequiredService<PluginTaskExecutor>();
            
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "测试自定义任务",
                TaskType = "custom",
                TaskParameters = JsonSerializer.Serialize(new CustomTaskParameters
                {
                    Operation = "echo",
                    Message = "Hello World",
                    DelayMilliseconds = 100
                })
            };
            
            TaskExecutionResult? result = null;
            await executor.ExecuteAsync(task, async (executionResult) =>
            {
                result = executionResult;
                await Task.CompletedTask;
            });
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(ExecutionStatus.Completed, result.Status);
            Assert.IsTrue(result.Result.Contains("回显消息: Hello World"));
        }

        [TestMethod]
        public async Task TestPluginTaskExecutor_ExecuteHttpTask()
        {
            var executor = _serviceProvider.GetRequiredService<PluginTaskExecutor>();
            
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "测试HTTP任务",
                TaskType = "http",
                TaskParameters = JsonSerializer.Serialize(new HttpTaskParameters
                {
                    Url = "https://httpbin.org/get",
                    Method = "GET"
                })
            };
            
            TaskExecutionResult? result = null;
            await executor.ExecuteAsync(task, async (executionResult) =>
            {
                result = executionResult;
                await Task.CompletedTask;
            });
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(ExecutionStatus.Completed, result.Status);
        }

        [TestMethod]
        public async Task TestPluginTaskExecutor_InvalidTaskType()
        {
            var executor = _serviceProvider.GetRequiredService<PluginTaskExecutor>();
            
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "测试无效任务类型",
                TaskType = "unknown",
                TaskParameters = "{}"
            };
            
            // 由于PluginTaskExecutor内部捕获了异常并返回失败结果，而不是抛出异常
            // 我们需要验证执行结果而不是期望抛出异常
            TaskExecutionResult? result = null;
            await executor.ExecuteAsync(task, async (executionResult) =>
            {
                result = executionResult;
                await Task.CompletedTask;
            });
            
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(ExecutionStatus.Failed, result.Status);
            Assert.IsTrue(result.ErrorMessage.Contains("未找到任务类型"));
        }

        [TestMethod]
        public async Task TestPluginTaskExecutor_InvalidParameters()
        {
            var executor = _serviceProvider.GetRequiredService<PluginTaskExecutor>();
            
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "测试无效参数",
                TaskType = "http",
                TaskParameters = JsonSerializer.Serialize(new HttpTaskParameters
                {
                    Url = "",
                    Method = ""
                })
            };
            
            TaskExecutionResult? result = null;
            await executor.ExecuteAsync(task, async (executionResult) =>
            {
                result = executionResult;
                await Task.CompletedTask;
            });
            
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(ExecutionStatus.Failed, result.Status);
        }

        [TestMethod]
        public void TestTaskHandlerFactory_RegisterCustomHandler()
        {
            // 测试注册自定义处理器
            var factory = new TaskHandlerFactory(_serviceProvider, _serviceProvider.GetRequiredService<ILogger<TaskHandlerFactory>>());
            
            factory.RegisterHandler<CustomTaskHandler>("test-custom");
            
            Assert.IsTrue(factory.IsTaskTypeSupported("test-custom"));
            var handler = factory.GetHandler("test-custom");
            Assert.IsNotNull(handler);
            Assert.AreEqual("custom", handler.TaskType);
        }
    }
}
