using System.Collections.Concurrent;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 节点注册表接口，用于管理 Worker 节点的注册、心跳、查询和移除等操作
    /// </summary>
    public interface INodeRegistry
    {
        /// <summary>
        /// 注册新的 Worker 节点，如果节点已存在则更新时间戳
        /// </summary>
        /// <param name="nodeUrl">要注册的节点URL</param>
        /// <exception cref="ArgumentNullException">当 nodeUrl 为空或 null 时抛出</exception>
        void Register(string nodeUrl);

        /// <summary>
        /// 更新节点的最后心跳时间，如果节点不存在则自动注册
        /// </summary>
        /// <param name="nodeUrl">发送心跳的节点URL</param>
        /// <exception cref="ArgumentNullException">当 nodeUrl 为空或 null 时抛出</exception>
        void Heartbeat(string nodeUrl);

        /// <summary>
        /// 获取所有在指定超时时间内有心跳的活跃节点
        /// </summary>
        /// <param name="timeout">心跳超时时间</param>
        /// <returns>活跃节点URL列表</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 timeout 小于等于零时抛出</exception>
        List<string> GetAliveNodes(TimeSpan timeout);

        /// <summary>
        /// 获取所有注册的节点及其最后心跳时间
        /// </summary>
        /// <returns>所有注册节点的 URL 及其最后心跳时间的字典</returns>
        ConcurrentDictionary<string, DateTime> GetAllNodes();

        /// <summary>
        /// 从注册表中移除指定节点
        /// </summary>
        /// <param name="nodeUrl">要移除的节点URL</param>
        /// <exception cref="ArgumentNullException">当 nodeUrl 为空或 null 时抛出</exception>
        void RemoveNode(string nodeUrl);
    }
}