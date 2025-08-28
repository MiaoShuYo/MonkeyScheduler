using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services
{
    /// <summary>
    /// 任务处理器接口
    /// 定义任务执行的标准接口，支持插件化扩展
    /// </summary>
    public interface ITaskHandler
    {
        /// <summary>
        /// 任务类型名称
        /// </summary>
        string TaskType { get; }
        
        /// <summary>
        /// 任务类型描述
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// 处理任务执行
        /// </summary>
        /// <param name="task">调度任务</param>
        /// <param name="parameters">任务参数</param>
        /// <returns>任务执行结果</returns>
        Task<TaskExecutionResult> HandleAsync(ScheduledTask task, object? parameters = null);
        
        /// <summary>
        /// 验证任务参数
        /// </summary>
        /// <param name="parameters">任务参数</param>
        /// <returns>验证结果</returns>
        Task<bool> ValidateParametersAsync(object? parameters);
        
        /// <summary>
        /// 获取任务处理器配置信息
        /// </summary>
        /// <returns>配置信息</returns>
        TaskHandlerConfiguration GetConfiguration();
    }
    
    /// <summary>
    /// 任务处理器配置
    /// </summary>
    public class TaskHandlerConfiguration
    {
        public string TaskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public bool SupportsRetry { get; set; } = true;
        public bool SupportsTimeout { get; set; } = true;
        public int DefaultTimeoutSeconds { get; set; } = 300;
        public Dictionary<string, object> DefaultParameters { get; set; } = new();
    }
}
