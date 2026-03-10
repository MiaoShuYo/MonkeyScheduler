using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.WorkerService.Services;
using MonkeyScheduler.WorkerService.Tests.Extensions;
using Moq;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.WorkerService.Tests.Services
{
    [TestClass]
    public class StatusReporterServiceTests
    {
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private StatusReporterService _service;
        private Mock<ILogger<StatusReporterService>> _loggerMock;
        private const string SchedulerUrl = "http://test-scheduler";
        private const string WorkerUrl = "http://test-worker";

        [TestInitialize]
        public void Initialize()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<StatusReporterService>>();

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var workerOptions = new MonkeyScheduler.WorkerService.Options.WorkerOptions
            {
                SchedulerUrl = SchedulerUrl,
                WorkerUrl = WorkerUrl
            };
            _service = new StatusReporterService(
                _httpClientFactoryMock.Object,
                Microsoft.Extensions.Options.Options.Create(workerOptions),
                _loggerMock.Object
            );
        }

        [TestMethod]
        public async Task ReportStatusAsync_SuccessfulReport_SendsCorrectRequest()
        {
            // Arrange
            var result = new TaskExecutionResult
            {
                TaskId = Guid.NewGuid(),
                Status = ExecutionStatus.Completed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Result = "Task completed successfully",
                Success = true
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _httpMessageHandlerMock.SetupSendAsync(response);

            // Act
            await _service.ReportStatusAsync(result);

            // Assert
            _httpMessageHandlerMock.VerifySendAsync(
                $"{SchedulerUrl}/api/tasks/status",
                HttpMethod.Post,
                Times.Once());

            // 验证 WorkerNodeUrl 是否被正确设置
            Assert.AreEqual(WorkerUrl, result.WorkerNodeUrl);
        }

        [TestMethod]
        public async Task ReportStatusAsync_FailedReport_ThrowsException()
        {
            // Arrange
            var result = new TaskExecutionResult
            {
                TaskId = Guid.NewGuid(),
                Status = ExecutionStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ErrorMessage = "Task failed",
                Success = false
            };

            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            _httpMessageHandlerMock.SetupSendAsync(response);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(
                async () => await _service.ReportStatusAsync(result));

            // 验证请求是否被发送
            _httpMessageHandlerMock.VerifySendAsync(
                $"{SchedulerUrl}/api/tasks/status",
                HttpMethod.Post,
                Times.Once());
        }

        [TestMethod]
        public async Task ReportStatusAsync_NetworkError_ThrowsException()
        {
            // Arrange
            var result = new TaskExecutionResult
            {
                TaskId = Guid.NewGuid(),
                Status = ExecutionStatus.Failed,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                ErrorMessage = "Network error",
                Success = false
            };

            var exception = new HttpRequestException("Network error");
            _httpMessageHandlerMock.SetupSendAsync(exception);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<HttpRequestException>(
                async () => await _service.ReportStatusAsync(result));

            // 验证请求是否被发送
            _httpMessageHandlerMock.VerifySendAsync(
                $"{SchedulerUrl}/api/tasks/status",
                HttpMethod.Post,
                Times.Once());
        }
    }
} 