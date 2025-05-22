using System;

namespace MonkeyScheduler.Data.MySQL.Models
{
    /// <summary>
    /// 计划任务数据模型
    /// 用于在MySQL数据库中存储计划任务的信息
    /// </summary>
    public class ScheduledTask
    {
        /// <summary>
        /// 任务唯一标识符
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 任务描述
        /// 可选字段，用于详细说明任务的用途和功能
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Cron表达式
        /// 用于定义任务的执行时间计划
        /// </summary>
        public string CronExpression { get; set; }

        /// <summary>
        /// 任务是否启用
        /// true表示任务处于活动状态，false表示任务被禁用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 任务创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 任务最后修改时间
        /// 可选字段，记录任务信息的最后更新时间
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// 任务类型
        /// 用于区分不同类型的任务，如定时任务、周期任务等
        /// </summary>
        public string TaskType { get; set; }

        /// <summary>
        /// 任务参数
        /// 存储任务执行所需的参数信息，通常为JSON格式
        /// </summary>
        public string TaskParameters { get; set; }
    }
} 