using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language.Flow;
using Moq.Protected;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class TaskDispatcherTests
    {
        private Mock<INodeRegistry> _mockNodeRegistry;
        private Mock<ILoadBalancer> _mockLoadBalancer;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private TaskDispatcher _taskDispatcher;
        private ScheduledTask _testTask;
        private const string NodeUrl = "http://test-node:5000";

        [TestInitialize]
        public void Setup()
        {
            _mockNodeRegistry = new Mock<INodeRegistry>();
            _mockLoadBalancer = new Mock<ILoadBalancer>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _taskDispatcher = new TaskDispatcher(_mockNodeRegistry.Object, _httpClient, _mockLoadBalancer.Object);
            
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
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(string.Empty);

            // Act
            await _taskDispatcher.DispatchTaskAsync(_testTask);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DispatchTaskAsync_WhenSelectNodeThrowsException_PropagatesException()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask))
                .Throws(new InvalidOperationException("No available nodes"));

            // Act
            await _taskDispatcher.DispatchTaskAsync(_testTask);
        }

        [TestMethod]
        public async Task DispatchTaskAsync_WhenExceptionOccursWithNullNode_HandlesCorrectly()
        {
            // Arrange
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