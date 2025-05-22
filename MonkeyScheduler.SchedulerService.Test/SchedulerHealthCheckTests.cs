using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.SchedulerService;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class SchedulerHealthCheckTests
    {
        private SchedulerHealthCheck _healthCheck;
        private SchedulerHealthCheckMockNodeRegistry _mockNodeRegistry;
        private HealthCheckContext _healthCheckContext;

        [TestInitialize]
        public void Setup()
        {
            _mockNodeRegistry = new SchedulerHealthCheckMockNodeRegistry();
            _healthCheck = new SchedulerHealthCheck(_mockNodeRegistry);
            _healthCheckContext = new HealthCheckContext();
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithNoNodes_ReturnsUnhealthy()
        {
            // Act
            var result = await _healthCheck.CheckHealthAsync(_healthCheckContext);

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("没有注册的任务节点", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithAllHealthyNodes_ReturnsHealthy()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            _mockNodeRegistry.AddNode("http://node2:5000");

            // Act
            var result = await _healthCheck.CheckHealthAsync(_healthCheckContext);

            // Assert
            Assert.AreEqual(HealthStatus.Healthy, result.Status);
            Assert.AreEqual("所有节点心跳正常", result.Description);
            Assert.AreEqual(2, result.Data["total_nodes"]);
            Assert.AreEqual(2, result.Data["healthy_nodes"]);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithSomeUnhealthyNodes_ReturnsDegraded()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            _mockNodeRegistry.AddNode("http://node2:5000");
            
            // 设置一个节点的心跳时间超过超时时间
            _mockNodeRegistry.SetNodeHeartbeatTime("http://node2:5000", DateTime.UtcNow.AddSeconds(-31));

            // Act
            var result = await _healthCheck.CheckHealthAsync(_healthCheckContext);

            // Assert
            Assert.AreEqual(HealthStatus.Degraded, result.Status);
            Assert.IsTrue(result.Description.Contains("以下节点心跳超时"));
            Assert.IsTrue(result.Description.Contains("http://node2:5000"));
            Assert.AreEqual(2, result.Data["total_nodes"]);
            Assert.AreEqual(1, result.Data["healthy_nodes"]);
            Assert.AreEqual("http://node2:5000", result.Data["unhealthy_nodes"]);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithAllUnhealthyNodes_ReturnsDegraded()
        {
            // Arrange
            _mockNodeRegistry.AddNode("http://node1:5000");
            _mockNodeRegistry.AddNode("http://node2:5000");
            
            // 设置所有节点的心跳时间超过超时时间
            _mockNodeRegistry.SetNodeHeartbeatTime("http://node1:5000", DateTime.UtcNow.AddSeconds(-31));
            _mockNodeRegistry.SetNodeHeartbeatTime("http://node2:5000", DateTime.UtcNow.AddSeconds(-31));

            // Act
            var result = await _healthCheck.CheckHealthAsync(_healthCheckContext);

            // Assert
            Assert.AreEqual(HealthStatus.Degraded, result.Status);
            Assert.IsTrue(result.Description.Contains("以下节点心跳超时"));
            Assert.IsTrue(result.Description.Contains("http://node1:5000"));
            Assert.IsTrue(result.Description.Contains("http://node2:5000"));
            Assert.AreEqual(2, result.Data["total_nodes"]);
            Assert.AreEqual(0, result.Data["healthy_nodes"]);
            Assert.IsTrue(result.Data["unhealthy_nodes"].ToString().Contains("http://node1:5000"));
            Assert.IsTrue(result.Data["unhealthy_nodes"].ToString().Contains("http://node2:5000"));
        }
    }

    /// <summary>
    /// 用于测试的模拟节点注册表
    /// </summary>
    public class SchedulerHealthCheckMockNodeRegistry : NodeRegistry
    {
        private readonly ConcurrentDictionary<string, DateTime> _nodes = new();

        public override void Register(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        public override void Heartbeat(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        public override List<string> GetAliveNodes(TimeSpan timeout)
        {
            var now = DateTime.UtcNow;
            return _nodes
                .Where(n => now - n.Value <= timeout)
                .Select(n => n.Key)
                .ToList();
        }

        public override ConcurrentDictionary<string, DateTime> GetAllNodes()
        {
            return _nodes;
        }

        public override void RemoveNode(string nodeUrl)
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

        // 用于测试的辅助方法，设置节点的心跳时间
        public void SetNodeHeartbeatTime(string nodeUrl, DateTime heartbeatTime)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            if (!_nodes.ContainsKey(nodeUrl))
                throw new InvalidOperationException($"节点 {nodeUrl} 不存在");

            _nodes[nodeUrl] = heartbeatTime;
        }
    }
} 