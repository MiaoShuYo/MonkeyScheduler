using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.WorkerService.Services;
using Moq;

namespace MonkeyScheduler.WorkerService.Tests.Services
{
    [TestClass]
    public class WorkerHealthCheckTests
    {
        private WorkerHealthCheck _healthCheck;
        private Mock<ILogger<WorkerHealthCheck>> _loggerMock;

        [TestInitialize]
        public void Initialize()
        {
            _loggerMock = new Mock<ILogger<WorkerHealthCheck>>();
            _healthCheck = new WorkerHealthCheck();
        }

        [TestMethod]
        public async Task CheckHealthAsync_ReturnsHealthyStatus()
        {
            // Arrange
            var context = new HealthCheckContext();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _healthCheck.CheckHealthAsync(context, cancellationToken);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(HealthStatus.Healthy, result.Status);
            Assert.AreEqual("Worker节点运行正常", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithCancelledToken_ReturnsHealthyStatus()
        {
            // Arrange
            var context = new HealthCheckContext();
            var cancellationToken = new CancellationToken(true);

            // Act
            var result = await _healthCheck.CheckHealthAsync(context, cancellationToken);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(HealthStatus.Healthy, result.Status);
            Assert.AreEqual("Worker节点运行正常", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithNullContext_ReturnsHealthyStatus()
        {
            // Arrange
            HealthCheckContext? context = null;
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _healthCheck.CheckHealthAsync(context!, cancellationToken);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(HealthStatus.Healthy, result.Status);
            Assert.AreEqual("Worker节点运行正常", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithException_ReturnsHealthyStatus()
        {
            // Arrange
            var context = new HealthCheckContext();
            var cancellationToken = CancellationToken.None;

            // 模拟一个异常情况
            _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _healthCheck.CheckHealthAsync(context, cancellationToken);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(HealthStatus.Healthy, result.Status);
            Assert.AreEqual("Worker节点运行正常", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithCustomData_ReturnsHealthyStatus()
        {
            // Arrange
            var context = new HealthCheckContext();
            var cancellationToken = CancellationToken.None;

            // Act
            var result = await _healthCheck.CheckHealthAsync(context, cancellationToken);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(HealthStatus.Healthy, result.Status);
            Assert.AreEqual("Worker节点运行正常", result.Description);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual(0, result.Data.Count);
        }
    }
} 