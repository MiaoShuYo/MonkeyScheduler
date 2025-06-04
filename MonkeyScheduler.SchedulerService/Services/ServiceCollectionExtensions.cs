using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.SchedulerService.Services;

/// <summary>
/// 调度器服务集合扩展方法，用于配置和注册调度器服务相关的依赖项
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加负载均衡器到服务集合中
    /// </summary>
    /// <typeparam name="TLoadBalancer">负载均衡器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，用于链式调用</returns>
    public static IServiceCollection AddLoadBalancer<TLoadBalancer>(this IServiceCollection services)
        where TLoadBalancer : class, ILoadBalancer
    {
        services.AddSingleton<ILoadBalancer, TLoadBalancer>();
        return services;
    }

    /// <summary>
    /// 使用工厂方法添加负载均衡器到服务集合中
    /// </summary>
    /// <typeparam name="TLoadBalancer">负载均衡器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="implementationFactory">负载均衡器工厂方法</param>
    /// <returns>服务集合，用于链式调用</returns>
    public static IServiceCollection AddLoadBalancer<TLoadBalancer>(this IServiceCollection services, Func<IServiceProvider, TLoadBalancer> implementationFactory)
        where TLoadBalancer : class, ILoadBalancer
    {
        services.AddSingleton<ILoadBalancer>(implementationFactory);
        return services;
    }

    /// <summary>
    /// 添加任务仓库到服务集合中
    /// </summary>
    /// <typeparam name="TTaskRepository">任务仓库类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，用于链式调用</returns>
    public static IServiceCollection AddTaskRepository<TTaskRepository>(this IServiceCollection services)
        where TTaskRepository : class, ITaskRepository
    {
        services.AddSingleton<ITaskRepository, TTaskRepository>();
        return services;
    }

    /// <summary>
    /// 使用工厂方法添加任务仓库到服务集合中
    /// </summary>
    /// <typeparam name="TTaskRepository">任务仓库类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="implementationFactory">任务仓库工厂方法</param>
    /// <returns>服务集合，用于链式调用</returns>
    public static IServiceCollection AddTaskRepository<TTaskRepository>(this IServiceCollection services, Func<IServiceProvider, TTaskRepository> implementationFactory)
        where TTaskRepository : class, ITaskRepository
    {
        services.AddSingleton<ITaskRepository>(implementationFactory);
        return services;
    }

    /// <summary>
    /// 添加调度器服务到服务集合中
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，用于链式调用</returns>
    /// <remarks>
    /// 此方法会注册以下服务：
    /// 1. 负载均衡器
    /// 2. 节点注册表
    /// 3. 内存任务仓库
    /// 4. 调度器
    /// 5. HTTP 客户端工厂
    /// 6. 任务分发器
    /// 7. 任务重试管理器
    /// 8. 健康检查服务
    /// </remarks>
    public static IServiceCollection AddSchedulerService(this IServiceCollection services)
    {
        // 添加基础服务
        services.AddSingleton<ILoadBalancer, LoadBalancer>();
        services.AddSingleton<INodeRegistry, NodeRegistry>();
        services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
        services.AddSingleton<Scheduler>();
        // 添加HTTP客户端
        services.AddHttpClient();
        
        // 添加任务调度相关服务
        services.AddSingleton<ITaskDispatcher, TaskDispatcher>();
        services.AddSingleton<ITaskRetryManager, TaskRetryManager>();
        // 添加健康检查
        services.AddHealthChecks()
            .AddCheck<SchedulerHealthCheck>("scheduler_health_check");
        return services;
    }
    
    /// <summary>
    /// 配置调度器服务的中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器，用于链式调用</returns>
    /// <remarks>
    /// 此方法会执行以下操作：
    /// 1. 启动调度器服务
    /// 2. 配置健康检查端点
    /// </remarks>
    public static IApplicationBuilder UseSchedulerService(this IApplicationBuilder app)
    {
        // 启动调度器
        var scheduler = app.ApplicationServices.GetRequiredService<Scheduler>();
        scheduler.Start();
        
        // 添加健康检查端点
        app.UseHealthChecks("/scheduler_health");
        
        return app;
    }
} 