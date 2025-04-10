using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Moq.Protected;
using System.Net;
using MonkeyScheduler.WorkerService.Services;

namespace MonkeyScheduler.WorkerService.Tests
{
    /// <summary>
    /// NodeHeartbeatService类的单元测试
    /// </summary>
    public class NodeHeartbeatServiceTests
    {
        /// <summary>
        /// 测试心跳服务 - 成功注册和发送心跳
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_ShouldRegisterAndSendHeartbeat()
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
                    StatusCode = HttpStatusCode.OK
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var schedulerUrl = "http://localhost:5000";
            var workerUrl = "http://localhost:5001";
            var service = new NodeHeartbeatService(mockHttpClientFactory.Object, schedulerUrl, workerUrl);

            //  Act
            // 启动服务并等待一段时间，让它有机会发送心跳
            var cts = new CancellationTokenSource();
            var task = service.StartAsync(cts.Token);
            
            // 等待足够长的时间，让服务有机会发送心跳
            await Task.Delay(100);
            
            // 停止服务
            await service.StopAsync(cts.Token);
            cts.Cancel();

            //  Assert
            // 验证HTTP客户端工厂被调用
            mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.AtLeast(1));
            
            // 验证HTTP请求被发送
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeast(2), // 至少一次注册和一次心跳
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri != null &&
                    (req.RequestUri.ToString().Contains("/api/worker/register") || 
                     req.RequestUri.ToString().Contains("/api/worker/heartbeat"))),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        /// <summary>
        /// 测试心跳服务 - 处理HTTP请求失败
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
            var workerUrl = "http://localhost:5001";
            var service = new NodeHeartbeatService(mockHttpClientFactory.Object, schedulerUrl, workerUrl);

            //  Act
            // 启动服务并等待一段时间，让它有机会发送心跳
            var cts = new CancellationTokenSource();
            var task = service.StartAsync(cts.Token);
            
            // 等待足够长的时间，让服务有机会发送心跳
            await Task.Delay(100);
            
            // 停止服务
            await service.StopAsync(cts.Token);
            cts.Cancel();

            //  Assert
            // 验证HTTP客户端工厂被调用
            mockHttpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.AtLeast(1));
            
            // 验证HTTP请求被发送
            mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.AtLeast(1), // 至少尝试了一次请求
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri != null &&
                    (req.RequestUri.ToString().Contains("/api/worker/register") || 
                     req.RequestUri.ToString().Contains("/api/worker/heartbeat"))),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
} 