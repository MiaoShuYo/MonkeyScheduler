using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Configuration;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Core.Services.Handlers;
using System.Text.Json;

namespace MonkeyScheduler.Core.Services.Handlers.Examples
{
    /// <summary>
    /// 任务处理器使用示例
    /// 演示如何使用任务类型插件机制
    /// </summary>
    public class TaskHandlerUsageExample
    {
        /// <summary>
        /// 演示如何配置和使用任务处理器
        /// </summary>
        public static void ConfigureTaskHandlers(IServiceCollection services)
        {
            // 添加任务处理器服务
            services.AddTaskHandlers();
            
            // 配置任务处理器
            services.ConfigureTaskHandlers(factory =>
            {
                // 可以在这里注册自定义的任务处理器
                // factory.RegisterHandler<MyCustomTaskHandler>("my-custom");
                
                Console.WriteLine("任务处理器配置完成");
                Console.WriteLine($"支持的任务类型: {string.Join(", ", factory.GetSupportedTaskTypes())}");
            });
        }

        /// <summary>
        /// 演示如何创建不同类型的任务
        /// </summary>
        public static List<ScheduledTask> CreateExampleTasks()
        {
            var tasks = new List<ScheduledTask>();

            // 1. HTTP任务示例
            var httpTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "API健康检查",
                TaskType = "http",
                CronExpression = "*/5 * * * *", // 每5分钟执行
                TaskParameters = JsonSerializer.Serialize(new HttpTaskParameters
                {
                    Url = "https://httpbin.org/health",
                    Method = "GET",
                    Timeout = 30
                }),
                Description = "定期检查API服务健康状态"
            };
            tasks.Add(httpTask);

            // 2. SQL任务示例
            var sqlTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "数据库清理",
                TaskType = "sql",
                CronExpression = "0 2 * * *", // 每天凌晨2点执行
                TaskParameters = JsonSerializer.Serialize(new SqlTaskParameters
                {
                    SqlScript = "DELETE FROM logs WHERE created_date < DATE_SUB(NOW(), INTERVAL 30 DAY)",
                    ConnectionString = "Server=localhost;Database=testdb;Trusted_Connection=true;",
                    Database = "testdb",
                    Timeout = 300
                }),
                Description = "清理30天前的日志数据"
            };
            tasks.Add(sqlTask);

            // 3. Shell任务示例
            var shellTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "系统备份",
                TaskType = "shell",
                CronExpression = "0 1 * * *", // 每天凌晨1点执行
                TaskParameters = JsonSerializer.Serialize(new ShellTaskParameters
                {
                    Command = "tar -czf /backup/$(date +%Y%m%d).tar.gz /var/www",
                    WorkingDirectory = "/tmp",
                    Timeout = 1800
                }),
                Description = "备份网站文件"
            };
            tasks.Add(shellTask);

            // 4. 自定义任务示例
            var customTask = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = "自定义数据处理",
                TaskType = "custom",
                CronExpression = "0 */6 * * *", // 每6小时执行
                TaskParameters = JsonSerializer.Serialize(new CustomTaskParameters
                {
                    Operation = "calculate",
                    Number1 = 100,
                    Number2 = 200,
                    DelayMilliseconds = 5000
                }),
                Description = "执行自定义数据处理逻辑"
            };
            tasks.Add(customTask);

            return tasks;
        }

        /// <summary>
        /// 演示如何执行任务
        /// </summary>
        public static async Task ExecuteTaskExample(IServiceProvider serviceProvider)
        {
            var executor = serviceProvider.GetRequiredService<PluginTaskExecutor>();
            var logger = serviceProvider.GetRequiredService<ILogger<TaskHandlerUsageExample>>();

            // 创建示例任务
            var tasks = CreateExampleTasks();

            foreach (var task in tasks)
            {
                logger.LogInformation("开始执行任务: {TaskName} ({TaskType})", task.Name, task.TaskType);

                try
                {
                    await executor.ExecuteAsync(task, async (result) =>
                    {
                        logger.LogInformation("任务执行完成: {TaskName}, 成功: {Success}, 结果: {Result}",
                            task.Name, result.Success, result.Result);
                        
                        if (!result.Success)
                        {
                            logger.LogError("任务执行失败: {TaskName}, 错误: {Error}",
                                task.Name, result.ErrorMessage);
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "任务执行异常: {TaskName}", task.Name);
                }
            }
        }

        /// <summary>
        /// 演示如何验证任务参数
        /// </summary>
        public static async Task ValidateTaskParametersExample(IServiceProvider serviceProvider)
        {
            var handlerFactory = serviceProvider.GetRequiredService<ITaskHandlerFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<TaskHandlerUsageExample>>();

            // 验证HTTP任务参数
            var httpHandler = handlerFactory.GetHandler("http");
            var httpParams = new HttpTaskParameters
            {
                Url = "https://api.example.com/data",
                Method = "POST",
                Body = "{\"key\":\"value\"}"
            };
            
            var httpValid = await httpHandler.ValidateParametersAsync(httpParams);
            logger.LogInformation("HTTP任务参数验证结果: {IsValid}", httpValid);

            // 验证自定义任务参数
            var customHandler = handlerFactory.GetHandler("custom");
            var customParams = new CustomTaskParameters
            {
                Operation = "echo",
                Message = "Hello World"
            };
            
            var customValid = await customHandler.ValidateParametersAsync(customParams);
            logger.LogInformation("自定义任务参数验证结果: {IsValid}", customValid);
        }

        /// <summary>
        /// 演示如何获取任务处理器配置
        /// </summary>
        public static void GetHandlerConfigurationExample(IServiceProvider serviceProvider)
        {
            var handlerFactory = serviceProvider.GetRequiredService<ITaskHandlerFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<TaskHandlerUsageExample>>();

            foreach (var taskType in handlerFactory.GetSupportedTaskTypes())
            {
                try
                {
                    var handler = handlerFactory.GetHandler(taskType);
                    var config = handler.GetConfiguration();
                    
                    logger.LogInformation("任务类型 {TaskType} 配置:", taskType);
                    logger.LogInformation("  描述: {Description}", config.Description);
                    logger.LogInformation("  版本: {Version}", config.Version);
                    logger.LogInformation("  支持重试: {SupportsRetry}", config.SupportsRetry);
                    logger.LogInformation("  支持超时: {SupportsTimeout}", config.SupportsTimeout);
                    logger.LogInformation("  默认超时: {DefaultTimeout}秒", config.DefaultTimeoutSeconds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "获取任务类型 {TaskType} 配置失败", taskType);
                }
            }
        }
    }
}
