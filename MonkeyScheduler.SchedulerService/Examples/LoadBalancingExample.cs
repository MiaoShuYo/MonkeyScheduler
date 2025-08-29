using System.Collections.Concurrent;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.SchedulerService.Services;
using MonkeyScheduler.SchedulerService.Services.Strategies;

namespace MonkeyScheduler.SchedulerService.Examples
{
    /// <summary>
    /// 负载均衡策略使用示例
    /// 展示如何使用不同的负载均衡策略
    /// </summary>
    public class LoadBalancingExample
    {
        /// <summary>
        /// 演示最少连接数策略
        /// </summary>
        public static void DemonstrateLeastConnectionStrategy()
        {
            Console.WriteLine("=== 最少连接数策略演示 ===");
            
            var strategy = new LeastConnectionStrategy();
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>
            {
                ["node1"] = 5,
                ["node2"] = 2,
                ["node3"] = 8
            };

            for (int i = 0; i < 5; i++)
            {
                var task = new ScheduledTask { Name = $"Task{i + 1}" };
                var selectedNode = strategy.SelectNode(availableNodes, task, nodeLoads);
                
                // 模拟任务分配
                nodeLoads[selectedNode]++;
                
                Console.WriteLine($"任务 {task.Name} 分配给 {selectedNode}，当前负载: {nodeLoads[selectedNode]}");
            }
        }

        /// <summary>
        /// 演示轮询策略
        /// </summary>
        public static void DemonstrateRoundRobinStrategy()
        {
            Console.WriteLine("\n=== 轮询策略演示 ===");
            
            var strategy = new RoundRobinStrategy();
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>();

            for (int i = 0; i < 6; i++)
            {
                var task = new ScheduledTask { Name = $"Task{i + 1}" };
                var selectedNode = strategy.SelectNode(availableNodes, task, nodeLoads);
                
                Console.WriteLine($"任务 {task.Name} 分配给 {selectedNode}");
            }
        }

        /// <summary>
        /// 演示加权轮询策略
        /// </summary>
        public static void DemonstrateWeightedRoundRobinStrategy()
        {
            Console.WriteLine("\n=== 加权轮询策略演示 ===");
            
            var strategy = new RoundRobinStrategy();
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>();

            // 配置加权轮询
            var config = new Dictionary<string, object>
            {
                ["EnableWeightedRoundRobin"] = true,
                ["NodeWeights"] = new Dictionary<string, int>
                {
                    ["node1"] = 3,  // 高性能节点
                    ["node2"] = 2,  // 中等性能节点
                    ["node3"] = 1   // 低性能节点
                }
            };
            strategy.UpdateConfiguration(config);

            var selections = new List<string>();
            for (int i = 0; i < 12; i++)
            {
                var task = new ScheduledTask { Name = $"Task{i + 1}" };
                var selectedNode = strategy.SelectNode(availableNodes, task, nodeLoads);
                selections.Add(selectedNode);
                
                Console.WriteLine($"任务 {task.Name} 分配给 {selectedNode}");
            }

            // 统计分配结果
            var node1Count = selections.Count(s => s == "node1");
            var node2Count = selections.Count(s => s == "node2");
            var node3Count = selections.Count(s => s == "node3");

            Console.WriteLine($"\n分配统计:");
            Console.WriteLine($"node1 (权重3): {node1Count} 次");
            Console.WriteLine($"node2 (权重2): {node2Count} 次");
            Console.WriteLine($"node3 (权重1): {node3Count} 次");
        }

        /// <summary>
        /// 演示随机策略
        /// </summary>
        public static void DemonstrateRandomStrategy()
        {
            Console.WriteLine("\n=== 随机策略演示 ===");
            
            var strategy = new RandomStrategy();
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>();

            var selections = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                var task = new ScheduledTask { Name = $"Task{i + 1}" };
                var selectedNode = strategy.SelectNode(availableNodes, task, nodeLoads);
                selections.Add(selectedNode);
                
                Console.WriteLine($"任务 {task.Name} 分配给 {selectedNode}");
            }

            // 统计分配结果
            var node1Count = selections.Count(s => s == "node1");
            var node2Count = selections.Count(s => s == "node2");
            var node3Count = selections.Count(s => s == "node3");

            Console.WriteLine($"\n分配统计:");
            Console.WriteLine($"node1: {node1Count} 次");
            Console.WriteLine($"node2: {node2Count} 次");
            Console.WriteLine($"node3: {node3Count} 次");
        }

        /// <summary>
        /// 演示自定义策略
        /// </summary>
        public static void DemonstrateCustomStrategy()
        {
            Console.WriteLine("\n=== 自定义策略演示 ===");
            
            var strategy = new CustomLoadBalancingStrategy();
            var availableNodes = new List<string> { "node1", "node2", "node3" };
            var nodeLoads = new Dictionary<string, int>();

            // 测试不同类型的任务
            var taskTypes = new[] { "cpu-intensive", "io-intensive", "memory-intensive", "general" };

            foreach (var taskType in taskTypes)
            {
                var task = new ScheduledTask 
                { 
                    Name = $"Task-{taskType}",
                    TaskType = taskType
                };
                
                var selectedNode = strategy.SelectNode(availableNodes, task, nodeLoads);
                Console.WriteLine($"任务 {task.Name} 分配给 {selectedNode}");
            }
        }

        /// <summary>
        /// 演示策略工厂
        /// </summary>
        public static void DemonstrateStrategyFactory()
        {
            Console.WriteLine("\n=== 策略工厂演示 ===");
            
            var factory = new LoadBalancingStrategyFactory();
            
            // 获取可用策略
            var availableStrategies = factory.GetAvailableStrategies();
            Console.WriteLine("可用策略:");
            foreach (var strategyName in availableStrategies)
            {
                Console.WriteLine($"- {strategyName}");
            }

            // 获取策略信息
            Console.WriteLine("\n策略详细信息:");
            var strategyInfos = factory.GetAllStrategyInfo();
            foreach (var infoObj in strategyInfos)
            {
                var info = infoObj as StrategyInfo ?? new StrategyInfo
                {
                    Name = (string)infoObj.GetType().GetProperty("Name")?.GetValue(infoObj),
                    Description = (string)infoObj.GetType().GetProperty("Description")?.GetValue(infoObj),
                    Configuration = (IDictionary<string, object>)infoObj.GetType().GetProperty("Configuration")?.GetValue(infoObj)
                };
                Console.WriteLine($"策略: {info.Name}");
                Console.WriteLine($"描述: {info.Description}");
                Console.WriteLine("配置:");
                foreach (var kvp in info.Configuration)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine();
            }

            // 注册自定义策略
            factory.RegisterStrategy("CustomExample", typeof(CustomLoadBalancingStrategy));
            Console.WriteLine("已注册自定义策略: CustomExample");

            // 创建策略实例
            var customStrategy = factory.CreateStrategy("CustomExample");
            Console.WriteLine($"创建的自定义策略: {customStrategy.StrategyName}");
        }

        /// <summary>
        /// 演示负载均衡器
        /// </summary>
        public static void DemonstrateLoadBalancer()
        {
            Console.WriteLine("\n=== 负载均衡器演示 ===");
            
            // 创建模拟的节点注册表
            var nodeRegistry = new MockNodeRegistry();
            nodeRegistry.Register("node1");
            nodeRegistry.Register("node2");
            nodeRegistry.Register("node3");

            // 创建负载均衡器
            var strategy = new LeastConnectionStrategy();
            var loadBalancer = new LoadBalancer(nodeRegistry, strategy);

            // 模拟任务分配
            for (int i = 0; i < 5; i++)
            {
                var task = new ScheduledTask { Name = $"Task{i + 1}" };
                var selectedNode = loadBalancer.SelectNode(task);
                
                Console.WriteLine($"任务 {task.Name} 分配给 {selectedNode}");
                
                // 模拟任务完成
                loadBalancer.DecreaseLoad(selectedNode);
            }

            // 获取负载信息
            var nodeLoads = loadBalancer.GetNodeLoads();
            Console.WriteLine("\n节点负载信息:");
            foreach (var kvp in nodeLoads)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }

            // 获取策略信息
            var strategyInfoObj = loadBalancer.GetStrategyInfo();
            var strategyInfo = strategyInfoObj as StrategyInfo ?? new StrategyInfo
            {
                Name = (string)strategyInfoObj.GetType().GetProperty("Name")?.GetValue(strategyInfoObj),
                Description = (string)strategyInfoObj.GetType().GetProperty("Description")?.GetValue(strategyInfoObj),
                Configuration = (IDictionary<string, object>)strategyInfoObj.GetType().GetProperty("Configuration")?.GetValue(strategyInfoObj)
            };
            Console.WriteLine($"\n当前策略: {strategyInfo.Name}");
        }

        /// <summary>
        /// 运行所有演示
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("负载均衡策略系统演示");
            Console.WriteLine("======================");

            DemonstrateLeastConnectionStrategy();
            DemonstrateRoundRobinStrategy();
            DemonstrateWeightedRoundRobinStrategy();
            DemonstrateRandomStrategy();
            DemonstrateCustomStrategy();
            DemonstrateStrategyFactory();
            DemonstrateLoadBalancer();

            Console.WriteLine("\n演示完成！");
        }
    }

    /// <summary>
    /// 模拟节点注册表
    /// </summary>
    public class MockNodeRegistry : INodeRegistry
    {
        private readonly Dictionary<string, DateTime> _nodes = new();

        public void Register(string nodeUrl)
        {
            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        public void Heartbeat(string nodeUrl)
        {
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
            return new ConcurrentDictionary<string, DateTime>(_nodes);
        }

        public void RemoveNode(string nodeUrl)
        {
            _nodes.Remove(nodeUrl);
        }
    }

    public class StrategyInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IDictionary<string, object> Configuration { get; set; }
    }
} 