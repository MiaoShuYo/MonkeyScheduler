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
        string connectionString = services.BuildServiceProvider()
            .GetService<IConfiguration>()
            ?.GetValue<string>("MonkeyScheduler:Database:MySQL") ?? string.Empty;
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
        
        // 注册日志记录器提供程序
        services.AddSingleton<ILoggerProvider>(sp =>
        {
            var logRepository = sp.GetRequiredService<LogRepository>();
            return new MySQLLoggerProvider(logRepository);
        });
        
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
        
        // 注册日志记录器提供程序
        services.AddSingleton<ILoggerProvider>(sp =>
        {
            var logRepository = sp.GetRequiredService<LogRepository>();
            return new MySQLLoggerProvider(logRepository);
        });
        
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
        
        // 注册日志记录器提供程序
        services.AddSingleton<ILoggerProvider>(sp =>
        {
            var logRepository = sp.GetRequiredService<LogRepository>();
            return new MySQLLoggerProvider(logRepository);
        });
        
        return services;
    }
}