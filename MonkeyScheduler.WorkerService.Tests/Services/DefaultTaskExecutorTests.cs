using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.WorkerService.Services;
using MonkeyScheduler.WorkerService.Tests.Extensions;
using Microsoft.Extensions.Options;

namespace MonkeyScheduler.WorkerService.Tests.Services
{
    [TestClass]
    public class DefaultTaskExecutorTests
    {
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private DefaultTaskExecutor _executor;
        private DefaultTaskExecutorOptions _options;

        [TestInitialize]
        public void Initialize()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _options = new DefaultTaskExecutorOptions
            {
                SchedulerUrl = "http://test-scheduler"
            };

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // 手动创建 IOptions<DefaultTaskExecutorOptions> 实例
            var optionsMock = Mock.Of<IOptions<DefaultTaskExecutorOptions>>(opt => opt.Value == _options);

            // 初始化 _executor
            _executor = new DefaultTaskExecutor(
                _httpClientFactoryMock.Object,
                optionsMock
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_SuccessfulExecution_CallsCallbackWithSuccess()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask"
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _httpMessageHandlerMock.SetupSendAsync(response);

            TaskExecutionResult? callbackResult = null;
            Func<TaskExecutionResult, Task> callback = result =>
            {
                callbackResult = result;
                return Task.CompletedTask;
            };

            // Act
            await _executor.ExecuteAsync(task, callback);

            // Assert
            Assert.IsNotNull(callbackResult);
            Assert.AreEqual(task.Id, callbackResult.TaskId);
            Assert.AreEqual(ExecutionStatus.Completed, callbackResult.Status);
            Assert.IsTrue(callbackResult.Success);
            Assert.IsNull(callbackResult.ErrorMessage);

            _httpMessageHandlerMock.VerifySendAsync(
                $"{_options.SchedulerUrl}/api/task/execute",
                HttpMethod.Post,
                Times.Once());
        }

        [TestMethod]
        public async Task ExecuteAsync_FailedExecution_CallsCallbackWithFailure()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask"
            };

            var errorMessage = "Test error";
            _httpMessageHandlerMock.SetupSendAsync(new Exception(errorMessage));

            TaskExecutionResult? callbackResult = null;
            Func<TaskExecutionResult, Task> callback = result =>
            {
                callbackResult = result;
                return Task.CompletedTask;
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<Exception>(() => 
                _executor.ExecuteAsync(task, callback));

            Assert.IsNotNull(callbackResult);
            Assert.AreEqual(task.Id, callbackResult.TaskId);
            Assert.AreEqual(ExecutionStatus.Failed, callbackResult.Status);
            Assert.IsFalse(callbackResult.Success);
            Assert.AreEqual(errorMessage, callbackResult.ErrorMessage);

            _httpMessageHandlerMock.VerifySendAsync(
                $"{_options.SchedulerUrl}/api/task/execute",
                HttpMethod.Post,
                Times.Once());
        }

        [TestMethod]
        public async Task ExecuteAsync_NoCallback_DoesNotThrow()
        {
            // Arrange
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask"
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _httpMessageHandlerMock.SetupSendAsync(response);

            // Act & Assert
            await _executor.ExecuteAsync(task);

            _httpMessageHandlerMock.VerifySendAsync(
                $"{_options.SchedulerUrl}/api/task/execute",
                HttpMethod.Post,
                Times.Once());
        }
    }
} 
