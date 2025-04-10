 using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 负载均衡器接口
    /// </summary>
    public interface ILoadBalancer
    {
        /// <summary>
        /// 选择最适合执行任务的节点
        /// </summary>
        /// <param name="task">要执行的任务</param>
        /// <returns>选中的节点URL</returns>
        string SelectNode(ScheduledTask task);

        /// <summary>
        /// 减少指定节点的负载计数
        /// </summary>
        /// <param name="nodeUrl">要减少负载的节点URL</param>
        void DecreaseLoad(string nodeUrl);

        /// <summary>
        /// 从负载均衡器中移除节点
        /// </summary>
        /// <param name="nodeUrl">要移除的节点URL</param>
        void RemoveNode(string nodeUrl);
    }
}