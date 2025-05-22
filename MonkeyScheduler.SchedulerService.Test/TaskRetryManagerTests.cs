using System.Net;
using Moq;
using Moq.Protected;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class TaskRetryManagerTests
    {
        private Mock<INodeRegistry> _mockNodeRegistry;
        private Mock<ILoadBalancer> _mockLoadBalancer;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private TaskRetryManager _taskRetryManager;
        private ScheduledTask _testTask;
        private const string FailedNodeUrl = "http://failed-node:5000";
        private const string NewNodeUrl = "http://new-node:5000";

        [TestInitialize]
        public void Setup()
        {
            _mockNodeRegistry = new Mock<INodeRegistry>();
            _mockLoadBalancer = new Mock<ILoadBalancer>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _taskRetryManager = new TaskRetryManager(_mockNodeRegistry.Object, _mockLoadBalancer.Object, _httpClient);
            
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
        public async Task RetryTaskAsync_WithValidTaskAndFailedNode_RetriesOnNewNode()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NewNodeUrl);
            
            SetupMockHttpResponse(HttpMethod.Post, $"{NewNodeUrl}/api/task/execute", HttpStatusCode.OK);

            // Act
            await _taskRetryManager.RetryTaskAsync(_testTask, FailedNodeUrl);

            // Assert
            _mockNodeRegistry.Verify(nr => nr.RemoveNode(FailedNodeUrl), Times.Exactly(1));
            _mockLoadBalancer.Verify(lb => lb.RemoveNode(FailedNodeUrl), Times.Exactly(1));
            _mockLoadBalancer.Verify(lb => lb.SelectNode(_testTask), Times.Exactly(1));
            VerifyHttpRequest(HttpMethod.Post, $"{NewNodeUrl}/api/task/execute", Times.Exactly(1));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryTaskAsync_WithNullTask_ThrowsArgumentNullException()
        {
            // Act
            await _taskRetryManager.RetryTaskAsync(null, FailedNodeUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryTaskAsync_WithNullFailedNode_ThrowsArgumentNullException()
        {
            // Act
            await _taskRetryManager.RetryTaskAsync(_testTask, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryTaskAsync_WithEmptyFailedNode_ThrowsArgumentNullException()
        {
            // Act
            await _taskRetryManager.RetryTaskAsync(_testTask, string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryTaskAsync_WithWhitespaceFailedNode_ThrowsArgumentNullException()
        {
            // Act
            await _taskRetryManager.RetryTaskAsync(_testTask, "   ");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task RetryTaskAsync_WithNoAvailableNodes_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask))
                .Throws(new InvalidOperationException("No available nodes"));

            // Act
            await _taskRetryManager.RetryTaskAsync(_testTask, FailedNodeUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task RetryTaskAsync_WhenNewNodeFails_ThrowsHttpRequestException()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(NewNodeUrl);
            
            SetupMockHttpResponse(HttpMethod.Post, $"{NewNodeUrl}/api/task/execute", HttpStatusCode.InternalServerError);

            // Act
            await _taskRetryManager.RetryTaskAsync(_testTask, FailedNodeUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task RetryTaskAsync_WhenNoNodeAvailable_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockLoadBalancer.Setup(lb => lb.SelectNode(_testTask)).Returns(string.Empty);

            // Act
            await _taskRetryManager.RetryTaskAsync(_testTask, FailedNodeUrl);
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