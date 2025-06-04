using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.WorkerService.Services;
using MonkeyScheduler.WorkerService.Tests.Extensions;
using Moq.Protected;

namespace MonkeyScheduler.WorkerService.Tests.Services
{
    [TestClass]
    public class NodeHeartbeatServiceTests
    {
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Mock<ILogger<NodeHeartbeatService>> _loggerMock;
        private NodeHeartbeatService _service;
        private const string SchedulerUrl = "http://test-scheduler";
        private const string WorkerUrl = "http://test-worker";

        [TestInitialize]
        public void Initialize()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<NodeHeartbeatService>>();

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            _service = new NodeHeartbeatService(
                _httpClientFactoryMock.Object,
                SchedulerUrl,
                WorkerUrl,
                _loggerMock.Object);
        }

        [TestMethod]
        public async Task ExecuteAsync_RegistersNodeAndSendsHeartbeats()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var registerResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var heartbeatResponse = new HttpResponseMessage(HttpStatusCode.OK);

            _httpMessageHandlerMock.SetupSendAsync(registerResponse);
            _httpMessageHandlerMock.SetupSendAsync(heartbeatResponse);

            // Act
            var executeTask = _service.StartAsync(cts.Token);
            
            // 等待一段时间让服务执行
            await Task.Delay(500);
            
            // 取消服务
            cts.Cancel();
            await executeTask;

            // Assert
            _httpMessageHandlerMock.VerifySendAsync(
                $"{SchedulerUrl}/api/worker/register",
                HttpMethod.Post,
                Times.Once());

            _httpMessageHandlerMock.VerifySendAsync(
                $"{SchedulerUrl}/api/worker/heartbeat",
                HttpMethod.Post,
                Times.AtLeastOnce());
        }

        [TestMethod]
        public async Task ExecuteAsync_RetriesRegistrationOnFailure()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var heartbeatResponse = new HttpResponseMessage(HttpStatusCode.OK);

            // 设置第一次注册失败，第二次成功
            _httpMessageHandlerMock.SetupSequenceSendAsync(
                errorResponse,
                successResponse,
                heartbeatResponse);

            // Act
            var executeTask = _service.StartAsync(cts.Token);
            
            // 等待一段时间让服务执行
            await Task.Delay(2000);
            
            // 取消服务
            cts.Cancel();
            await executeTask;

            // Assert
            _httpMessageHandlerMock.VerifySendAsync(
                $"{SchedulerUrl}/api/worker/register",
                HttpMethod.Post,
                Times.Exactly(2));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("节点注册失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());
        }

        [TestMethod]
        public async Task ExecuteAsync_RetriesHeartbeatOnFailure()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var registerResponse = new HttpResponseMessage(HttpStatusCode.OK);
            var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

            // 设置注册成功，第一次心跳失败，第二次成功
            _httpMessageHandlerMock.SetupSequenceSendAsync(
                registerResponse,
                errorResponse,
                successResponse);

            // Act
            var executeTask = _service.StartAsync(cts.Token);
            
            // 等待一段时间让服务执行
            await Task.Delay(2000);
            
            // 取消服务
            cts.Cancel();
            await executeTask;

            // Assert
            _httpMessageHandlerMock.VerifySendAsync(
                $"{SchedulerUrl}/api/worker/heartbeat",
                HttpMethod.Post,
                Times.Exactly(2));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("心跳发送失败")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once());
        }

        [TestMethod]
        public async Task ExecuteAsync_StopsAfterMaxRetries()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var testException = new HttpRequestException("Test error");

            // 设置HTTP处理器模拟连续失败
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => throw testException);

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<Exception>(
                async () =>
                {
                    // 使用反射调用私有方法
                    var method = typeof(NodeHeartbeatService).GetMethod("RegisterNodeWithRetryAsync",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    await (Task)method!.Invoke(_service, new object[] { httpClient, cts.Token })!;
                }
            );
            
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(5),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri.ToString().Contains("/api/worker/register")),
                ItExpr.IsAny<CancellationToken>()
            );

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("节点注册失败，已达到最大重试次数")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_HeartbeatFailsAfterMaxRetries()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var testException = new HttpRequestException("心跳发送失败，已达到最大重试次数");

            // 设置HTTP处理器模拟心跳失败
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/api/worker/heartbeat")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => throw testException);

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Act & Assert
            var exception = await Assert.ThrowsExceptionAsync<Exception>(
                async () =>
                {
                    // 使用反射调用私有方法
                    var method = typeof(NodeHeartbeatService).GetMethod("SendHeartbeatWithRetryAsync",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    await (Task)method!.Invoke(_service, new object[] { httpClient, cts.Token })!;
                }
            );

            Assert.AreEqual("心跳发送失败，已达到最大重试次数", exception.Message);
            
            // 验证心跳请求失败5次
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(5),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.RequestUri.ToString().Contains("/api/worker/heartbeat")),
                ItExpr.IsAny<CancellationToken>()
            );

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("心跳发送失败，已达到最大重试次数")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }

        [TestMethod]
        public async Task ExecuteAsync_LogsHeartbeatErrorAndContinues()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var testException = new HttpRequestException("Heartbeat error");

            // 设置HTTP处理器模拟注册成功但心跳失败
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/api/worker/register")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // 设置心跳请求连续失败6次
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("/api/worker/heartbeat")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => throw testException);

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Act
            var exception = await Assert.ThrowsExceptionAsync<Exception>(
                async () =>
                {
                    // 使用反射调用私有方法
                    var method = typeof(NodeHeartbeatService).GetMethod("ExecuteAsync",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    await (Task)method!.Invoke(_service, new object[] { cts.Token })!;
                }
            );

            // Assert
            Assert.AreEqual("心跳发送失败", exception.Message);

            // 验证警告日志
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("心跳发送失败，尝试次数")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce
            );

            // 验证错误日志
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("心跳发送失败:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce
            );
        }
    }
} 