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
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMySqlDataAccess(this IServiceCollection services)
    {
        string connectionString = services.BuildServiceProvider()
            .GetService<IConfiguration>()
            ?.GetValue<string>("MonkeyScheduler:SchedulerDb") ?? string.Empty;
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        // 注册数据库上下文为单例服务
        services.AddSingleton<MySqlDbContext>(_ => new MySqlDbContext(connectionString));

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