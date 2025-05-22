using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MonkeyScheduler.SchedulerService.Controllers;
using MonkeyScheduler.SchedulerService;

namespace MonkeyScheduler.SchedulerService.Test.Controllers
{
    [TestClass]
    public class WorkerApiControllerTests
    {
        private Mock<NodeRegistry> _mockNodeRegistry;
        private WorkerApiController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockNodeRegistry = new Mock<NodeRegistry>();
            _controller = new WorkerApiController(_mockNodeRegistry.Object);
        }

        [TestMethod]
        public void Register_WithValidNodeUrl_ReturnsOkResult()
        {
            // Arrange
            var nodeUrl = "http://localhost:5000";

            // Act
            var result = _controller.Register(nodeUrl);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkResult));
            _mockNodeRegistry.Verify(x => x.Register(nodeUrl), Times.Once);
        }

        [TestMethod]
        public void Register_WithNullNodeUrl_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Register(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
            _mockNodeRegistry.Verify(x => x.Register(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Register_WithEmptyNodeUrl_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Register(string.Empty);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
            _mockNodeRegistry.Verify(x => x.Register(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Register_WithWhitespaceNodeUrl_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Register("   ");

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
            _mockNodeRegistry.Verify(x => x.Register(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Heartbeat_WithValidNodeUrl_ReturnsOkResult()
        {
            // Arrange
            var nodeUrl = "http://localhost:5000";

            // Act
            var result = _controller.Heartbeat(nodeUrl);

            // Assert
            Assert.IsInstanceOfType(result, typeof(OkResult));
            _mockNodeRegistry.Verify(x => x.Heartbeat(nodeUrl), Times.Once);
        }

        [TestMethod]
        public void Heartbeat_WithNullNodeUrl_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Heartbeat(null);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
            _mockNodeRegistry.Verify(x => x.Heartbeat(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Heartbeat_WithEmptyNodeUrl_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Heartbeat(string.Empty);

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
            _mockNodeRegistry.Verify(x => x.Heartbeat(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Heartbeat_WithWhitespaceNodeUrl_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Heartbeat("   ");

            // Assert
            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
            _mockNodeRegistry.Verify(x => x.Heartbeat(It.IsAny<string>()), Times.Never);
        }
    }
} 