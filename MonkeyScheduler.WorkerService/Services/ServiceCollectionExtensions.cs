using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.WorkerService.Services;

/// <summary>
/// 服务集合扩展方法，用于配置和注册 Worker 服务相关的依赖项
/// </summary>
public static class ServiceCollectionExtensions
{
    
    /// <summary>
    /// 添加 Worker 服务到服务集合中
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="workerUrl">Worker 节点 URL</param>
    /// <returns>服务集合，用于链式调用</returns>
    /// <remarks>
    /// 此方法会注册以下服务：
    /// 1. 内存任务仓库
    /// 2. HTTP 客户端工厂
    /// 3. 状态上报服务
    /// 4. 节点心跳服务
    /// 5. 健康检查服务
    /// </remarks>
    public static IServiceCollection AddWorkerService(
        this IServiceCollection services,
        string workerUrl)
    {
        // 注册核心服务
        services.AddSingleton<ITaskRepository, InMemoryTaskRepository>(); // 使用内存存储作为任务仓库
        services.AddHttpClient(); // 注册HTTP客户端工厂

        // 从配置中获取schedulerUrl
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var schedulerUrl = configuration["MonkeyScheduler:SchedulingServer:Url"] ?? "http://localhost:4057";
        // 注册状态上报服务
        services.AddSingleton<IStatusReporterService>(provider =>
            new StatusReporterService(
                provider.GetRequiredService<IHttpClientFactory>(),
                schedulerUrl,
                workerUrl
            )
        );

        // 注册心跳服务
        services.AddHostedService(provider =>
            new NodeHeartbeatService(
                provider.GetRequiredService<IHttpClientFactory>(),
                schedulerUrl,
                workerUrl,
                provider.GetRequiredService<ILogger<NodeHeartbeatService>>()
            )
        );

        services.AddHealthChecks()
            .AddCheck<WorkerHealthCheck>("worker_health_check");

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
    public static IServiceCollection AddTaskRepository<TTaskRepository>(this IServiceCollection services,
        Func<IServiceProvider, TTaskRepository> implementationFactory)
        where TTaskRepository : class, ITaskRepository
    {
        services.AddSingleton<ITaskRepository>(implementationFactory);
        return services;
    }

    /// <summary>
    /// 添加任务执行器到服务集合中
    /// </summary>
    /// <typeparam name="TTaskExecutor">任务执行器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，用于链式调用</returns>
    public static IServiceCollection AddTaskExecutor<TTaskExecutor>(this IServiceCollection services)
        where TTaskExecutor : class, ITaskExecutor
    {
        services.AddSingleton<ITaskExecutor, TTaskExecutor>();
        return services;
    }

    /// <summary>
    /// 使用工厂方法添加任务执行器到服务集合中
    /// </summary>
    /// <typeparam name="TTaskExecutor">任务执行器类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="implementationFactory">任务执行器工厂方法</param>
    /// <returns>服务集合，用于链式调用</returns>
    public static IServiceCollection AddTaskExecutor<TTaskExecutor>(this IServiceCollection services,
        Func<IServiceProvider, TTaskExecutor> implementationFactory)
        where TTaskExecutor : class, ITaskExecutor
    {
        services.AddSingleton<ITaskExecutor>(implementationFactory);
        return services;
    }
    
    /// <summary>
    /// 配置 Worker 服务的中间件
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器，用于链式调用</returns>
    /// <remarks>
    /// 此方法会配置以下中间件：
    /// 1. 健康检查端点
    /// </remarks>
    public static IApplicationBuilder UseWorkerService(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/worker_health");
        return app;
    }
}