using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 负载均衡器接口，用于管理和分配工作节点的任务负载
    /// </summary>
    public interface ILoadBalancer
    {
        /// <summary>
        /// 根据负载均衡策略选择最适合执行任务的节点
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <returns>选中的节点URL，如果没有可用节点则返回 null</returns>
        /// <exception cref="ArgumentNullException">当 task 参数为 null 时抛出</exception>
        string SelectNode(ScheduledTask task);

        /// <summary>
        /// 减少指定节点的负载计数，通常在任务执行完成后调用
        /// </summary>
        /// <param name="nodeUrl">要减少负载的节点URL</param>
        /// <exception cref="ArgumentNullException">当 nodeUrl 参数为 null 或空时抛出</exception>
        void DecreaseLoad(string nodeUrl);

        /// <summary>
        /// 从负载均衡器中移除节点，通常在节点下线或故障时调用
        /// </summary>
        /// <param name="nodeUrl">要移除的节点URL</param>
        /// <exception cref="ArgumentNullException">当 nodeUrl 参数为 null 或空时抛出</exception>
        void RemoveNode(string nodeUrl);

        /// <summary>
        /// 添加新节点到负载均衡器，通常在节点上线时调用
        /// </summary>
        /// <param name="nodeUrl">要添加的节点URL</param>
        /// <exception cref="ArgumentNullException">当 nodeUrl 参数为 null 或空时抛出</exception>
        /// <exception cref="InvalidOperationException">当节点已存在时抛出</exception>
        void AddNode(string nodeUrl);

        /// <summary>
        /// 获取当前负载均衡策略信息
        /// </summary>
        /// <returns>策略信息</returns>
        object GetStrategyInfo();

        /// <summary>
        /// 更新负载均衡策略配置
        /// </summary>
        /// <param name="configuration">新的配置</param>
        void UpdateStrategyConfiguration(IDictionary<string, object> configuration);

        /// <summary>
        /// 获取节点负载信息
        /// </summary>
        /// <returns>节点负载字典</returns>
        IDictionary<string, int> GetNodeLoads();
    }
}