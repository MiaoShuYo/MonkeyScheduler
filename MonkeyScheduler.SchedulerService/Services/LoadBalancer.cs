using System;
using System.Collections.Generic;
using System.Linq;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 负载均衡器，负责在多个Worker节点之间分配任务
    /// 支持动态扩容和节点负载均衡
    /// </summary>
    public class LoadBalancer
    {
        private readonly NodeRegistry _nodeRegistry;
        private readonly Dictionary<string, int> _nodeLoad = new();
        private readonly object _lock = new();

        /// <summary>
        /// 初始化负载均衡器
        /// </summary>
        /// <param name="nodeRegistry">节点注册表，用于获取可用节点信息</param>
        public LoadBalancer(NodeRegistry nodeRegistry)
        {
            _nodeRegistry = nodeRegistry;
        }

        /// <summary>
        /// 选择最适合执行任务的节点
        /// 基于节点当前负载情况进行选择
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <returns>选中的节点URL</returns>
        /// <exception cref="InvalidOperationException">当没有可用节点时抛出</exception>
        public string SelectNode(ScheduledTask task)
        {
            // 获取所有存活的节点
            var nodes = _nodeRegistry.GetAliveNodes(TimeSpan.FromSeconds(30));
            if (!nodes.Any())
            {
                throw new InvalidOperationException("没有可用的Worker节点");
            }

            lock (_lock)
            {
                // 更新节点负载信息：添加新节点
                foreach (var node in nodes)
                {
                    if (!_nodeLoad.ContainsKey(node))
                    {
                        _nodeLoad[node] = 0;
                    }
                }

                // 更新节点负载信息：移除已不存在的节点
                var removedNodes = _nodeLoad.Keys.Except(nodes).ToList();
                foreach (var node in removedNodes)
                {
                    _nodeLoad.Remove(node);
                }

                // 选择当前负载最轻的节点
                var selectedNode = _nodeLoad.OrderBy(x => x.Value).First().Key;
                // 增加选中节点的负载计数
                _nodeLoad[selectedNode]++;

                return selectedNode;
            }
        }

        /// <summary>
        /// 减少指定节点的负载计数
        /// 通常在任务执行完成或失败时调用
        /// </summary>
        /// <param name="nodeUrl">节点URL</param>
        public void DecreaseLoad(string nodeUrl)
        {
            lock (_lock)
            {
                if (_nodeLoad.ContainsKey(nodeUrl))
                {
                    // 确保负载计数不会小于0
                    _nodeLoad[nodeUrl] = Math.Max(0, _nodeLoad[nodeUrl] - 1);
                }
            }
        }
    }
} 