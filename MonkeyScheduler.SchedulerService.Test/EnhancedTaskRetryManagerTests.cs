using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class EnhancedTaskRetryManagerTests
    {
        private Mock<INodeRegistry> _mockNodeRegistry;
        private Mock<ILoadBalancer> _mockLoadBalancer;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private Mock<IHttpClientFactory> _httpClientFactory;
        private Mock<ILogger<EnhancedTaskRetryManager>> _mockLogger;
        private RetryConfiguration _retryConfig;
        private EnhancedTaskRetryManager _retryManager;
        private ScheduledTask _testTask;
        private const string FailedNodeUrl = "http://failed-node:5000";
        private const string NewNodeUrl = "http://new-node:5000";

        [TestInitialize]
        public void Setup()
        {
            _mockNodeRegistry = new Mock<INodeRegistry>();
            _mockLoadBalancer = new Mock<ILoadBalancer>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _mockLogger = new Mock<ILogger<EnhancedTaskRetryManager>>();
            
            _retryConfig = new RetryConfiguration
            {
                EnableRetry = true,
                DefaultMaxRetryCount = 3,
                DefaultRetryIntervalSeconds = 60,
                DefaultRetryStrategy = RetryStrategy.Exponential,
                SkipFailedNodes = true,
                EnableRetryLogging = true
            };
            
            var retryConfigOptions = Options.Create(_retryConfig);
            _retryManager = new EnhancedTaskRetryManager(
                _mockNodeRegistry.Object, 
                _mockLoadBalancer.Object, 
                _httpClientFactory.Object, 
                _mockLogger.Object, 
                retryConfigOptions);
            
            _testTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "Test Task",
                CronExpression = "0 * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true,
                EnableRetry = true,
                MaxRetryCount = 3,
                RetryIntervalSeconds = 60,
                RetryStrategy = RetryStrategy.Exponential
            };
        }

        [TestMethod]
        public async Task RetryTaskAsync_WithValidTaskAndFailedNode_RetriesOnNewNode()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NewNodeUrl);
            SetupMockHttpResponse(HttpMethod.Post, $"{NewNodeUrl}/api/task/execute", HttpStatusCode.OK);

            // Act
            var result = await _retryManager.RetryTaskAsync(_testTask, FailedNodeUrl);

            // Assert
            Assert.IsTrue(result);
            _mockNodeRegistry.Verify(nr => nr.RemoveNode(FailedNodeUrl), Times.Exactly(1));
            _mockLoadBalancer.Verify(lb => lb.RemoveNode(FailedNodeUrl), Times.Exactly(1));
            _mockLoadBalancer.Verify(lb => lb.SelectNode(_testTask), Times.Exactly(1));
            VerifyHttpRequest(HttpMethod.Post, $"{NewNodeUrl}/api/task/execute", Times.Exactly(1));
        }

        [TestMethod]
        public async Task RetryTaskAsync_WithMaxRetriesReached_ReturnsFalse()
        {
            // Arrange
            _testTask.CurrentRetryCount = 3; // 已达到最大重试次数

            // Act
            var result = await _retryManager.RetryTaskAsync(_testTask, FailedNodeUrl);

            // Assert
            Assert.IsFalse(result);
            _mockLoadBalancer.Verify(lb => lb.SelectNode(It.IsAny<ScheduledTask>()), Times.Never);
        }

        [TestMethod]
        public async Task RetryTaskAsync_WithRetryDisabled_ReturnsFalse()
        {
            // Arrange
            _testTask.EnableRetry = false;

            // Act
            var result = await _retryManager.RetryTaskAsync(_testTask, FailedNodeUrl);

            // Assert
            Assert.IsFalse(result);
            _mockLoadBalancer.Verify(lb => lb.SelectNode(It.IsAny<ScheduledTask>()), Times.Never);
        }

        [TestMethod]
        public async Task RetryTaskAsync_WithRetryFailure_IncrementsRetryCount()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NewNodeUrl);
            SetupMockHttpResponse(HttpMethod.Post, $"{NewNodeUrl}/api/task/execute", HttpStatusCode.InternalServerError);

            // Act
            var result = await _retryManager.RetryTaskAsync(_testTask, FailedNodeUrl);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, _testTask.CurrentRetryCount);
            Assert.IsNotNull(_testTask.NextRetryTime);
        }

        [TestMethod]
        public void ShouldRetryTask_WithValidTask_ReturnsTrue()
        {
            // Act
            var result = _retryManager.ShouldRetryTask(_testTask);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ShouldRetryTask_WithMaxRetriesReached_ReturnsFalse()
        {
            // Arrange
            _testTask.CurrentRetryCount = 3;

            // Act
            var result = _retryManager.ShouldRetryTask(_testTask);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldRetryTask_WithRetryDisabled_ReturnsFalse()
        {
            // Arrange
            _testTask.EnableRetry = false;

            // Act
            var result = _retryManager.ShouldRetryTask(_testTask);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ShouldRetryTask_WithFutureRetryTime_ReturnsFalse()
        {
            // Arrange
            _testTask.NextRetryTime = DateTime.UtcNow.AddMinutes(5);

            // Act
            var result = _retryManager.ShouldRetryTask(_testTask);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CalculateNextRetryTime_WithExponentialStrategy_ReturnsCorrectTime()
        {
            // Arrange
            _testTask.RetryStrategy = RetryStrategy.Exponential;
            _testTask.RetryIntervalSeconds = 60;
            _testTask.CurrentRetryCount = 1;

            // Act
            var result = _retryManager.CalculateNextRetryTime(_testTask);

            // Assert
            var expectedDelay = 60 * Math.Pow(2, 0); // 第一次重试，指数为0
            var expectedTime = DateTime.UtcNow.AddSeconds(expectedDelay);
            Assert.IsTrue(result >= expectedTime.AddSeconds(-1) && result <= expectedTime.AddSeconds(1));
        }

        [TestMethod]
        public void CalculateNextRetryTime_WithLinearStrategy_ReturnsCorrectTime()
        {
            // Arrange
            _testTask.RetryStrategy = RetryStrategy.Linear;
            _testTask.RetryIntervalSeconds = 60;
            _testTask.CurrentRetryCount = 2;

            // Act
            var result = _retryManager.CalculateNextRetryTime(_testTask);

            // Assert
            var expectedDelay = 60 * 2; // 线性增长
            var expectedTime = DateTime.UtcNow.AddSeconds(expectedDelay);
            Assert.IsTrue(result >= expectedTime.AddSeconds(-1) && result <= expectedTime.AddSeconds(1));
        }

        [TestMethod]
        public void CalculateNextRetryTime_WithFixedStrategy_ReturnsCorrectTime()
        {
            // Arrange
            _testTask.RetryStrategy = RetryStrategy.Fixed;
            _testTask.RetryIntervalSeconds = 60;
            _testTask.CurrentRetryCount = 2;

            // Act
            var result = _retryManager.CalculateNextRetryTime(_testTask);

            // Assert
            var expectedTime = DateTime.UtcNow.AddSeconds(60);
            Assert.IsTrue(result >= expectedTime.AddSeconds(-1) && result <= expectedTime.AddSeconds(1));
        }

        [TestMethod]
        public void ResetRetryState_WithTask_ResetsRetryCountAndNextRetryTime()
        {
            // Arrange
            _testTask.CurrentRetryCount = 2;
            _testTask.NextRetryTime = DateTime.UtcNow.AddMinutes(5);

            // Act
            _retryManager.ResetRetryState(_testTask);

            // Assert
            Assert.AreEqual(0, _testTask.CurrentRetryCount);
            Assert.IsNull(_testTask.NextRetryTime);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryTaskAsync_WithNullTask_ThrowsArgumentNullException()
        {
            // Act
            await _retryManager.RetryTaskAsync(null, FailedNodeUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryTaskAsync_WithNullFailedNode_ThrowsArgumentNullException()
        {
            // Act
            await _retryManager.RetryTaskAsync(_testTask, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryTaskAsync_WithEmptyFailedNode_ThrowsArgumentNullException()
        {
            // Act
            await _retryManager.RetryTaskAsync(_testTask, string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task RetryTaskAsync_WithNoAvailableNodes_ThrowsInvalidOperationException()
        {
            // Arrange
            _retryConfig.EnableRetry = true;
            _testTask.EnableRetry = true;
            _testTask.MaxRetryCount = 3;
            _testTask.CurrentRetryCount = 0;
            _testTask.NextRetryTime = null;

            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask))
                .Callback(() => Console.WriteLine("SelectNode called"))
                .Returns((string)null);

            Console.WriteLine("ShouldRetryTask: " + _retryManager.ShouldRetryTask(_testTask));
            Assert.IsTrue(_retryManager.ShouldRetryTask(_testTask), "ShouldRetryTask 返回 false，测试无法进入 SelectNode");

            // Act
            await _retryManager.RetryTaskAsync(_testTask, FailedNodeUrl);
        }

        private void SetupMockHttpResponse(HttpMethod method, string url, HttpStatusCode statusCode)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == method && req.RequestUri.ToString() == url),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode));
        }

        private void VerifyHttpRequest(HttpMethod method, string url, Times times)
        {
            _mockHttpMessageHandler
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    times,
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == method && req.RequestUri.ToString() == url),
                    ItExpr.IsAny<CancellationToken>());
        }
    }
} 