using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MonkeyScheduler.SchedulerService
{
    /// <summary>
    /// 节点注册表
    /// 负责管理所有Worker节点的注册状态和心跳信息
    /// 使用线程安全的并发字典存储节点信息
    /// </summary>
    public class NodeRegistry
    {
        /// <summary>
        /// 存储节点信息的并发字典
        /// Key: 节点URL
        /// Value: 最后一次心跳时间
        /// </summary>
        private readonly ConcurrentDictionary<string, DateTime> _nodes = new();

        /// <summary>
        /// 注册新的Worker节点
        /// </summary>
        /// <param name="nodeUrl">要注册的节点URL</param>
        public virtual void Register(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新节点的最后心跳时间
        /// </summary>
        /// <param name="nodeUrl">发送心跳的节点URL</param>
        public virtual void Heartbeat(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodes[nodeUrl] = DateTime.UtcNow;
        }

        /// <summary>
        /// 获取所有在指定超时时间内有心跳的活跃节点
        /// </summary>
        /// <param name="timeout">心跳超时时间</param>
        /// <returns>活跃节点URL列表</returns>
        public virtual List<string> GetAliveNodes(TimeSpan timeout)
        {
            var now = DateTime.UtcNow;
            return _nodes
                .Where(n => now - n.Value <= timeout)
                .Select(n => n.Key)
                .ToList();
        }

        /// <summary>
        /// 获取所有注册的节点
        /// </summary>
        /// <returns>所有注册节点的URL列表</returns>
        public virtual IEnumerable<string> GetAllNodes()
        {
            return _nodes.Keys;
        }

        /// <summary>
        /// 从注册表中移除指定节点
        /// </summary>
        /// <param name="nodeUrl">要移除的节点URL</param>
        public virtual void RemoveNode(string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
                throw new ArgumentNullException(nameof(nodeUrl));

            _nodes.TryRemove(nodeUrl, out _);
        }
    }
} 