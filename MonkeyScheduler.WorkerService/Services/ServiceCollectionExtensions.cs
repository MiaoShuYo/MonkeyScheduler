using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Storage;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.WorkerService.Options;

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
        services.AddSingleton<IStatusReporterService, StatusReporterService>(); // 注册状态上报服务
        services.AddHostedService<NodeHeartbeatService>(); // 注册节点心跳服务为托管服务

        // 配置Worker选项 - 使用延迟配置，避免在服务注册时构建ServiceProvider
        services.Configure<WorkerOptions>(options =>
        {
            options.WorkerUrl = workerUrl;
            options.SchedulerUrl = "http://localhost:4057"; // 默认调度器URL
        });

        services.PostConfigure<WorkerOptions>(options =>
        {
            if (string.IsNullOrEmpty(options.SchedulerUrl))
            {
                options.SchedulerUrl = "http://localhost:4057";
            }
        });
        
        // 只保留 AddHttpClient、AddHealthChecks、AddSingleton<ITaskRepository, InMemoryTaskRepository>() 等标准注册

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