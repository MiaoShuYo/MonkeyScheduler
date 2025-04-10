 #nullable enable
using System;
using Xunit;
using MonkeyScheduler.SchedulerService;

namespace MonkeyScheduler.SchedulerService.Tests
{
    public class NodeRegistryTests
    {
        [Fact]
        public void Register_ShouldAddNodeToRegistry()
        {
            //  Arrange
            var registry = new NodeRegistry();
            var nodeUrl = "http://localhost:5001";

            //  Act
            registry.Register(nodeUrl);

            //  Assert
            var aliveNodes = registry.GetAliveNodes(TimeSpan.FromSeconds(30));
            Assert.Contains(nodeUrl, aliveNodes);
        }

        [Fact]
        public void Register_WithNullOrEmptyUrl_ShouldThrowArgumentNullException()
        {
            //  Arrange
            var registry = new NodeRegistry();
            string? nullUrl = null;
            string emptyUrl = string.Empty;
            string whitespaceUrl = "   ";

            //  Act & Assert
            var ex1 = Assert.Throws<ArgumentNullException>(() => registry.Register(nullUrl!));
            var ex2 = Assert.Throws<ArgumentNullException>(() => registry.Register(emptyUrl));
            var ex3 = Assert.Throws<ArgumentNullException>(() => registry.Register(whitespaceUrl));

            Assert.Equal("nodeUrl", ex1.ParamName);
            Assert.Equal("nodeUrl", ex2.ParamName);
            Assert.Equal("nodeUrl", ex3.ParamName);
        }

        [Fact]
        public void Heartbeat_ShouldUpdateLastHeartbeatTime()
        {
            //  Arrange
            var registry = new NodeRegistry();
            var nodeUrl = "http://localhost:5001";
            registry.Register(nodeUrl);

            //  Act
            System.Threading.Thread.Sleep(500);
            registry.Heartbeat(nodeUrl);

            //  Assert
            var aliveNodes = registry.GetAliveNodes(TimeSpan.FromSeconds(30));
            Assert.Contains(nodeUrl, aliveNodes);
        }

        [Fact]
        public void Heartbeat_WithNullOrEmptyUrl_ShouldThrowArgumentNullException()
        {
            //  Arrange
            var registry = new NodeRegistry();
            string? nullUrl = null;
            string emptyUrl = string.Empty;
            string whitespaceUrl = "   ";

            //  Act & Assert
            var ex1 = Assert.Throws<ArgumentNullException>(() => registry.Heartbeat(nullUrl!));
            var ex2 = Assert.Throws<ArgumentNullException>(() => registry.Heartbeat(emptyUrl));
            var ex3 = Assert.Throws<ArgumentNullException>(() => registry.Heartbeat(whitespaceUrl));

            Assert.Equal("nodeUrl", ex1.ParamName);
            Assert.Equal("nodeUrl", ex2.ParamName);
            Assert.Equal("nodeUrl", ex3.ParamName);
        }

        [Fact]
        public void GetAliveNodes_ShouldReturnOnlyAliveNodes()
        {
            //  Arrange
            var registry = new NodeRegistry();
            var nodeUrl1 = "http://localhost:5001";
            var nodeUrl2 = "http://localhost:5002";
            registry.Register(nodeUrl1);
            registry.Register(nodeUrl2);

            //  Act
            registry.Heartbeat(nodeUrl1);
            System.Threading.Thread.Sleep(500);

            //  Assert
            var aliveNodes = registry.GetAliveNodes(TimeSpan.FromMilliseconds(100));
            Assert.DoesNotContain(nodeUrl1, aliveNodes);
            Assert.DoesNotContain(nodeUrl2, aliveNodes);
        }

        [Fact]
        public void RemoveNode_ShouldRemoveNodeFromRegistry()
        {
            //  Arrange
            var registry = new NodeRegistry();
            var nodeUrl = "http://localhost:5001";
            registry.Register(nodeUrl);

            //  Act
            registry.RemoveNode(nodeUrl);

            //  Assert
            var aliveNodes = registry.GetAliveNodes(TimeSpan.FromSeconds(30));
            Assert.DoesNotContain(nodeUrl, aliveNodes);
        }

        [Fact]
        public void RemoveNode_WithNullOrEmptyUrl_ShouldThrowArgumentNullException()
        {
            //  Arrange
            var registry = new NodeRegistry();
            string? nullUrl = null;
            string emptyUrl = string.Empty;
            string whitespaceUrl = "   ";

            //  Act & Assert
            var ex1 = Assert.Throws<ArgumentNullException>(() => registry.RemoveNode(nullUrl!));
            var ex2 = Assert.Throws<ArgumentNullException>(() => registry.RemoveNode(emptyUrl));
            var ex3 = Assert.Throws<ArgumentNullException>(() => registry.RemoveNode(whitespaceUrl));

            Assert.Equal("nodeUrl", ex1.ParamName);
            Assert.Equal("nodeUrl", ex2.ParamName);
            Assert.Equal("nodeUrl", ex3.ParamName);
        }

        [Fact]
        public void RemoveNode_ShouldNotThrowExceptionForNonExistentNode()
        {
            //  Arrange
            var registry = new NodeRegistry();
            var nodeUrl = "http://localhost:5001";

            //  Act & Assert
            var exception = Record.Exception(() => registry.RemoveNode(nodeUrl));
            Assert.Null(exception);
        }
    }
}