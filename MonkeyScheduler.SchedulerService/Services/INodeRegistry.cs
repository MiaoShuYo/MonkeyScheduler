 using System;
using System.Collections.Generic;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 节点注册表接口
    /// </summary>
    public interface INodeRegistry
    {
        /// <summary>
        /// 注册新的Worker节点
        /// </summary>
        /// <param name="nodeUrl">要注册的节点URL</param>
        void Register(string nodeUrl);

        /// <summary>
        /// 更新节点的最后心跳时间
        /// </summary>
        /// <param name="nodeUrl">发送心跳的节点URL</param>
        void Heartbeat(string nodeUrl);

        /// <summary>
        /// 获取所有在指定超时时间内有心跳的活跃节点
        /// </summary>
        /// <param name="timeout">心跳超时时间</param>
        /// <returns>活跃节点URL列表</returns>
        List<string> GetAliveNodes(TimeSpan timeout);

        /// <summary>
        /// 获取所有注册的节点
        /// </summary>
        /// <returns>所有注册节点的URL列表</returns>
        IEnumerable<string> GetAllNodes();

        /// <summary>
        /// 从注册表中移除指定节点
        /// </summary>
        /// <param name="nodeUrl">要移除的节点URL</param>
        void RemoveNode(string nodeUrl);
    }
}