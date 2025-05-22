
namespace MonkeyScheduler.SchedulerService.Models
{
    /// <summary>
    /// 创建任务的请求模型
    /// </summary>
    public class CreateTaskRequest
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Cron表达式，用于定义任务执行时间
        /// </summary>
        public string CronExpression { get; set; } = string.Empty;

        /// <summary>
        /// 任务描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 任务参数（可选）
        /// </summary>
        public string? Parameters { get; set; }
    }
} 