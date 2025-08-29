namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// 负载均衡器配置选项
    /// </summary>
    public class LoadBalancerConfiguration
    {
        /// <summary>
        /// 负载均衡策略
        /// </summary>
        public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.LeastConnection;

        /// <summary>
        /// 节点健康检查间隔（秒）
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// 节点超时时间（秒）
        /// </summary>
        public int NodeTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// 最大失败次数，超过后节点将被标记为不可用
        /// </summary>
        public int MaxFailureCount { get; set; } = 3;

        /// <summary>
        /// 节点恢复时间（秒），失败节点在此时间后可以重新参与负载均衡
        /// </summary>
        public int NodeRecoveryTimeSeconds { get; set; } = 300;

        /// <summary>
        /// 是否启用节点权重
        /// </summary>
        public bool EnableNodeWeighting { get; set; } = true;

        /// <summary>
        /// 默认节点权重
        /// </summary>
        public int DefaultNodeWeight { get; set; } = 1;

        /// <summary>
        /// 是否启用会话亲和性
        /// </summary>
        public bool EnableSessionAffinity { get; set; } = false;

        /// <summary>
        /// 会话亲和性超时时间（秒）
        /// </summary>
        public int SessionAffinityTimeoutSeconds { get; set; } = 1800;
    }

    /// <summary>
    /// 负载均衡策略枚举
    /// </summary>
    public enum LoadBalancingStrategy
    {
        /// <summary>
        /// 轮询策略
        /// </summary>
        RoundRobin,

        /// <summary>
        /// 最少连接策略
        /// </summary>
        LeastConnection,

        /// <summary>
        /// 加权轮询策略
        /// </summary>
        WeightedRoundRobin,

        /// <summary>
        /// 随机策略
        /// </summary>
        Random,

        /// <summary>
        /// IP哈希策略
        /// </summary>
        IpHash
    }
} 