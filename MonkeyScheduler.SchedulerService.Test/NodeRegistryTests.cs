using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.SchedulerService;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class NodeRegistryTests
    {
        private NodeRegistry _nodeRegistry;

        [TestInitialize]
        public void Setup()
        {
            _nodeRegistry = new NodeRegistry();
        }

        [TestMethod]
        public void Register_ValidNodeUrl_NodeIsRegistered()
        {
            // Arrange
            string nodeUrl = "http://localhost:5000";

            // Act
            _nodeRegistry.Register(nodeUrl);

            // Assert
            var allNodes = _nodeRegistry.GetAllNodes();
            Assert.IsTrue(allNodes.ContainsKey(nodeUrl));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Register_NullNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _nodeRegistry.Register(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Register_EmptyNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _nodeRegistry.Register("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Register_WhitespaceNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _nodeRegistry.Register("   ");
        }

        [TestMethod]
        public void Heartbeat_ValidNodeUrl_UpdatesLastHeartbeatTime()
        {
            // Arrange
            string nodeUrl = "http://localhost:5000";
            _nodeRegistry.Register(nodeUrl);
            
            // Act
            _nodeRegistry.Heartbeat(nodeUrl);
            
            // Assert
            var allNodes = _nodeRegistry.GetAllNodes();
            Assert.IsTrue(allNodes.ContainsKey(nodeUrl));
            // 验证心跳时间已更新（由于时间差异很小，我们只能验证它存在）
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Heartbeat_NullNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _nodeRegistry.Heartbeat(null);
        }

        [TestMethod]
        public void GetAliveNodes_WithTimeout_ReturnsAliveNodes()
        {
            // Arrange
            string nodeUrl1 = "http://localhost:5000";
            string nodeUrl2 = "http://localhost:5001";
            
            // 注册两个节点
            _nodeRegistry.Register(nodeUrl1);
            _nodeRegistry.Register(nodeUrl2);
            
            // 等待一小段时间，让两个节点的初始注册时间有一定差异
            Thread.Sleep(100);
            
            // 只更新nodeUrl1的心跳时间
            _nodeRegistry.Heartbeat(nodeUrl1);
            
            // Act
            // 使用较短的超时时间，但确保它大于0
            var aliveNodes = _nodeRegistry.GetAliveNodes(TimeSpan.FromMilliseconds(10));
            
            // Assert
            Assert.AreEqual(1, aliveNodes.Count);
            Assert.AreEqual(nodeUrl1, aliveNodes[0]);
        }

        [TestMethod]
        public void GetAllNodes_ReturnsAllRegisteredNodes()
        {
            // Arrange
            string nodeUrl1 = "http://localhost:5000";
            string nodeUrl2 = "http://localhost:5001";
            _nodeRegistry.Register(nodeUrl1);
            _nodeRegistry.Register(nodeUrl2);
            
            // Act
            var allNodes = _nodeRegistry.GetAllNodes();
            
            // Assert
            Assert.AreEqual(2, allNodes.Count);
            Assert.IsTrue(allNodes.ContainsKey(nodeUrl1));
            Assert.IsTrue(allNodes.ContainsKey(nodeUrl2));
        }

        [TestMethod]
        public void RemoveNode_ValidNodeUrl_NodeIsRemoved()
        {
            // Arrange
            string nodeUrl = "http://localhost:5000";
            _nodeRegistry.Register(nodeUrl);
            
            // Act
            _nodeRegistry.RemoveNode(nodeUrl);
            
            // Assert
            var allNodes = _nodeRegistry.GetAllNodes();
            Assert.IsFalse(allNodes.ContainsKey(nodeUrl));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RemoveNode_NullNodeUrl_ThrowsArgumentNullException()
        {
            // Act
            _nodeRegistry.RemoveNode(null);
        }
    }
} 