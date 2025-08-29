using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services.Strategies
{
    /// <summary>
    /// 自定义负载均衡策略示例
    /// 演示如何实现自定义的负载均衡策略
    /// 此策略根据任务类型和节点性能进行智能选择
    /// </summary>
    public class CustomLoadBalancingStrategy : ILoadBalancingStrategy
    {
        private readonly Dictionary<string, object> _configuration;
        private readonly Dictionary<string, NodePerformance> _nodePerformance;

        public CustomLoadBalancingStrategy()
        {
            _configuration = new Dictionary<string, object>
            {
                ["EnableTaskTypeRouting"] = true,
                ["EnablePerformanceBasedSelection"] = true,
                ["MaxConnectionsPerNode"] = 100,
                ["PerformanceWeight"] = 0.7,
                ["LoadWeight"] = 0.3
            };

            _nodePerformance = new Dictionary<string, NodePerformance>();
        }

        public string StrategyName => "Custom";

        public string StrategyDescription => "自定义策略：根据任务类型和节点性能进行智能选择，支持任务类型路由和性能权重";

        public string SelectNode(IEnumerable<string> availableNodes, ScheduledTask task, IDictionary<string, int> nodeLoads)
        {
            if (availableNodes == null || !availableNodes.Any())
                throw new InvalidOperationException("没有可用的节点");

            var maxConnections = (int)_configuration["MaxConnectionsPerNode"];
            var enableTaskTypeRouting = (bool)_configuration["EnableTaskTypeRouting"];
            var enablePerformanceBasedSelection = (bool)_configuration["EnablePerformanceBasedSelection"];

            // 过滤掉已达到最大连接数的节点
            var eligibleNodes = availableNodes
                .Where(node => !nodeLoads.ContainsKey(node) || nodeLoads[node] < maxConnections)
                .ToList();

            if (!eligibleNodes.Any())
                throw new InvalidOperationException("所有节点都已达到最大连接数限制");

            // 如果只有一个节点，直接返回
            if (eligibleNodes.Count == 1)
                return eligibleNodes.First();

            string selectedNode;

            if (enableTaskTypeRouting && !string.IsNullOrEmpty(task.TaskType))
            {
                // 根据任务类型选择节点
                selectedNode = SelectNodeByTaskType(eligibleNodes, task, nodeLoads);
            }
            else if (enablePerformanceBasedSelection)
            {
                // 根据性能权重选择节点
                selectedNode = SelectNodeByPerformance(eligibleNodes, nodeLoads);
            }
            else
            {
                // 使用最少连接数策略作为后备
                selectedNode = eligibleNodes
                    .OrderBy(node => nodeLoads.ContainsKey(node) ? nodeLoads[node] : 0)
                    .First();
            }

            return selectedNode;
        }

        /// <summary>
        /// 根据任务类型选择节点
        /// </summary>
        private string SelectNodeByTaskType(List<string> eligibleNodes, ScheduledTask task, IDictionary<string, int> nodeLoads)
        {
            // 这里可以根据任务类型和节点的能力进行匹配
            // 例如：某些节点专门处理CPU密集型任务，某些节点处理IO密集型任务
            
            switch (task.TaskType.ToLower())
            {
                case "cpu-intensive":
                    // 选择CPU性能最好的节点
                    return SelectBestPerformanceNode(eligibleNodes, "cpu");
                    
                case "io-intensive":
                    // 选择IO性能最好的节点
                    return SelectBestPerformanceNode(eligibleNodes, "io");
                    
                case "memory-intensive":
                    // 选择内存性能最好的节点
                    return SelectBestPerformanceNode(eligibleNodes, "memory");
                    
                default:
                    // 默认使用性能权重选择
                    return SelectNodeByPerformance(eligibleNodes, nodeLoads);
            }
        }

        /// <summary>
        /// 根据性能权重选择节点
        /// </summary>
        private string SelectNodeByPerformance(List<string> eligibleNodes, IDictionary<string, int> nodeLoads)
        {
            var performanceWeight = (double)_configuration["PerformanceWeight"];
            var loadWeight = (double)_configuration["LoadWeight"];

            var nodeScores = eligibleNodes.Select(node =>
            {
                var performance = GetNodePerformance(node);
                var load = nodeLoads.ContainsKey(node) ? nodeLoads[node] : 0;
                var maxLoad = (int)_configuration["MaxConnectionsPerNode"];

                // 计算性能分数（0-1）
                var performanceScore = performance.GetOverallScore();
                
                // 计算负载分数（0-1，负载越少分数越高）
                var loadScore = 1.0 - (double)load / maxLoad;

                // 综合分数
                var totalScore = performanceScore * performanceWeight + loadScore * loadWeight;

                return new { Node = node, Score = totalScore };
            }).ToList();

            // 选择分数最高的节点
            return nodeScores.OrderByDescending(x => x.Score).First().Node;
        }

        /// <summary>
        /// 选择指定性能类型最好的节点
        /// </summary>
        private string SelectBestPerformanceNode(List<string> eligibleNodes, string performanceType)
        {
            var nodeScores = eligibleNodes.Select(node =>
            {
                var performance = GetNodePerformance(node);
                var score = performanceType switch
                {
                    "cpu" => performance.CpuScore,
                    "io" => performance.IoScore,
                    "memory" => performance.MemoryScore,
                    _ => performance.GetOverallScore()
                };

                return new { Node = node, Score = score };
            }).ToList();

            return nodeScores.OrderByDescending(x => x.Score).First().Node;
        }

        /// <summary>
        /// 获取节点性能信息
        /// </summary>
        private NodePerformance GetNodePerformance(string nodeUrl)
        {
            if (!_nodePerformance.ContainsKey(nodeUrl))
            {
                // 初始化默认性能值
                _nodePerformance[nodeUrl] = new NodePerformance
                {
                    CpuScore = 0.8,
                    IoScore = 0.7,
                    MemoryScore = 0.9,
                    LastUpdated = DateTime.UtcNow
                };
            }

            return _nodePerformance[nodeUrl];
        }

        /// <summary>
        /// 更新节点性能信息
        /// </summary>
        public void UpdateNodePerformance(string nodeUrl, NodePerformance performance)
        {
            _nodePerformance[nodeUrl] = performance;
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

    /// <summary>
    /// 节点性能信息
    /// </summary>
    public class NodePerformance
    {
        /// <summary>
        /// CPU性能分数（0-1）
        /// </summary>
        public double CpuScore { get; set; } = 0.8;

        /// <summary>
        /// IO性能分数（0-1）
        /// </summary>
        public double IoScore { get; set; } = 0.7;

        /// <summary>
        /// 内存性能分数（0-1）
        /// </summary>
        public double MemoryScore { get; set; } = 0.9;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 获取综合性能分数
        /// </summary>
        /// <returns>综合性能分数</returns>
        public double GetOverallScore()
        {
            return (CpuScore + IoScore + MemoryScore) / 3.0;
        }
    }
} 