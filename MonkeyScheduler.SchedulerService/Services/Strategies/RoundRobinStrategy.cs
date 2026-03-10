using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services.Strategies
{
    /// <summary>
    /// 轮询负载均衡策略
    /// 按顺序轮流选择节点来执行任务
    /// 适合节点性能相近的场景
    /// </summary>
    public class RoundRobinStrategy : ILoadBalancingStrategy
    {
        private readonly Dictionary<string, object> _configuration;
        private int _currentIndex = 0;
        private readonly object _lockObject = new object();
        private readonly Dictionary<string, Dictionary<string, int>> _nodeWeightCounters = new Dictionary<string, Dictionary<string, int>>();

        public RoundRobinStrategy()
        {
            _configuration = new Dictionary<string, object>
            {
                ["EnableWeightedRoundRobin"] = false,
                ["NodeWeights"] = new Dictionary<string, int>(),
                ["MaxConnectionsPerNode"] = 100
            };
        }

        public string StrategyName => "RoundRobin";

        public string StrategyDescription => "轮询策略：按顺序轮流选择节点来执行任务，确保任务均匀分布";

        public string SelectNode(IEnumerable<string> availableNodes, ScheduledTask task, IDictionary<string, int> nodeLoads)
        {
            if (availableNodes == null || !availableNodes.Any())
                throw new InvalidOperationException("没有可用的节点");

            var maxConnections = (int)_configuration["MaxConnectionsPerNode"];
            var enableWeighted = (bool)_configuration["EnableWeightedRoundRobin"];
            var nodeWeights = (Dictionary<string, int>)_configuration["NodeWeights"];

            // 过滤掉已达到最大连接数的节点
            var eligibleNodes = availableNodes
                .Where(node => !nodeLoads.ContainsKey(node) || nodeLoads[node] < maxConnections)
                .ToList();

            if (!eligibleNodes.Any())
                throw new InvalidOperationException("所有节点都已达到最大连接数限制");

            string selectedNode;

            lock (_lockObject)
            {
                if (enableWeighted && nodeWeights.Any())
                {
                    // 加权轮询
                    selectedNode = SelectWeightedNode(eligibleNodes, nodeWeights);
                }
                else
                {
                    // 简单轮询
                    selectedNode = eligibleNodes[_currentIndex % eligibleNodes.Count];
                    _currentIndex = (_currentIndex + 1) % eligibleNodes.Count;
                }
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
                // 如果没有配置权重，使用简单轮询
                var selectedNode = eligibleNodes[_currentIndex % eligibleNodes.Count];
                _currentIndex = (_currentIndex + 1) % eligibleNodes.Count;
                return selectedNode;
            }

            // 使用确定性加权轮询算法
            // 维护每个节点的当前权重计数器
            if (!_nodeWeightCounters.ContainsKey("weighted"))
            {
                _nodeWeightCounters["weighted"] = new Dictionary<string, int>();
            }

            var weightCounters = _nodeWeightCounters["weighted"];

            // 初始化权重计数器
            foreach (var node in eligibleNodes)
            {
                if (!weightCounters.ContainsKey(node))
                {
                    weightCounters[node] = 0;
                }
            }

            // 选择权重最高的节点
            var weightedSelectedNode = eligibleNodes
                .Where(node => nodeWeights.ContainsKey(node))
                .OrderByDescending(node => nodeWeights[node] - weightCounters[node])
                .First();

            // 更新选中节点的权重计数器
            weightCounters[weightedSelectedNode] += totalWeight;

            return weightedSelectedNode;
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
        }
    }
} 