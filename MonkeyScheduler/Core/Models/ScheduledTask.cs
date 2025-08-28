using System;
using System.Collections.Generic;
using System.Linq;

namespace MonkeyScheduler.Core.Models
{
    public class ScheduledTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public DateTime NextRunTime { get; set; }
        public bool Enabled { get; set; } = true;
        
        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 任务类型
        /// </summary>
        public string TaskType { get; set; } = string.Empty;
        
        /// <summary>
        /// 任务参数（JSON格式）
        /// </summary>
        public string TaskParameters { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否启用重试机制
        /// </summary>
        public bool EnableRetry { get; set; } = true;
        
        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
        
        /// <summary>
        /// 重试间隔（秒）
        /// </summary>
        public int RetryIntervalSeconds { get; set; } = 60;
        
        /// <summary>
        /// 重试策略（Fixed, Exponential, Linear）
        /// </summary>
        public RetryStrategy RetryStrategy { get; set; } = RetryStrategy.Exponential;
        
        /// <summary>
        /// 当前重试次数
        /// </summary>
        public int CurrentRetryCount { get; set; } = 0;
        
        /// <summary>
        /// 下次重试时间
        /// </summary>
        public DateTime? NextRetryTime { get; set; }
        
        /// <summary>
        /// 任务超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        // ========== DAG任务编排相关字段 ==========
        
        /// <summary>
        /// 依赖的任务ID列表（前置任务）
        /// 只有当所有依赖任务都成功执行后，当前任务才能执行
        /// </summary>
        public List<Guid>? Dependencies { get; set; }
        
        /// <summary>
        /// 后续任务ID列表（后置任务）
        /// 当前任务成功执行后，会自动触发这些任务
        /// </summary>
        public List<Guid>? NextTaskIds { get; set; }
        
        /// <summary>
        /// DAG执行状态
        /// </summary>
        public DagExecutionStatus DagStatus { get; set; } = DagExecutionStatus.Waiting;
        
        /// <summary>
        /// 已完成的依赖任务数量
        /// </summary>
        public int CompletedDependenciesCount { get; set; } = 0;
        
        /// <summary>
        /// 总依赖任务数量
        /// </summary>
        public int TotalDependenciesCount { get; set; } = 0;
        
        /// <summary>
        /// 是否属于DAG工作流
        /// </summary>
        public bool IsDagTask => Dependencies?.Any() == true || NextTaskIds?.Any() == true;
        
        /// <summary>
        /// DAG工作流ID（用于分组管理）
        /// </summary>
        public Guid? DagWorkflowId { get; set; }
        
        /// <summary>
        /// 在DAG中的执行优先级（数字越小优先级越高）
        /// </summary>
        public int DagPriority { get; set; } = 0;
        
        /// <summary>
        /// 是否允许并行执行（在DAG中）
        /// </summary>
        public bool AllowParallelExecution { get; set; } = true;
    }
    
    /// <summary>
    /// 重试策略枚举
    /// </summary>
    public enum RetryStrategy
    {
        /// <summary>
        /// 固定间隔重试
        /// </summary>
        Fixed,
        
        /// <summary>
        /// 指数退避重试
        /// </summary>
        Exponential,
        
        /// <summary>
        /// 线性增长重试
        /// </summary>
        Linear
    }
    
    /// <summary>
    /// DAG执行状态枚举
    /// </summary>
    public enum DagExecutionStatus
    {
        /// <summary>
        /// 等待依赖任务完成
        /// </summary>
        Waiting,
        
        /// <summary>
        /// 依赖已满足，可以执行
        /// </summary>
        Ready,
        
        /// <summary>
        /// 正在执行
        /// </summary>
        Running,
        
        /// <summary>
        /// 执行完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 执行失败
        /// </summary>
        Failed,
        
        /// <summary>
        /// 跳过执行（依赖任务失败）
        /// </summary>
        Skipped
    }
} 