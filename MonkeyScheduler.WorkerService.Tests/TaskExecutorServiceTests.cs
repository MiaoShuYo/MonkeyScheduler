using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Moq.Protected;
using System.Net;
using MonkeyScheduler.WorkerService.Services;
using System.Text.Json;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;

namespace MonkeyScheduler.WorkerService.Tests
{
    /// <summary>
    /// DefaultTaskExecutor类的单元测试
    /// </summary>
    public class TaskExecutorServiceTests
    {
        /// <summary>
        /// 测试任务执行服务 - 成功执行任务
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldExecuteTaskSuccessfully()
        {
            //  Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("OK")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var service = new DefaultTaskExecutor(mockHttpClientFactory.Object, schedulerUrl);

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            //  Act
            await service.ExecuteAsync(task);

            //  Assert
            // 验证HTTP客户端工厂被调用
            mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.AtLeast(1));
            
            // 验证HTTP请求被发送
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeast(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/api/task/execute")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// 测试任务执行服务 - 处理HTTP请求失败
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldHandleHttpRequestFailure()
        {
            //  Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("模拟网络错误"));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var service = new DefaultTaskExecutor(mockHttpClientFactory.Object, schedulerUrl);

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            //  Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => service.ExecuteAsync(task));

            // 验证HTTP客户端工厂被调用
            mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.AtLeast(1));
            
            // 验证HTTP请求被发送
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeast(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/api/task/execute")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// 测试任务执行服务 - 处理任务执行失败
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldHandleTaskExecutionFailure()
        {
            //  Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(JsonSerializer.Serialize(new { error = "任务执行失败" }))
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var service = new DefaultTaskExecutor(mockHttpClientFactory.Object, schedulerUrl);

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            //  Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => service.ExecuteAsync(task));

            // 验证HTTP客户端工厂被调用
            mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.AtLeast(1));
            
            // 验证HTTP请求被发送
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeast(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/api/task/execute")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// 测试任务执行服务 - 任务执行完成后发送回调通知
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldSendCallbackAfterTaskCompletion()
        {
            //  Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var requestCount = 0;
            
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    requestCount++;
                    if (request.Method == HttpMethod.Post && request.RequestUri != null && 
                        request.RequestUri.ToString().Contains("/api/task/execute"))
                    {
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(JsonSerializer.Serialize(new { success = true }))
                        };
                    }
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var service = new DefaultTaskExecutor(mockHttpClientFactory.Object, schedulerUrl);

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            var callbackCalled = false;
            Func<TaskExecutionResult, Task> onCompleted = async result =>
            {
                callbackCalled = true;
                await Task.CompletedTask;
            };

            //  Act
            await service.ExecuteAsync(task, onCompleted);

            //  Assert
            Assert.True(callbackCalled);
            Assert.True(requestCount >= 1);
            
            // 验证HTTP请求被正确发送
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeast(1),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/api/task/execute")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// 测试任务执行服务 - 服务器返回非200状态码
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldHandleNon200Status()
        {
            //  Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("服务器内部错误")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var service = new DefaultTaskExecutor(mockHttpClientFactory.Object, schedulerUrl);

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            //  Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => service.ExecuteAsync(task));
            Assert.Contains("500", exception.Message);
        }

        /// <summary>
        /// 测试任务执行服务 - 回调函数为null的情况
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldHandleNullCallback()
        {
            //  Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("OK")
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var service = new DefaultTaskExecutor(mockHttpClientFactory.Object, schedulerUrl);

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            //  Act & Assert
            // 不应抛出异常
            await service.ExecuteAsync(task, null);

            // 验证HTTP请求被正确发送
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/api/task/execute")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// 测试任务执行服务 - HTTP请求超时
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldHandleTimeout()
        {
            //  Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Returns<HttpRequestMessage, CancellationToken>((request, token) =>
                    Task.Delay(TimeSpan.FromSeconds(5), token)
                        .ContinueWith<HttpResponseMessage>(_ => 
                            throw new TaskCanceledException()));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var service = new DefaultTaskExecutor(mockHttpClientFactory.Object, schedulerUrl);

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            //  Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => service.ExecuteAsync(task));
        }

        /// <summary>
        /// 测试任务执行服务 - 服务器返回非200状态码但响应体格式正确
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldHandleNon200StatusWithValidResponse()
        {
            //  Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var errorResponse = new { success = false, error = "服务器内部错误" };
            
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(JsonSerializer.Serialize(errorResponse))
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var service = new DefaultTaskExecutor(mockHttpClientFactory.Object, schedulerUrl);

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "TestTask",
                CronExpression = "*/5 * * * * *",
                NextRunTime = DateTime.UtcNow,
                Enabled = true
            };

            //  Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => service.ExecuteAsync(task));
            Assert.Contains("500", exception.Message);
        }
    }
} 