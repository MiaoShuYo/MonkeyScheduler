using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService.Controllers
{
    /// <summary>
    /// 负载均衡策略管理控制器
    /// 提供负载均衡策略的查询、切换和配置功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LoadBalancingController : ControllerBase
    {
        private readonly LoadBalancingStrategyFactory _strategyFactory;
        private readonly ILoadBalancer _loadBalancer;
        private readonly ILogger<LoadBalancingController> _logger;

        public LoadBalancingController(
            LoadBalancingStrategyFactory strategyFactory,
            ILoadBalancer loadBalancer,
            ILogger<LoadBalancingController> logger)
        {
            _strategyFactory = strategyFactory;
            _loadBalancer = loadBalancer;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有可用的负载均衡策略
        /// </summary>
        /// <returns>策略列表</returns>
        [HttpGet("strategies")]
        public ActionResult GetAvailableStrategies()
        {
            _logger.LogInformation("获取可用负载均衡策略列表");
            
            var strategies = _strategyFactory.GetAvailableStrategies();
            var strategyInfos = _strategyFactory.GetAllStrategyInfo();
            
            return Ok(new
            {
                AvailableStrategies = strategies,
                StrategyDetails = strategyInfos
            });
        }

        /// <summary>
        /// 获取指定策略的详细信息
        /// </summary>
        /// <param name="strategyName">策略名称</param>
        /// <returns>策略详细信息</returns>
        [HttpGet("strategies/{strategyName}")]
        public ActionResult GetStrategyInfo(string strategyName)
        {
            if (string.IsNullOrWhiteSpace(strategyName))
            {
                return BadRequest("策略名称不能为空");
            }

            try
            {
                var strategyInfo = _strategyFactory.GetStrategyInfo(strategyName);
                _logger.LogInformation("获取策略 {StrategyName} 的详细信息", strategyName);
                return Ok(strategyInfo);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("策略 {StrategyName} 不存在: {Message}", strategyName, ex.Message);
                return NotFound($"策略 {strategyName} 不存在");
            }
        }

        /// <summary>
        /// 获取当前负载均衡器的状态信息
        /// </summary>
        /// <returns>负载均衡器状态</returns>
        [HttpGet("status")]
        public ActionResult GetLoadBalancerStatus()
        {
            _logger.LogInformation("获取负载均衡器状态");
            
            var strategyInfo = _loadBalancer.GetStrategyInfo();
            var nodeLoads = _loadBalancer.GetNodeLoads();
            
            return Ok(new
            {
                CurrentStrategy = strategyInfo,
                NodeLoads = nodeLoads,
                TotalNodes = nodeLoads.Count,
                TotalLoad = nodeLoads.Values.Sum()
            });
        }

        /// <summary>
        /// 更新当前策略的配置
        /// </summary>
        /// <param name="configuration">新的配置参数</param>
        /// <returns>更新结果</returns>
        [HttpPut("configuration")]
        public ActionResult UpdateStrategyConfiguration([FromBody] Dictionary<string, object> configuration)
        {
            if (configuration == null)
            {
                return BadRequest("配置参数不能为空");
            }

            try
            {
                _loadBalancer.UpdateStrategyConfiguration(configuration);
                _logger.LogInformation("更新负载均衡策略配置: {@Configuration}", configuration);
                
                return Ok(new { message = "策略配置已更新" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新策略配置失败");
                return BadRequest($"更新策略配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取节点负载统计信息
        /// </summary>
        /// <returns>节点负载统计</returns>
        [HttpGet("node-loads")]
        public ActionResult GetNodeLoads()
        {
            _logger.LogInformation("获取节点负载统计信息");
            
            var nodeLoads = _loadBalancer.GetNodeLoads();
            var totalLoad = nodeLoads.Values.Sum();
            var averageLoad = nodeLoads.Any() ? (double)totalLoad / nodeLoads.Count : 0;
            
            var loadStats = nodeLoads.Select(kvp => new
            {
                NodeUrl = kvp.Key,
                CurrentLoad = kvp.Value,
                LoadPercentage = totalLoad > 0 ? (double)kvp.Value / totalLoad * 100 : 0
            }).OrderByDescending(x => x.CurrentLoad).ToList();
            
            return Ok(new
            {
                NodeLoads = loadStats,
                Statistics = new
                {
                    TotalNodes = nodeLoads.Count,
                    TotalLoad = totalLoad,
                    AverageLoad = Math.Round(averageLoad, 2),
                    MaxLoad = nodeLoads.Any() ? nodeLoads.Values.Max() : 0,
                    MinLoad = nodeLoads.Any() ? nodeLoads.Values.Min() : 0
                }
            });
        }

        /// <summary>
        /// 注册自定义负载均衡策略
        /// </summary>
        /// <param name="request">注册请求</param>
        /// <returns>注册结果</returns>
        [HttpPost("register-strategy")]
        public ActionResult RegisterCustomStrategy([FromBody] RegisterStrategyRequest request)
        {
            if (request == null)
            {
                return BadRequest("注册请求不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.StrategyName))
            {
                return BadRequest("策略名称不能为空");
            }

            if (string.IsNullOrWhiteSpace(request.StrategyTypeName))
            {
                return BadRequest("策略类型名称不能为空");
            }

            try
            {
                // 尝试加载策略类型
                var strategyType = Type.GetType(request.StrategyTypeName);
                if (strategyType == null)
                {
                    return BadRequest($"无法找到策略类型: {request.StrategyTypeName}");
                }

                // 注册策略
                _strategyFactory.RegisterStrategy(request.StrategyName, strategyType);
                
                _logger.LogInformation("注册自定义负载均衡策略: {StrategyName} -> {StrategyType}", 
                    request.StrategyName, request.StrategyTypeName);
                
                return Ok(new { message = $"策略 {request.StrategyName} 注册成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册自定义策略失败: {StrategyName}", request.StrategyName);
                return BadRequest($"注册策略失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 注册策略请求模型
    /// </summary>
    public class RegisterStrategyRequest
    {
        /// <summary>
        /// 策略名称
        /// </summary>
        public string StrategyName { get; set; } = string.Empty;

        /// <summary>
        /// 策略类型名称（包含程序集信息）
        /// </summary>
        public string StrategyTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 策略描述
        /// </summary>
        public string? Description { get; set; }
    }
} 