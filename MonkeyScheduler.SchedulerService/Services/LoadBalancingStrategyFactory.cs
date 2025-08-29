using MonkeyScheduler.SchedulerService.Services.Strategies;

namespace MonkeyScheduler.SchedulerService.Services
{
    /// <summary>
    /// 负载均衡策略工厂
    /// 负责创建和管理不同的负载均衡策略
    /// </summary>
    public class LoadBalancingStrategyFactory
    {
        private readonly Dictionary<string, Type> _strategyTypes;
        private readonly Dictionary<string, ILoadBalancingStrategy> _strategyInstances;

        public LoadBalancingStrategyFactory()
        {
            _strategyTypes = new Dictionary<string, Type>
            {
                ["LeastConnection"] = typeof(LeastConnectionStrategy),
                ["RoundRobin"] = typeof(RoundRobinStrategy),
                ["Random"] = typeof(RandomStrategy)
            };

            _strategyInstances = new Dictionary<string, ILoadBalancingStrategy>();
        }

        /// <summary>
        /// 获取可用的策略列表
        /// </summary>
        /// <returns>策略名称列表</returns>
        public IEnumerable<string> GetAvailableStrategies()
        {
            return _strategyTypes.Keys;
        }

        /// <summary>
        /// 创建策略实例
        /// </summary>
        /// <param name="strategyName">策略名称</param>
        /// <returns>策略实例</returns>
        public ILoadBalancingStrategy CreateStrategy(string strategyName)
        {
            if (string.IsNullOrWhiteSpace(strategyName))
                throw new ArgumentNullException(nameof(strategyName));

            if (!_strategyTypes.ContainsKey(strategyName))
                throw new ArgumentException($"不支持的负载均衡策略: {strategyName}");

            // 如果已经创建过实例，返回缓存的实例
            if (_strategyInstances.ContainsKey(strategyName))
                return _strategyInstances[strategyName];

            // 创建新实例
            var strategyType = _strategyTypes[strategyName];
            var strategy = (ILoadBalancingStrategy)Activator.CreateInstance(strategyType)!;
            
            // 缓存实例
            _strategyInstances[strategyName] = strategy;
            
            return strategy;
        }

        /// <summary>
        /// 注册自定义策略
        /// </summary>
        /// <param name="strategyName">策略名称</param>
        /// <param name="strategyType">策略类型</param>
        public void RegisterStrategy(string strategyName, Type strategyType)
        {
            if (string.IsNullOrWhiteSpace(strategyName))
                throw new ArgumentNullException(nameof(strategyName));

            if (strategyType == null)
                throw new ArgumentNullException(nameof(strategyType));

            if (!typeof(ILoadBalancingStrategy).IsAssignableFrom(strategyType))
                throw new ArgumentException($"类型 {strategyType.Name} 必须实现 ILoadBalancingStrategy 接口");

            _strategyTypes[strategyName] = strategyType;
            
            // 如果已经创建了实例，清除缓存
            if (_strategyInstances.ContainsKey(strategyName))
                _strategyInstances.Remove(strategyName);
        }

        /// <summary>
        /// 获取策略信息
        /// </summary>
        /// <param name="strategyName">策略名称</param>
        /// <returns>策略信息</returns>
        public object GetStrategyInfo(string strategyName)
        {
            var strategy = CreateStrategy(strategyName);
            return new
            {
                Name = strategy.StrategyName,
                Description = strategy.StrategyDescription,
                Configuration = strategy.GetConfiguration()
            };
        }

        /// <summary>
        /// 获取所有策略信息
        /// </summary>
        /// <returns>所有策略信息</returns>
        public IEnumerable<object> GetAllStrategyInfo()
        {
            return _strategyTypes.Keys.Select(GetStrategyInfo);
        }
    }
} 