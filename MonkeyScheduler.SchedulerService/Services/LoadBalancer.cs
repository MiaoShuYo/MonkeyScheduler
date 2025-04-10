using System;
using System.Collections.Concurrent;
using System.Linq;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 负载均衡器，负责在多个Worker节点之间分配任务
    /// 支持动态扩容和节点负载均衡，确保任务均匀分布
    /// 使用简单的轮询算法进行负载均衡
    /// </summary>
    public class LoadBalancer : ILoadBalancer
    {
        /// <summary>
        /// 节点注册表，用于获取当前可用的Worker节点列表
        /// </summary>
        private readonly INodeRegistry _nodeRegistry;

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
        public LoadBalancer(INodeRegistry nodeRegistry)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
        }

        /// <summary>
        /// 选择最适合执行任务的节点
        /// 基于节点当前负载情况进行选择，优先选择负载较轻的节点
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <returns>选中的节点URL</returns>
        /// <exception cref="InvalidOperationException">当没有可用节点时抛出</exception>
        public virtual string SelectNode(ScheduledTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            var nodes = _nodeRegistry.GetAllNodes().ToList();
            if (!nodes.Any())
                throw new InvalidOperationException("没有可用的Worker节点");

            // 使用最少任务数的节点
            var selectedNode = nodes.OrderBy(n => _nodeTasks.GetOrAdd(n, 0)).First();
            _nodeTasks.AddOrUpdate(selectedNode, 1, (_, count) => count + 1);

            return selectedNode;
        }

        /// <summary>
        /// 减少指定节点的负载计数
        /// 通常在任务执行完成或失败时调用
        /// 确保负载计数的准确性
        /// </summary>
        /// <param name="nodeUrl">要减少负载的节点URL</param>
        public virtual void DecreaseLoad(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodeTasks.AddOrUpdate(nodeUrl, 0, (_, count) => Math.Max(0, count - 1));
        }

        /// <summary>
        /// 从负载均衡器中移除节点
        /// 通常在节点故障或下线时调用
        /// </summary>
        /// <param name="nodeUrl">要移除的节点URL</param>
        public virtual void RemoveNode(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodeTasks.TryRemove(nodeUrl, out _);
        }
    }
}