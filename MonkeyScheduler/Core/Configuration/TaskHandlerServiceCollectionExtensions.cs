using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Core.Services.Handlers;

namespace MonkeyScheduler.Core.Configuration
{
    /// <summary>
    /// 任务处理器服务注册扩展
    /// </summary>
    public static class TaskHandlerServiceCollectionExtensions
    {
        /// <summary>
        /// 添加任务处理器服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddTaskHandlers(this IServiceCollection services)
        {
            // 注册任务处理器工厂
            services.AddSingleton<ITaskHandlerFactory, TaskHandlerFactory>();
            
            // 注册数据库连接工厂
            services.AddTransient<IDbConnectionFactory, DefaultDbConnectionFactory>();
            
            // 注册内置任务处理器
            services.AddTransient<HttpTaskHandler>();
            services.AddTransient<SqlTaskHandler>();
            services.AddTransient<ShellTaskHandler>();
            services.AddTransient<CustomTaskHandler>();
            
            // 注册插件化任务执行器
            services.AddTransient<PluginTaskExecutor>();
            
            return services;
        }

        /// <summary>
        /// 配置任务处理器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configure">配置委托</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection ConfigureTaskHandlers(this IServiceCollection services, Action<ITaskHandlerFactory> configure)
        {
            services.AddSingleton<ITaskHandlerFactory>(provider =>
            {
                var factory = new TaskHandlerFactory(provider, provider.GetRequiredService<ILogger<TaskHandlerFactory>>());
                
                // 注册内置处理器
                factory.RegisterHandler<HttpTaskHandler>("http");
                factory.RegisterHandler<SqlTaskHandler>("sql");
                factory.RegisterHandler<ShellTaskHandler>("shell");
                factory.RegisterHandler<CustomTaskHandler>("custom");
                
                // 执行自定义配置
                configure?.Invoke(factory);
                
                return factory;
            });
            
            return services;
        }
    }
}
