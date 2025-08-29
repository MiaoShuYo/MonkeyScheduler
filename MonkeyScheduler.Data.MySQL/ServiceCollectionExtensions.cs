using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Data.MySQL.Data;
using MonkeyScheduler.Data.MySQL.Logger;
using MonkeyScheduler.Data.MySQL.Repositories;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.Data.MySQL;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 MySQL 数据访问服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="maxRetryAttempts">最大重试次数</param>
    /// <param name="retryDelay">重试延迟</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMySqlDataAccess(this IServiceCollection services, int maxRetryAttempts = 3, TimeSpan? retryDelay = null)
    {
        // 注册数据库上下文为单例服务，延迟到解析时再从 IConfiguration 读取连接串，避免在注册阶段构建临时 ServiceProvider
        services.AddSingleton<MySqlDbContext>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetValue<string>("MonkeyScheduler:Database:MySQL") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            var options = new MySqlConnectionOptions
            {
                ConnectionString = connectionString,
                MaxRetryAttempts = maxRetryAttempts,
                RetryDelay = retryDelay ?? TimeSpan.FromSeconds(1)
            };

            var logger = sp.GetService<ILogger<MySqlDbContext>>();
            return new MySqlDbContext(options, logger);
        });

        // 注册DapperWrapper
        services.AddSingleton<IDapperWrapper, DapperWrapper>();

        // 注册仓储类为单例服务
        services.AddSingleton<TaskRepository>();
        services.AddSingleton<LogRepository>();
        services.AddSingleton<ITaskRepository, MySQLTaskRepository>();
        services.AddSingleton<ITaskExecutionResult, TaskExecutionResultRepository>();
        
        return services;
    }

    /// <summary>
    /// 添加 MySQL 数据访问服务（使用自定义连接字符串）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="maxRetryAttempts">最大重试次数</param>
    /// <param name="retryDelay">重试延迟</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMySqlDataAccess(this IServiceCollection services, string connectionString, int maxRetryAttempts = 3, TimeSpan? retryDelay = null)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        var options = new MySqlConnectionOptions
        {
            ConnectionString = connectionString,
            MaxRetryAttempts = maxRetryAttempts,
            RetryDelay = retryDelay ?? TimeSpan.FromSeconds(1)
        };

        // 注册数据库上下文为单例服务
        services.AddSingleton<MySqlDbContext>(sp =>
        {
            var logger = sp.GetService<ILogger<MySqlDbContext>>();
            return new MySqlDbContext(options, logger);
        });

        // 注册DapperWrapper
        services.AddSingleton<IDapperWrapper, DapperWrapper>();

        // 注册仓储类为单例服务
        services.AddSingleton<TaskRepository>();
        services.AddSingleton<LogRepository>();
        services.AddSingleton<ITaskRepository, MySQLTaskRepository>();
        services.AddSingleton<ITaskExecutionResult, TaskExecutionResultRepository>();
        
        return services;
    }

    /// <summary>
    /// 添加 MySQL 数据访问服务（使用完整配置选项）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="options">连接配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMySqlDataAccess(this IServiceCollection services, MySqlConnectionOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrEmpty(options.ConnectionString))
        {
            throw new ArgumentException("连接字符串不能为空", nameof(options));
        }

        // 注册数据库上下文为单例服务
        services.AddSingleton<MySqlDbContext>(sp =>
        {
            var logger = sp.GetService<ILogger<MySqlDbContext>>();
            return new MySqlDbContext(options, logger);
        });

        // 注册DapperWrapper
        services.AddSingleton<IDapperWrapper, DapperWrapper>();

        // 注册仓储类为单例服务
        services.AddSingleton<TaskRepository>();
        services.AddSingleton<LogRepository>();
        services.AddSingleton<ITaskRepository, MySQLTaskRepository>();
        services.AddSingleton<ITaskExecutionResult, TaskExecutionResultRepository>();
        
        return services;
    }

    /// <summary>
    /// 启用 MySQL 日志记录（在应用启动后调用，避免构建期间阻塞）
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseMySqlLogging(this IApplicationBuilder app)
    {
        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logRepository = app.ApplicationServices.GetRequiredService<LogRepository>();
        var mySqlLoggerProvider = new MySQLLoggerProvider(logRepository);
        
        loggerFactory.AddProvider(mySqlLoggerProvider);
        
        return app;
    }
}