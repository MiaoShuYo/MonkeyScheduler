using MonkeyScheduler.Core.Models;

namespace MonkeyScheduler.Core.Services
{
    /// <summary>
    /// DAG任务执行管理器接口
    /// 负责管理DAG工作流的执行，包括依赖检查、任务触发等
    /// </summary>
    public interface IDagExecutionManager
    {
        /// <summary>
        /// 启动DAG工作流执行
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="allTasks">所有任务列表</param>
        /// <returns>执行结果</returns>
        Task<DagExecutionResult> StartWorkflowAsync(Guid workflowId, IEnumerable<ScheduledTask> allTasks);
        
        /// <summary>
        /// 处理任务完成事件
        /// </summary>
        /// <param name="completedTaskId">已完成的任务ID</param>
        /// <param name="result">执行结果</param>
        /// <param name="allTasks">所有任务列表</param>
        /// <returns>需要触发的后续任务列表</returns>
        Task<List<ScheduledTask>> OnTaskCompletedAsync(Guid completedTaskId, TaskExecutionResult result, IEnumerable<ScheduledTask> allTasks);
        
        /// <summary>
        /// 检查任务是否可以执行（依赖是否满足）
        /// </summary>
        /// <param name="task">要检查的任务</param>
        /// <param name="allTasks">所有任务列表</param>
        /// <returns>是否可以执行</returns>
        Task<bool> CanExecuteTaskAsync(ScheduledTask task, IEnumerable<ScheduledTask> allTasks);
        
        /// <summary>
        /// 获取工作流执行状态
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>工作流状态</returns>
        Task<WorkflowExecutionStatus> GetWorkflowStatusAsync(Guid workflowId);
        
        /// <summary>
        /// 暂停工作流执行
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>操作结果</returns>
        Task<bool> PauseWorkflowAsync(Guid workflowId);
        
        /// <summary>
        /// 恢复工作流执行
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>操作结果</returns>
        Task<bool> ResumeWorkflowAsync(Guid workflowId);
        
        /// <summary>
        /// 取消工作流执行
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>操作结果</returns>
        Task<bool> CancelWorkflowAsync(Guid workflowId);
    }
    
    /// <summary>
    /// DAG执行结果
    /// </summary>
    public class DagExecutionResult
    {
        /// <summary>
        /// 是否成功启动
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 启动的任务数量
        /// </summary>
        public int StartedTaskCount { get; set; }
        
        /// <summary>
        /// 工作流ID
        /// </summary>
        public Guid WorkflowId { get; set; }
        
        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime StartTime { get; set; }
    }
    
    /// <summary>
    /// 工作流执行状态
    /// </summary>
    public class WorkflowExecutionStatus
    {
        /// <summary>
        /// 工作流ID
        /// </summary>
        public Guid WorkflowId { get; set; }
        
        /// <summary>
        /// 执行状态
        /// </summary>
        public WorkflowStatus Status { get; set; }
        
        /// <summary>
        /// 总任务数
        /// </summary>
        public int TotalTasks { get; set; }
        
        /// <summary>
        /// 已完成任务数
        /// </summary>
        public int CompletedTasks { get; set; }
        
        /// <summary>
        /// 失败任务数
        /// </summary>
        public int FailedTasks { get; set; }
        
        /// <summary>
        /// 正在执行的任务数
        /// </summary>
        public int RunningTasks { get; set; }
        
        /// <summary>
        /// 等待执行的任务数
        /// </summary>
        public int WaitingTasks { get; set; }
        
        /// <summary>
        /// 跳过执行的任务数
        /// </summary>
        public int SkippedTasks { get; set; }
        
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }
        
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// 进度百分比
        /// </summary>
        public double ProgressPercentage => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
    }
    
    /// <summary>
    /// 工作流状态枚举
    /// </summary>
    public enum WorkflowStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        NotStarted,
        
        /// <summary>
        /// 正在执行
        /// </summary>
        Running,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 部分失败
        /// </summary>
        PartiallyFailed,
        
        /// <summary>
        /// 完全失败
        /// </summary>
        Failed,
        
        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }
}
