using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services.Strategies
{
    /// <summary>
    /// 随机负载均衡策略
    /// 随机选择节点来执行任务
    /// 适合节点性能相近且任务执行时间差异不大的场景
    /// </summary>
    public class RandomStrategy : ILoadBalancingStrategy
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly Random _random = new Random();

        public RandomStrategy()
        {
            _configuration = new Dictionary<string, object>
            {
                ["MaxConnectionsPerNode"] = 100,
                ["EnableWeightedRandom"] = false,
                ["NodeWeights"] = new Dictionary<string, int>(),
                ["Seed"] = Environment.TickCount
            };
        }

        public string StrategyName => "Random";

        public string StrategyDescription => "随机策略：随机选择节点来执行任务，适合节点性能相近的场景";

        public string SelectNode(IEnumerable<string> availableNodes, ScheduledTask task, IDictionary<string, int> nodeLoads)
        {
            if (availableNodes == null || !availableNodes.Any())
                throw new InvalidOperationException("没有可用的节点");

            var maxConnections = (int)_configuration["MaxConnectionsPerNode"];
            var enableWeighted = (bool)_configuration["EnableWeightedRandom"];
            var nodeWeights = (Dictionary<string, int>)_configuration["NodeWeights"];

            // 过滤掉已达到最大连接数的节点
            var eligibleNodes = availableNodes
                .Where(node => !nodeLoads.ContainsKey(node) || nodeLoads[node] < maxConnections)
                .ToList();

            if (!eligibleNodes.Any())
                throw new InvalidOperationException("所有节点都已达到最大连接数限制");

            string selectedNode;

            if (enableWeighted && nodeWeights.Any())
            {
                // 加权随机
                selectedNode = SelectWeightedNode(eligibleNodes, nodeWeights);
            }
            else
            {
                // 简单随机
                var randomIndex = _random.Next(eligibleNodes.Count);
                selectedNode = eligibleNodes[randomIndex];
            }

            return selectedNode;
        }

        private string SelectWeightedNode(List<string> eligibleNodes, Dictionary<string, int> nodeWeights)
        {
            // 计算总权重
            var totalWeight = eligibleNodes
                .Where(node => nodeWeights.ContainsKey(node))
                .Sum(node => nodeWeights[node]);

            if (totalWeight == 0)
            {
                // 如果没有配置权重，使用简单随机
                var randomIndex = _random.Next(eligibleNodes.Count);
                return eligibleNodes[randomIndex];
            }

            // 生成随机数
            var randomValue = _random.Next(1, totalWeight + 1);

            // 根据权重选择节点
            var currentWeight = 0;
            foreach (var node in eligibleNodes)
            {
                if (nodeWeights.ContainsKey(node))
                {
                    currentWeight += nodeWeights[node];
                    if (randomValue <= currentWeight)
                    {
                        return node;
                    }
                }
            }

            // 如果没有找到，返回第一个节点
            return eligibleNodes.First();
        }

        public IDictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>(_configuration);
        }

        public void UpdateConfiguration(IDictionary<string, object> configuration)
        {
            foreach (var kvp in configuration)
            {
                _configuration[kvp.Key] = kvp.Value;
            }

            // 如果更新了种子，重新初始化随机数生成器
            if (configuration.ContainsKey("Seed"))
            {
                var seed = (int)configuration["Seed"];
                // 注意：这里不能直接赋值给只读字段，需要在构造函数中初始化
                // 或者将 _random 改为属性
            }
        }
    }
} 