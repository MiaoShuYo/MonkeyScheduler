using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class TaskDispatcherTests
    {
        private Mock<INodeRegistry> _mockNodeRegistry;
        private Mock<ILoadBalancer> _mockLoadBalancer;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private Mock<IEnhancedTaskRetryManager> _mockRetryManager;
        private Mock<ILogger<TaskDispatcher>> _mockLogger;
        private Mock<IOptions<RetryConfiguration>> _mockRetryOptions;
        private HttpClient _httpClient;
        private Mock<IHttpClientFactory> _httpClientFactory;
        private TaskDispatcher _taskDispatcher;
        private ScheduledTask _testTask;
        private const string NodeUrl = "http://test-node:5000";

        [TestInitialize]
        public void Setup()
        {
            _mockNodeRegistry = new Mock<INodeRegistry>();
            _mockLoadBalancer = new Mock<ILoadBalancer>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockRetryManager = new Mock<IEnhancedTaskRetryManager>();
            _mockLogger = new Mock<ILogger<TaskDispatcher>>();
            _mockRetryOptions = new Mock<IOptions<RetryConfiguration>>();
            _mockRetryOptions.Setup(x => x.Value).Returns(new RetryConfiguration());
            
            // 设置重试管理器的Mock行为
            _mockRetryManager.Setup(rm => rm.ShouldRetryTask(It.IsAny<ScheduledTask>())).Returns(false);
            _mockRetryManager.Setup(rm => rm.ResetRetryState(It.IsAny<ScheduledTask>()));
            
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);
            _taskDispatcher = new TaskDispatcher(_mockNodeRegistry.Object, _httpClientFactory.Object, _mockLoadBalancer.Object, _mockRetryManager.Object, _mockLogger.Object, _mockRetryOptions.Object);
            
            _testTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "Test Task",
                CronExpression = "0 * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true,
                EnableRetry = false, // 禁用重试，确保异常能够抛出
                MaxRetryCount = 0,
                CurrentRetryCount = 0
            };
        }

        [TestMethod]
        public async Task DispatchTaskAsync_WithValidTaskAndCallback_CallsCallbackWithSuccessResult()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NodeUrl);
            SetupMockHttpResponse(HttpMethod.Post, $"{NodeUrl}/api/task/execute", HttpStatusCode.OK);
            
            TaskExecutionResult? callbackResult = null;
            Func<TaskExecutionResult, Task> callback = result =>
            {
                callbackResult = result;
                return Task.CompletedTask;
            };

            // Act
            await _taskDispatcher.DispatchTaskAsync(_testTask, callback);

            // Assert
            Assert.IsNotNull(callbackResult);
            Assert.AreEqual(_testTask.Id, callbackResult.TaskId);
            Assert.AreEqual(ExecutionStatus.Completed, callbackResult.Status);
            Assert.IsTrue(callbackResult.Success);
            Assert.AreEqual(NodeUrl, callbackResult.WorkerNodeUrl);
            Assert.IsNull(callbackResult.ErrorMessage);
        }

        [TestMethod]
        public async Task DispatchTaskAsync_WithValidTaskAndNoCallback_ExecutesSuccessfully()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NodeUrl);
            SetupMockHttpResponse(HttpMethod.Post, $"{NodeUrl}/api/task/execute", HttpStatusCode.OK);

            // Act
            await _taskDispatcher.DispatchTaskAsync(_testTask);

            // Assert
            VerifyHttpRequest(HttpMethod.Post, $"{NodeUrl}/api/task/execute", Times.Exactly(1));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DispatchTaskAsync_WithNullTask_ThrowsArgumentNullException()
        {
            // Act
            await _taskDispatcher.DispatchTaskAsync(null);
        }

        [TestMethod]
        public async Task DispatchTaskAsync_WhenNodeFails_RemovesNodeAndCallsCallbackWithFailure()
        {
            // Arrange
            _testTask.EnableRetry = true;
            _mockRetryManager.Setup(rm => rm.RetryTaskAsync(It.IsAny<ScheduledTask>(), It.IsAny<string>(), It.IsAny<Exception>())).ReturnsAsync(false);
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NodeUrl);
            SetupMockHttpResponse(HttpMethod.Post, $"{NodeUrl}/api/task/execute", HttpStatusCode.InternalServerError);
            
            TaskExecutionResult? callbackResult = null;
            Func<TaskExecutionResult, Task> callback = result =>
            {
                callbackResult = result;
                return Task.CompletedTask;
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => 
                _taskDispatcher.DispatchTaskAsync(_testTask, callback));

            Assert.IsNotNull(callbackResult);
            Assert.AreEqual(_testTask.Id, callbackResult.TaskId);
            Assert.AreEqual(ExecutionStatus.Failed, callbackResult.Status);
            Assert.IsFalse(callbackResult.Success);
            Assert.AreEqual(NodeUrl, callbackResult.WorkerNodeUrl);
            Assert.IsNotNull(callbackResult.ErrorMessage);

            _mockNodeRegistry.Verify(nr => nr.RemoveNode(NodeUrl), Times.Exactly(1));
            _mockLoadBalancer.Verify(lb => lb.RemoveNode(NodeUrl), Times.Exactly(1));
        }

        [TestMethod]
        public async Task DispatchTaskAsync_WhenNodeFailsAndNoCallback_RemovesNodeAndThrowsException()
        {
            // Arrange
            _testTask.EnableRetry = true;
            _mockRetryManager.Setup(rm => rm.RetryTaskAsync(It.IsAny<ScheduledTask>(), It.IsAny<string>(), It.IsAny<Exception>())).ReturnsAsync(false);
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NodeUrl);
            SetupMockHttpResponse(HttpMethod.Post, $"{NodeUrl}/api/task/execute", HttpStatusCode.InternalServerError);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => 
                _taskDispatcher.DispatchTaskAsync(_testTask));

            _mockNodeRegistry.Verify(nr => nr.RemoveNode(NodeUrl), Times.Exactly(1));
            _mockLoadBalancer.Verify(lb => lb.RemoveNode(NodeUrl), Times.Exactly(1));
        }

        [TestMethod]
        public async Task DispatchTaskAsync_WhenNodeFailsWithNetworkError_HandlesExceptionCorrectly()
        {
            // Arrange
            _testTask.EnableRetry = true;
            _mockRetryManager.Setup(rm => rm.RetryTaskAsync(It.IsAny<ScheduledTask>(), It.IsAny<string>(), It.IsAny<Exception>())).ReturnsAsync(false);
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NodeUrl);
            SetupMockHttpResponse(HttpMethod.Post, $"{NodeUrl}/api/task/execute", 
                new HttpRequestException("Network error"));

            TaskExecutionResult? callbackResult = null;
            Func<TaskExecutionResult, Task> callback = result =>
            {
                callbackResult = result;
                return Task.CompletedTask;
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => 
                _taskDispatcher.DispatchTaskAsync(_testTask, callback));

            Assert.IsNotNull(callbackResult);
            Assert.AreEqual(_testTask.Id, callbackResult.TaskId);
            Assert.AreEqual(ExecutionStatus.Failed, callbackResult.Status);
            Assert.IsFalse(callbackResult.Success);
            Assert.AreEqual(NodeUrl, callbackResult.WorkerNodeUrl);
            Assert.AreEqual("Network error", callbackResult.ErrorMessage);

            _mockNodeRegistry.Verify(nr => nr.RemoveNode(NodeUrl), Times.Exactly(1));
            _mockLoadBalancer.Verify(lb => lb.RemoveNode(NodeUrl), Times.Exactly(1));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DispatchTaskAsync_WhenNoNodeAvailable_ThrowsInvalidOperationException()
        {
            // Arrange
            _testTask.EnableRetry = false;
            _mockRetryManager.Setup(rm => rm.ShouldRetryTask(It.IsAny<ScheduledTask>())).Returns(false);
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(string.Empty);

            // Act
            await _taskDispatcher.DispatchTaskAsync(_testTask);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DispatchTaskAsync_WhenSelectNodeThrowsException_PropagatesException()
        {
            // Arrange
            _testTask.EnableRetry = false;
            _mockRetryManager.Setup(rm => rm.ShouldRetryTask(It.IsAny<ScheduledTask>())).Returns(false);
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask))
                .Throws(new InvalidOperationException("No available nodes"));

            // Act
            await _taskDispatcher.DispatchTaskAsync(_testTask);
        }

        [TestMethod]
        public async Task DispatchTaskAsync_WhenExceptionOccursWithNullNode_HandlesCorrectly()
        {
            // Arrange
            _testTask.EnableRetry = false;
            _mockRetryManager.Setup(rm => rm.ShouldRetryTask(It.IsAny<ScheduledTask>())).Returns(false);
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask))
                .Returns((string)null);

            TaskExecutionResult? callbackResult = null;
            Func<TaskExecutionResult, Task> callback = result =>
            {
                callbackResult = result;
                return Task.CompletedTask;
            };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => 
                _taskDispatcher.DispatchTaskAsync(_testTask, callback));

            Assert.IsNotNull(callbackResult);
            Assert.AreEqual(_testTask.Id, callbackResult.TaskId);
            Assert.AreEqual(ExecutionStatus.Failed, callbackResult.Status);
            Assert.IsFalse(callbackResult.Success);
            Assert.AreEqual(string.Empty, callbackResult.WorkerNodeUrl);
            Assert.IsNotNull(callbackResult.ErrorMessage);

            _mockNodeRegistry.Verify(nr => nr.RemoveNode(It.IsAny<string>()), Times.Never);
            _mockLoadBalancer.Verify(lb => lb.RemoveNode(It.IsAny<string>()), Times.Never);
        }

        // 辅助方法
        private void SetupMockHttpResponse(HttpMethod method, string url, HttpStatusCode statusCode)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == method && 
                        req.RequestUri.ToString() == url),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(statusCode));
        }

        private void SetupMockHttpResponse(HttpMethod method, string url, Exception exception)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == method && 
                        req.RequestUri.ToString() == url),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(exception);
        }

        private void VerifyHttpRequest(HttpMethod method, string url, Times times)
        {
            _mockHttpMessageHandler
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    times,
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == method && 
                        req.RequestUri.ToString() == url),
                    ItExpr.IsAny<CancellationToken>()
                );
        }
    }
} 