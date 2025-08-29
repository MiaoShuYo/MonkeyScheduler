using System.Collections.Concurrent;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class LoadBalancerTests
    {
        private LoadBalancer _loadBalancer;
        private LoadBalancerMockNodeRegistry _mockNodeRegistry;
        private ScheduledTask _testTask;

        [TestInitialize]
        public void Setup()
        {
            _mockNodeRegistry = new LoadBalancerMockNodeRegistry();
            _loadBalancer = new LoadBalancer(_mockNodeRegistry, new MonkeyScheduler.SchedulerService.Services.Strategies.LeastConnectionStrategy());
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
        public void SelectNode_WithAvailableNodes_ReturnsNodeUrl()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            _mockNodeRegistry.AddNode("http://node2:5000");

            // Act
            string selectedNode = _loadBalancer.SelectNode(_testTask);

            // Assert
            Assert.IsNotNull(selectedNode);
            Assert.IsTrue(selectedNode == "http://node1:5000" || selectedNode == "http://node2:5000");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SelectNode_WithNoAvailableNodes_ThrowsInvalidOperationException()
        {
            // Act
            _loadBalancer.SelectNode(_testTask);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void SelectNode_WithNullTask_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.SelectNode(null);
        }

        [TestMethod]
        public void SelectNode_WithMultipleNodes_SelectsLeastLoadedNode()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            _mockNodeRegistry.AddNode("http://node2:5000");
            _mockNodeRegistry.AddNode("http://node3:5000");

            // Act
            // 第一次选择
            string firstNode = _loadBalancer.SelectNode(_testTask);
            
            // 第二次选择
            string secondNode = _loadBalancer.SelectNode(_testTask);
            
            // 第三次选择
            string thirdNode = _loadBalancer.SelectNode(_testTask);
            
            // 第四次选择 - 应该选择负载最小的节点
            string fourthNode = _loadBalancer.SelectNode(_testTask);

            // Assert
            // 验证第四次选择的是负载最小的节点
            Assert.AreEqual(firstNode, fourthNode);
        }

        [TestMethod]
        public void DecreaseLoad_ValidNodeUrl_DecreasesLoadCount()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            string selectedNode = _loadBalancer.SelectNode(_testTask);
            
            // Act
            _loadBalancer.DecreaseLoad(selectedNode);
            
            // 再次选择节点，应该选择同一个节点
            string secondSelectedNode = _loadBalancer.SelectNode(_testTask);
            
            // Assert
            Assert.AreEqual(selectedNode, secondSelectedNode);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DecreaseLoad_NullNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.DecreaseLoad(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DecreaseLoad_EmptyNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.DecreaseLoad("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DecreaseLoad_WhitespaceNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.DecreaseLoad("   ");
        }

        [TestMethod]
        public void RemoveNode_ValidNodeUrl_RemovesNode()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            _mockNodeRegistry.AddNode("http://node2:5000");
            
            // Act
            _loadBalancer.RemoveNode("http://node1:5000");
            
            // 再次选择节点，不应该选择已移除的节点
            string selectedNode = _loadBalancer.SelectNode(_testTask);
            
            // Assert
            Assert.AreEqual("http://node2:5000", selectedNode);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RemoveNode_NullNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.RemoveNode(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RemoveNode_EmptyNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.RemoveNode("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RemoveNode_WhitespaceNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.RemoveNode("   ");
        }

        [TestMethod]
        public void AddNode_ValidNodeUrl_AddsNode()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            
            // Act
            _loadBalancer.AddNode("http://node2:5000");
            
            // 选择节点，应该能够选择新添加的节点
            string selectedNode = _loadBalancer.SelectNode(_testTask);
            
            // Assert
            Assert.IsTrue(selectedNode == "http://node1:5000" || selectedNode == "http://node2:5000");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNode_NullNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.AddNode(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNode_EmptyNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.AddNode("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNode_WhitespaceNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _loadBalancer.AddNode("   ");
        }

        [TestMethod]
        public void AddNode_DuplicateNodeUrl_DoesNotAddDuplicate()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            
            // Act
            _loadBalancer.AddNode("http://node1:5000");
            
            // 选择节点，应该只返回一个节点
            string selectedNode = _loadBalancer.SelectNode(_testTask);
            
            // Assert
            Assert.AreEqual("http://node1:5000", selectedNode);
        }
    }

    /// <summary>
    /// 用于测试的模拟节点注册表
    /// </summary>
    public class LoadBalancerMockNodeRegistry : INodeRegistry
    {
        private readonly ConcurrentDictionary<string, DateTime> _nodes = new();

        public void Register(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        public void Heartbeat(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        public List<string> GetAliveNodes(TimeSpan timeout)
        {
            var now = DateTime.UtcNow;
            return _nodes
                .Where(n => now - n.Value <= timeout)
                .Select(n => n.Key)
                .ToList();
        }

        public ConcurrentDictionary<string, DateTime> GetAllNodes()
        {
            return _nodes;
        }

        public void RemoveNode(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodes.TryRemove(nodeUrl, out _);
        }

        // 用于测试的辅助方法
        public void AddNode(string nodeUrl)
        {
            Register(nodeUrl);
        }
    }
} 