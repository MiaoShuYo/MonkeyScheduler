using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 负载均衡策略接口
    /// 定义负载均衡算法的核心方法
    /// </summary>
    public interface ILoadBalancingStrategy
    {
        /// <summary>
        /// 策略名称
        /// </summary>
        string StrategyName { get; }

        /// <summary>
        /// 策略描述
        /// </summary>
        string StrategyDescription { get; }

        /// <summary>
        /// 选择最适合执行任务的节点
        /// </summary>
        /// <param name="availableNodes">可用节点列表</param>
        /// <param name="task">要执行的任务</param>
        /// <param name="nodeLoads">节点负载信息</param>
        /// <returns>选中的节点URL</returns>
        string SelectNode(IEnumerable<string> availableNodes, ScheduledTask task, IDictionary<string, int> nodeLoads);

        /// <summary>
        /// 获取策略的配置参数
        /// </summary>
        /// <returns>策略配置字典</returns>
        IDictionary<string, object> GetConfiguration();

        /// <summary>
        /// 更新策略配置
        /// </summary>
        /// <param name="configuration">新的配置参数</param>
        void UpdateConfiguration(IDictionary<string, object> configuration);
    }
} 