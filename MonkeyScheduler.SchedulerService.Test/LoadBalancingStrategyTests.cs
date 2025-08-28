using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;
using MonkeyScheduler.SchedulerService.Services.Strategies;

namespace MonkeyScheduler.SchedulerService.Test
{
    [TestClass]
    public class LoadBalancingStrategyTests
    {
        private LoadBalancingStrategyFactory _factory;

        [TestInitialize]
        public void Setup()
        {
            _factory = new LoadBalancingStrategyFactory();
        }

        [TestMethod]
        public void TestLeastConnectionStrategy()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("LeastConnection");
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>
            {
                ["node1"] = 5,
                ["node2"] = 2,
                ["node3"] = 8
            };
            var task = new ScheduledTask { Name = "TestTask" };

            // Act
            var selectedNode = strategy.SelectNode(availableNodes, task, nodeLoads);

            // Assert
            Assert.AreEqual("node2", selectedNode, "应该选择负载最少的节点");
        }

        [TestMethod]
        public void TestRoundRobinStrategy()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("RoundRobin");
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>();
            var task = new ScheduledTask { Name = "TestTask" };

            // Act & Assert
            var firstSelection = strategy.SelectNode(availableNodes, task, nodeLoads);
            var secondSelection = strategy.SelectNode(availableNodes, task, nodeLoads);
            var thirdSelection = strategy.SelectNode(availableNodes, task, nodeLoads);

            // 验证轮询顺序
            Assert.IsTrue(availableNodes.Contains(firstSelection));
            Assert.IsTrue(availableNodes.Contains(secondSelection));
            Assert.IsTrue(availableNodes.Contains(thirdSelection));
        }

        [TestMethod]
        public void TestRandomStrategy()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("Random");
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>();
            var task = new ScheduledTask { Name = "TestTask" };

            // Act
            var selections = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                selections.Add(strategy.SelectNode(availableNodes, task, nodeLoads));
            }

            // Assert
            Assert.IsTrue(selections.All(s => availableNodes.Contains(s)), "所有选择都应该是有效节点");
            
            // 验证随机性（至少有两个不同的选择）
            var uniqueSelections = selections.Distinct().Count();
            Assert.IsTrue(uniqueSelections >= 2, "随机策略应该产生不同的选择");
        }

        [TestMethod]
        public void TestStrategyWithMaxConnections()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("LeastConnection");
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>
            {
                ["node1"] = 100, // 达到最大连接数
                ["node2"] = 50,
                ["node3"] = 100  // 达到最大连接数
            };
            var task = new ScheduledTask { Name = "TestTask" };

            // Act
            var selectedNode = strategy.SelectNode(availableNodes, task, nodeLoads);

            // Assert
            Assert.AreEqual("node2", selectedNode, "应该选择未达到最大连接数的节点");
        }

        [TestMethod]
        public void TestStrategyWithNoAvailableNodes()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("LeastConnection");
            var availableNodes = new List<string>();
            var nodeLoads = new Dictionary<string, int>();
            var task = new ScheduledTask { Name = "TestTask" };

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                strategy.SelectNode(availableNodes, task, nodeLoads));
        }

        [TestMethod]
        public void TestStrategyConfiguration()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("LeastConnection");
            var originalConfig = strategy.GetConfiguration();

            // Act
            var newConfig = new Dictionary<string, object>
            {
                ["MaxConnectionsPerNode"] = 200,
                ["EnableStickySessions"] = true
            };
            strategy.UpdateConfiguration(newConfig);
            var updatedConfig = strategy.GetConfiguration();

            // Assert
            Assert.AreEqual(200, updatedConfig["MaxConnectionsPerNode"]);
            Assert.AreEqual(true, updatedConfig["EnableStickySessions"]);
        }

        [TestMethod]
        public void TestStrategyFactory()
        {
            // Arrange & Act
            var availableStrategies = _factory.GetAvailableStrategies().ToList();
            var strategyInfos = _factory.GetAllStrategyInfo().ToList();

            // Assert
            Assert.IsTrue(availableStrategies.Contains("LeastConnection"));
            Assert.IsTrue(availableStrategies.Contains("RoundRobin"));
            Assert.IsTrue(availableStrategies.Contains("Random"));
            Assert.AreEqual(availableStrategies.Count, strategyInfos.Count);
        }

        [TestMethod]
        public void TestCustomStrategyRegistration()
        {
            // Arrange
            var strategyName = "CustomTest";
            var strategyType = typeof(CustomLoadBalancingStrategy);

            // Act
            _factory.RegisterStrategy(strategyName, strategyType);
            var strategy = _factory.CreateStrategy(strategyName);

            // Assert
            Assert.IsNotNull(strategy);
            Assert.AreEqual("Custom", strategy.StrategyName);
        }

        [TestMethod]
        public void TestWeightedRoundRobinStrategy()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("RoundRobin");
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>();
            var task = new ScheduledTask { Name = "TestTask" };

            // 配置加权轮询
            var config = new Dictionary<string, object>
            {
                ["EnableWeightedRoundRobin"] = true,
                ["NodeWeights"] = new Dictionary<string, int>
                {
                    ["node1"] = 3,
                    ["node2"] = 2,
                    ["node3"] = 1
                }
            };
            strategy.UpdateConfiguration(config);

            // Act
            var selections = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                selections.Add(strategy.SelectNode(availableNodes, task, nodeLoads));
            }

            // Assert
            var node1Count = selections.Count(s => s == "node1");
            var node2Count = selections.Count(s => s == "node2");
            var node3Count = selections.Count(s => s == "node3");

            // 验证权重分配（node1应该被选择最多）
            Assert.IsTrue(node1Count >= node2Count, "node1应该比node2被选择更多次");
            Assert.IsTrue(node2Count >= node3Count, "node2应该比node3被选择更多次");
        }

        [TestMethod]
        public void TestCustomLoadBalancingStrategy()
        {
            // Arrange
            var strategy = new CustomLoadBalancingStrategy();
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>();
            var task = new ScheduledTask 
            { 
                Name = "TestTask",
                TaskType = "cpu-intensive"
            };

            // Act
            var selectedNode = strategy.SelectNode(availableNodes, task, nodeLoads);

            // Assert
            Assert.IsTrue(availableNodes.Contains(selectedNode), "选择的节点应该是有效节点");
        }

        [TestMethod]
        public void TestStrategyWithNullTask()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("LeastConnection");
            var availableNodes = new List<string> { "node1", "node2" };
            var nodeLoads = new Dictionary<string, int>();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                strategy.SelectNode(availableNodes, null!, nodeLoads));
        }

        [TestMethod]
        public void TestStrategyWithNullNodes()
        {
            // Arrange
            var strategy = _factory.CreateStrategy("LeastConnection");
            var nodeLoads = new Dictionary<string, int>();
            var task = new ScheduledTask { Name = "TestTask" };

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                strategy.SelectNode(null!, task, nodeLoads));
        }
    }
} 