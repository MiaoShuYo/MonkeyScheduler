using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// 配置扩展方法
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// 添加并验证MonkeyScheduler配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddMonkeySchedulerConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 绑定根配置
            services.Configure<MonkeySchedulerConfiguration>(configuration.GetSection("MonkeyScheduler"));

            // 绑定各个子配置
            services.Configure<DatabaseConfiguration>(configuration.GetSection("MonkeyScheduler:Database"));
            services.Configure<RetryConfiguration>(configuration.GetSection("MonkeyScheduler:Retry"));
            services.Configure<SchedulerConfiguration>(configuration.GetSection("MonkeyScheduler:Scheduler"));
            services.Configure<WorkerConfiguration>(configuration.GetSection("MonkeyScheduler:Worker"));
            services.Configure<LoadBalancerConfiguration>(configuration.GetSection("MonkeyScheduler:LoadBalancer"));
            services.Configure<LoggingConfiguration>(configuration.GetSection("MonkeyScheduler:Logging"));
            services.Configure<SecurityConfiguration>(configuration.GetSection("MonkeyScheduler:Security"));

            // 注册配置验证器
            services.AddSingleton<IConfigurationValidator, ConfigurationValidatorImpl>();

            return services;
        }

        /// <summary>
        /// 验证MonkeyScheduler配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>验证结果</returns>
        public static List<ValidationResult> ValidateMonkeySchedulerConfiguration(this IConfiguration configuration)
        {
            var config = new MonkeySchedulerConfiguration();
            configuration.GetSection("MonkeyScheduler").Bind(config);
            return ConfigurationValidator.ValidateMonkeySchedulerConfiguration(config);
        }

        /// <summary>
        /// 获取MonkeyScheduler配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>MonkeyScheduler配置</returns>
        public static MonkeySchedulerConfiguration GetMonkeySchedulerConfiguration(this IConfiguration configuration)
        {
            var config = new MonkeySchedulerConfiguration();
            configuration.GetSection("MonkeyScheduler").Bind(config);
            return config;
        }

        /// <summary>
        /// 获取数据库配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>数据库配置</returns>
        public static DatabaseConfiguration GetDatabaseConfiguration(this IConfiguration configuration)
        {
            var config = new DatabaseConfiguration();
            configuration.GetSection("MonkeyScheduler:Database").Bind(config);
            return config;
        }

        /// <summary>
        /// 获取重试配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>重试配置</returns>
        public static RetryConfiguration GetRetryConfiguration(this IConfiguration configuration)
        {
            var config = new RetryConfiguration();
            configuration.GetSection("MonkeyScheduler:Retry").Bind(config);
            return config;
        }

        /// <summary>
        /// 获取调度器配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>调度器配置</returns>
        public static SchedulerConfiguration GetSchedulerConfiguration(this IConfiguration configuration)
        {
            var config = new SchedulerConfiguration();
            configuration.GetSection("MonkeyScheduler:Scheduler").Bind(config);
            return config;
        }

        /// <summary>
        /// 获取Worker配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>Worker配置</returns>
        public static WorkerConfiguration GetWorkerConfiguration(this IConfiguration configuration)
        {
            var config = new WorkerConfiguration();
            configuration.GetSection("MonkeyScheduler:Worker").Bind(config);
            return config;
        }

        /// <summary>
        /// 获取负载均衡器配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>负载均衡器配置</returns>
        public static LoadBalancerConfiguration GetLoadBalancerConfiguration(this IConfiguration configuration)
        {
            var config = new LoadBalancerConfiguration();
            configuration.GetSection("MonkeyScheduler:LoadBalancer").Bind(config);
            return config;
        }

        /// <summary>
        /// 获取日志配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>日志配置</returns>
        public static LoggingConfiguration GetLoggingConfiguration(this IConfiguration configuration)
        {
            var config = new LoggingConfiguration();
            configuration.GetSection("MonkeyScheduler:Logging").Bind(config);
            return config;
        }

        /// <summary>
        /// 获取安全配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>安全配置</returns>
        public static SecurityConfiguration GetSecurityConfiguration(this IConfiguration configuration)
        {
            var config = new SecurityConfiguration();
            configuration.GetSection("MonkeyScheduler:Security").Bind(config);
            return config;
        }
    }

    /// <summary>
    /// 配置验证器接口
    /// </summary>
    public interface IConfigurationValidator
    {
        /// <summary>
        /// 验证配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>验证结果</returns>
        List<ValidationResult> ValidateConfiguration(MonkeySchedulerConfiguration configuration);
    }

    /// <summary>
    /// 配置验证器实现
    /// </summary>
    public class ConfigurationValidatorImpl : IConfigurationValidator
    {
        /// <summary>
        /// 验证配置
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>验证结果</returns>
        public List<ValidationResult> ValidateConfiguration(MonkeySchedulerConfiguration configuration)
        {
            return ConfigurationValidator.ValidateMonkeySchedulerConfiguration(configuration);
        }
    }
} 