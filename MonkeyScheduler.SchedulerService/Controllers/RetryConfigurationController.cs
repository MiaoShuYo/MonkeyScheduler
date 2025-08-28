using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.SchedulerService.Controllers
{
    /// <summary>
    /// 重试配置控制器
    /// 提供重试配置的管理功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RetryConfigurationController : ControllerBase
    {
        private readonly RetryConfiguration _retryConfig;
        private readonly ILogger<RetryConfigurationController> _logger;

        public RetryConfigurationController(
            IOptions<RetryConfiguration> retryConfig,
            ILogger<RetryConfigurationController> logger)
        {
            _retryConfig = retryConfig.Value;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前重试配置
        /// </summary>
        /// <returns>重试配置</returns>
        [HttpGet]
        public ActionResult<RetryConfiguration> GetRetryConfiguration()
        {
            _logger.LogInformation("获取重试配置");
            return Ok(_retryConfig);
        }

        /// <summary>
        /// 更新重试配置
        /// </summary>
        /// <param name="configuration">新的重试配置</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public ActionResult UpdateRetryConfiguration([FromBody] RetryConfiguration configuration)
        {
            if (configuration == null)
            {
                return BadRequest("重试配置不能为空");
            }

            _logger.LogInformation("更新重试配置: {@Configuration}", configuration);

            // 验证配置
            if (configuration.DefaultMaxRetryCount < 0)
            {
                return BadRequest("默认最大重试次数不能为负数");
            }

            if (configuration.DefaultRetryIntervalSeconds < 0)
            {
                return BadRequest("默认重试间隔不能为负数");
            }

            if (configuration.MaxRetryIntervalSeconds < configuration.DefaultRetryIntervalSeconds)
            {
                return BadRequest("最大重试间隔不能小于默认重试间隔");
            }

            // 注意：这里只是示例，实际应用中需要持久化配置
            // 可以通过配置提供器或数据库来持久化配置
            
            return Ok(new { message = "重试配置已更新" });
        }

        /// <summary>
        /// 获取重试策略列表
        /// </summary>
        /// <returns>重试策略列表</returns>
        [HttpGet("strategies")]
        public ActionResult GetRetryStrategies()
        {
            var strategies = Enum.GetValues<RetryStrategy>()
                .Select(s => new { 
                    Value = (int)s, 
                    Name = s.ToString(),
                    Description = GetRetryStrategyDescription(s)
                })
                .ToList();

            return Ok(strategies);
        }

        /// <summary>
        /// 测试重试间隔计算
        /// </summary>
        /// <param name="baseInterval">基础间隔（秒）</param>
        /// <param name="strategy">重试策略</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <returns>重试间隔计算结果</returns>
        [HttpGet("test-intervals")]
        public ActionResult TestRetryIntervals(
            [FromQuery] int baseInterval = 60,
            [FromQuery] RetryStrategy strategy = RetryStrategy.Exponential,
            [FromQuery] int maxRetries = 3)
        {
            if (baseInterval <= 0)
            {
                return BadRequest("基础间隔必须大于0");
            }

            if (maxRetries <= 0)
            {
                return BadRequest("最大重试次数必须大于0");
            }

            var intervals = new List<object>();
            
            for (int i = 1; i <= maxRetries; i++)
            {
                int delaySeconds;
                switch (strategy)
                {
                    case RetryStrategy.Fixed:
                        delaySeconds = baseInterval;
                        break;
                        
                    case RetryStrategy.Exponential:
                        delaySeconds = baseInterval * (int)Math.Pow(2, i - 1);
                        break;
                        
                    case RetryStrategy.Linear:
                        delaySeconds = baseInterval * i;
                        break;
                        
                    default:
                        delaySeconds = baseInterval;
                        break;
                }

                intervals.Add(new
                {
                    RetryAttempt = i,
                    DelaySeconds = delaySeconds,
                    DelayMinutes = Math.Round(delaySeconds / 60.0, 2),
                    NextRetryTime = DateTime.UtcNow.AddSeconds(delaySeconds)
                });
            }

            return Ok(new
            {
                Strategy = strategy.ToString(),
                BaseIntervalSeconds = baseInterval,
                MaxRetries = maxRetries,
                Intervals = intervals
            });
        }

        private string GetRetryStrategyDescription(RetryStrategy strategy)
        {
            return strategy switch
            {
                RetryStrategy.Fixed => "固定间隔重试，每次重试间隔相同",
                RetryStrategy.Exponential => "指数退避重试，重试间隔呈指数增长",
                RetryStrategy.Linear => "线性增长重试，重试间隔呈线性增长",
                _ => "未知策略"
            };
        }
    }
} 