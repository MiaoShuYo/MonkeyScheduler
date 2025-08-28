using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services.Strategies
{
    /// <summary>
    /// 最少连接数负载均衡策略
    /// 选择当前连接数最少的节点来执行任务
    /// 这是默认的负载均衡策略，适合大多数场景
    /// </summary>
    public class LeastConnectionStrategy : ILoadBalancingStrategy
    {
        private readonly Dictionary<string, object> _configuration;

        public LeastConnectionStrategy()
        {
            _configuration = new Dictionary<string, object>
            {
                ["MaxConnectionsPerNode"] = 100,
                ["EnableStickySessions"] = false,
                ["StickySessionTimeout"] = 300
            };
        }

        public string StrategyName => "LeastConnection";

        public string StrategyDescription => "最少连接数策略：选择当前连接数最少的节点来执行任务，确保负载均匀分布";

        public string SelectNode(IEnumerable<string> availableNodes, ScheduledTask task, IDictionary<string, int> nodeLoads)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            if (availableNodes == null || !availableNodes.Any())
                throw new InvalidOperationException("没有可用的节点");

            var maxConnections = (int)_configuration["MaxConnectionsPerNode"];
            
            // 过滤掉已达到最大连接数的节点
            var eligibleNodes = availableNodes
                .Where(node => !nodeLoads.ContainsKey(node) || nodeLoads[node] < maxConnections)
                .ToList();

            if (!eligibleNodes.Any())
                throw new InvalidOperationException("所有节点都已达到最大连接数限制");

            // 选择连接数最少的节点
            var selectedNode = eligibleNodes
                .OrderBy(node => nodeLoads.ContainsKey(node) ? nodeLoads[node] : 0)
                .First();

            return selectedNode;
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