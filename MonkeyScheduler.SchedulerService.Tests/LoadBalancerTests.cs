#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Moq;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService.Tests
{
    public class LoadBalancerTests
    {
        private readonly Mock<INodeRegistry> _mockNodeRegistry;
        private readonly LoadBalancer _loadBalancer;
        private readonly ScheduledTask _testTask;

        public LoadBalancerTests()
        {
            _mockNodeRegistry = new Mock<INodeRegistry>();
            _loadBalancer = new LoadBalancer(_mockNodeRegistry.Object);
            _testTask = new ScheduledTask { Name = "TestTask" };
        }

        [Fact]
        public void SelectNode_WithSingleNode_ShouldReturnThatNode()
        {
            // Arrange
            var nodeUrl = "http://localhost:5001";
            _mockNodeRegistry.Setup(r => r.GetAllNodes())
                          .Returns(new[] { nodeUrl });

            // Act
            var selectedNode = _loadBalancer.SelectNode(_testTask);

            // Assert
            Assert.Equal(nodeUrl, selectedNode);
            _mockNodeRegistry.Verify(r => r.GetAllNodes(), Times.Once);
        }

        [Fact]
        public void SelectNode_WithMultipleNodes_ShouldSelectLeastLoadedNode()
        {
            // Arrange
            var nodeUrl1 = "http://localhost:5001";
            var nodeUrl2 = "http://localhost:5002";
            _mockNodeRegistry.Setup(r => r.GetAllNodes())
                          .Returns(new[] { nodeUrl1, nodeUrl2 });

            // Act
            var firstSelectedNode = _loadBalancer.SelectNode(_testTask);
            var secondSelectedNode = _loadBalancer.SelectNode(_testTask);
            _loadBalancer.DecreaseLoad(nodeUrl1); // 减少第一个节点的负载
            var thirdSelectedNode = _loadBalancer.SelectNode(_testTask);

            // Assert
            Assert.Equal(nodeUrl1, firstSelectedNode);
            Assert.Equal(nodeUrl2, secondSelectedNode);
            Assert.Equal(nodeUrl1, thirdSelectedNode);
            _mockNodeRegistry.Verify(r => r.GetAllNodes(), Times.Exactly(3));
        }

        [Fact]
        public void SelectNode_WithNoNodes_ShouldThrowException()
        {
            // Arrange
            _mockNodeRegistry.Setup(r => r.GetAllNodes())
                          .Returns(Enumerable.Empty<string>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _loadBalancer.SelectNode(_testTask));
            Assert.Equal("没有可用的Worker节点", exception.Message);
            _mockNodeRegistry.Verify(r => r.GetAllNodes(), Times.Once);
        }

        [Fact]
        public void DecreaseLoad_ShouldDecreaseNodeLoad()
        {
            // Arrange
            var nodeUrl = "http://localhost:5001";
            _mockNodeRegistry.Setup(r => r.GetAllNodes())
                          .Returns(new[] { nodeUrl });

            // Act
            var selectedNode = _loadBalancer.SelectNode(_testTask);
            Assert.Equal(nodeUrl, selectedNode);
            
            _loadBalancer.DecreaseLoad(nodeUrl);
            
            var selectedNodeAgain = _loadBalancer.SelectNode(_testTask);

            // Assert
            Assert.Equal(nodeUrl, selectedNodeAgain);
            _mockNodeRegistry.Verify(r => r.GetAllNodes(), Times.Exactly(2));
        }

        [Fact]
        public void DecreaseLoad_WithNonExistentNode_ShouldNotThrowException()
        {
            // Arrange
            var nonExistentNodeUrl = "http://localhost:9999";

            // Act & Assert
            var exception = Record.Exception(() => _loadBalancer.DecreaseLoad(nonExistentNodeUrl));
            Assert.Null(exception);
        }

        [Fact]
        public void SelectNode_WithNullTask_ShouldThrowArgumentNullException()
        {
            // Arrange
            ScheduledTask? nullTask = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => _loadBalancer.SelectNode(nullTask!));
            Assert.Equal("task", ex.ParamName);
        }

        [Fact]
        public void DecreaseLoad_WithNullOrEmptyUrl_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? nullUrl = null;
            string emptyUrl = string.Empty;
            string whitespaceUrl = "   ";

            // Act & Assert
            var ex1 = Assert.Throws<ArgumentNullException>(() => _loadBalancer.DecreaseLoad(nullUrl!));
            var ex2 = Assert.Throws<ArgumentNullException>(() => _loadBalancer.DecreaseLoad(emptyUrl));
            var ex3 = Assert.Throws<ArgumentNullException>(() => _loadBalancer.DecreaseLoad(whitespaceUrl));

            Assert.Equal("nodeUrl", ex1.ParamName);
            Assert.Equal("nodeUrl", ex2.ParamName);
            Assert.Equal("nodeUrl", ex3.ParamName);
        }
    }
}