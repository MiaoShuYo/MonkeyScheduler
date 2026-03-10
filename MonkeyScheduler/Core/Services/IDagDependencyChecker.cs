using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services
{
    /// <summary>
    /// DAG依赖检查器接口
    /// 负责检查任务依赖关系、循环依赖检测等
    /// </summary>
    public interface IDagDependencyChecker
    {
        /// <summary>
        /// 检查任务依赖关系是否有效
        /// </summary>
        /// <param name="task">要检查的任务</param>
        /// <param name="allTasks">所有任务列表</param>
        /// <returns>依赖检查结果</returns>
        Task<DependencyCheckResult> CheckDependenciesAsync(ScheduledTask task, IEnumerable<ScheduledTask> allTasks);
        
        /// <summary>
        /// 检测循环依赖
        /// </summary>
        /// <param name="tasks">任务列表</param>
        /// <returns>循环依赖检测结果</returns>
        Task<CycleDetectionResult> DetectCyclesAsync(IEnumerable<ScheduledTask> tasks);
        
        /// <summary>
        /// 获取任务的所有依赖路径
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="allTasks">所有任务列表</param>
        /// <returns>依赖路径列表</returns>
        Task<List<List<Guid>>> GetDependencyPathsAsync(Guid taskId, IEnumerable<ScheduledTask> allTasks);
        
        /// <summary>
        /// 验证DAG工作流的完整性
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="tasks">工作流中的所有任务</param>
        /// <returns>验证结果</returns>
        Task<WorkflowValidationResult> ValidateWorkflowAsync(Guid workflowId, IEnumerable<ScheduledTask> tasks);
    }
    
    /// <summary>
    /// 依赖检查结果
    /// </summary>
    public class DependencyCheckResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 缺失的依赖任务ID列表
        /// </summary>
        public List<Guid> MissingDependencies { get; set; } = new();
        
        /// <summary>
        /// 无效的依赖任务ID列表
        /// </summary>
        public List<Guid> InvalidDependencies { get; set; } = new();
    }
    
    /// <summary>
    /// 循环依赖检测结果
    /// </summary>
    public class CycleDetectionResult
    {
        /// <summary>
        /// 是否存在循环依赖
        /// </summary>
        public bool HasCycle { get; set; }
        
        /// <summary>
        /// 循环依赖路径
        /// </summary>
        public List<List<Guid>> Cycles { get; set; } = new();
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// 工作流验证结果
    /// </summary>
    public class WorkflowValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 入口任务列表（没有依赖的任务）
        /// </summary>
        public List<Guid> EntryTasks { get; set; } = new();
        
        /// <summary>
        /// 出口任务列表（没有后续任务的任务）
        /// </summary>
        public List<Guid> ExitTasks { get; set; } = new();
        
        /// <summary>
        /// 任务执行层级
        /// </summary>
        public Dictionary<int, List<Guid>> ExecutionLevels { get; set; } = new();
    }
}
