using System.Collections.Concurrent;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 负载均衡器，负责在多个Worker节点之间分配任务。
    /// 支持动态扩容和节点负载均衡，确保任务均匀分布。
    /// 使用可插拔的负载均衡策略。
    /// </summary>
    public class LoadBalancer : ILoadBalancer
    {
        /// <summary>
        /// 节点注册表，用于获取当前可用的Worker节点列表
        /// </summary>
        private readonly INodeRegistry _nodeRegistry;

        /// <summary>
        /// 负载均衡策略
        /// </summary>
        private readonly ILoadBalancingStrategy _strategy;

        /// <summary>
        /// 节点负载计数器
        /// Key: 节点URL
        /// Value: 当前正在执行的任务数量
        /// </summary>
        private readonly ConcurrentDictionary<string, int> _nodeTasks = new();

        /// <summary>
        /// 初始化负载均衡器
        /// </summary>
        /// <param name="nodeRegistry">节点注册表，用于获取可用节点信息</param>
        /// <param name="strategy">负载均衡策略</param>
        public LoadBalancer(INodeRegistry nodeRegistry, ILoadBalancingStrategy strategy)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>
        /// 选择最适合执行任务的节点。
        /// 使用配置的负载均衡策略来选择节点。
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <returns>选中的节点URL</returns>
        /// <exception cref="InvalidOperationException">当没有可用节点时抛出</exception>
        /// <remarks>
        /// 1. 获取所有可用节点，支持动态扩容。
        /// 2. 通过策略接口选择节点，便于扩展。
        /// 3. 选中节点后自动增加其负载计数。
        /// </remarks>
        public virtual string SelectNode(ScheduledTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var nodes = _nodeRegistry.GetAllNodes().ToList();
            if (!nodes.Any())
                throw new InvalidOperationException("没有可用的Worker节点");

            // 获取可用节点列表
            var availableNodes = nodes.Select(n => n.Key).ToList();
            
            // 使用策略选择节点
            var selectedNode = _strategy.SelectNode(availableNodes, task, _nodeTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            
            // 增加选中节点的负载计数
            _nodeTasks.AddOrUpdate(selectedNode, 1, (_, count) => count + 1);

            return selectedNode;
        }

        /// <summary>
        /// 减少指定节点的负载计数。
        /// 通常在任务执行完成或失败时调用。
        /// 确保负载计数的准确性。
        /// </summary>
        /// <param name="nodeUrl">要减少负载的节点URL</param>
        public virtual void DecreaseLoad(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodeTasks.AddOrUpdate(nodeUrl, 0, (_, count) => Math.Max(0, count - 1));
        }

        /// <summary>
        /// 从负载均衡器中移除节点。
        /// 通常在节点故障或下线时调用。
        /// </summary>
        /// <param name="nodeUrl">要移除的节点URL</param>
        public virtual void RemoveNode(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            // 从节点任务计数器中移除
            _nodeTasks.TryRemove(nodeUrl, out _);
            
            // 从节点注册表中移除
            _nodeRegistry.RemoveNode(nodeUrl);
        }

        /// <summary>
        /// 添加新节点到负载均衡器。
        /// </summary>
        /// <param name="nodeUrl">新节点URL</param>
        public virtual void AddNode(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            // 如果节点已经存在，则不添加
            if (!_nodeTasks.ContainsKey(nodeUrl))
            {
                _nodeTasks.TryAdd(nodeUrl, 0);
            }
        }

        /// <summary>
        /// 获取当前负载均衡策略信息
        /// </summary>
        /// <returns>策略信息</returns>
        public object GetStrategyInfo()
        {
            return new
            {
                Name = _strategy.StrategyName,
                Description = _strategy.StrategyDescription,
                Configuration = _strategy.GetConfiguration()
            };
        }

        /// <summary>
        /// 更新负载均衡策略配置
        /// </summary>
        /// <param name="configuration">新的配置</param>
        public void UpdateStrategyConfiguration(IDictionary<string, object> configuration)
        {
            _strategy.UpdateConfiguration(configuration);
        }

        /// <summary>
        /// 获取节点负载信息
        /// </summary>
        /// <returns>节点负载字典</returns>
        public IDictionary<string, int> GetNodeLoads()
        {
            return _nodeTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}