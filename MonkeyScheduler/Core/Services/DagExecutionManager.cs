using MonkeyScheduler.Core.Models;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.Core.Services
{
    /// <summary>
    /// DAG任务执行管理器实现
    /// 负责管理DAG工作流的执行，包括依赖检查、任务触发等
    /// </summary>
    public class DagExecutionManager : IDagExecutionManager
    {
        private readonly IDagDependencyChecker _dependencyChecker;
        private readonly ILogger<DagExecutionManager> _logger;
        private readonly Dictionary<Guid, WorkflowExecutionStatus> _workflowStatuses = new();
        private readonly Dictionary<Guid, Dictionary<Guid, DagExecutionStatus>> _taskStatuses = new();

        public DagExecutionManager(IDagDependencyChecker dependencyChecker, ILogger<DagExecutionManager> logger)
        {
            _dependencyChecker = dependencyChecker ?? throw new ArgumentNullException(nameof(dependencyChecker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 启动DAG工作流执行
        /// </summary>
        public async Task<DagExecutionResult> StartWorkflowAsync(Guid workflowId, IEnumerable<ScheduledTask> allTasks)
        {
            var result = new DagExecutionResult
            {
                WorkflowId = workflowId,
                StartTime = DateTime.UtcNow
            };

            try
            {
                var workflowTasks = allTasks.Where(t => t.DagWorkflowId == workflowId).ToList();
                
                if (!workflowTasks.Any())
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"工作流 {workflowId} 中没有找到任何任务";
                    return result;
                }

                // 验证工作流
                var validationResult = await _dependencyChecker.ValidateWorkflowAsync(workflowId, workflowTasks);
                if (!validationResult.IsValid)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = validationResult.ErrorMessage;
                    return result;
                }

                // 初始化工作流状态
                var workflowStatus = new WorkflowExecutionStatus
                {
                    WorkflowId = workflowId,
                    Status = WorkflowStatus.Running,
                    TotalTasks = workflowTasks.Count,
                    StartTime = DateTime.UtcNow
                };

                // 初始化任务状态
                var taskStatusDict = new Dictionary<Guid, DagExecutionStatus>();
                foreach (var task in workflowTasks)
                {
                    task.DagStatus = DagExecutionStatus.Waiting;
                    task.TotalDependenciesCount = task.Dependencies?.Count ?? 0;
                    task.CompletedDependenciesCount = 0;
                    taskStatusDict[task.Id] = DagExecutionStatus.Waiting;
                }

                _workflowStatuses[workflowId] = workflowStatus;
                _taskStatuses[workflowId] = taskStatusDict;

                // 启动入口任务
                var entryTasks = workflowTasks.Where(t => t.Dependencies == null || !t.Dependencies.Any()).ToList();
                var startedCount = 0;

                foreach (var entryTask in entryTasks)
                {
                    if (await CanExecuteTaskAsync(entryTask, workflowTasks))
                    {
                        entryTask.DagStatus = DagExecutionStatus.Ready;
                        taskStatusDict[entryTask.Id] = DagExecutionStatus.Ready;
                        startedCount++;
                    }
                }

                result.IsSuccess = true;
                result.StartedTaskCount = startedCount;

                _logger.LogInformation("工作流 {WorkflowId} 启动成功，入口任务数: {EntryTaskCount}, 已启动: {StartedCount}",
                    workflowId, entryTasks.Count, startedCount);

                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"启动工作流 {workflowId} 时发生错误: {ex.Message}";
                _logger.LogError(ex, "启动工作流 {WorkflowId} 失败", workflowId);
                return result;
            }
        }

        /// <summary>
        /// 处理任务完成事件
        /// </summary>
        public async Task<List<ScheduledTask>> OnTaskCompletedAsync(Guid completedTaskId, TaskExecutionResult result, IEnumerable<ScheduledTask> allTasks)
        {
            var triggeredTasks = new List<ScheduledTask>();
            
            try
            {
                var completedTask = allTasks.FirstOrDefault(t => t.Id == completedTaskId);
                if (completedTask == null)
                {
                    _logger.LogWarning("未找到已完成的任务: {TaskId}", completedTaskId);
                    return triggeredTasks;
                }

                // 更新任务状态
                completedTask.DagStatus = result.Success ? DagExecutionStatus.Completed : DagExecutionStatus.Failed;
                
                if (completedTask.DagWorkflowId.HasValue)
                {
                    var workflowId = completedTask.DagWorkflowId.Value;
                    if (_taskStatuses.ContainsKey(workflowId))
                    {
                        _taskStatuses[workflowId][completedTaskId] = completedTask.DagStatus;
                    }
                }

                // 如果任务执行成功，触发后续任务
                if (result.Success && completedTask.NextTaskIds != null)
                {
                    var workflowTasks = allTasks.Where(t => t.DagWorkflowId == completedTask.DagWorkflowId).ToList();
                    
                    foreach (var nextTaskId in completedTask.NextTaskIds)
                    {
                        var nextTask = workflowTasks.FirstOrDefault(t => t.Id == nextTaskId);
                        if (nextTask != null)
                        {
                            // 增加已完成依赖计数
                            nextTask.CompletedDependenciesCount++;
                            
                            // 检查是否可以执行
                            if (await CanExecuteTaskAsync(nextTask, workflowTasks))
                            {
                                nextTask.DagStatus = DagExecutionStatus.Ready;
                                triggeredTasks.Add(nextTask);
                                
                                _logger.LogInformation("任务 {CompletedTaskName} 完成后，触发后续任务 {NextTaskName}",
                                    completedTask.Name, nextTask.Name);
                            }
                        }
                    }
                }

                // 更新工作流状态
                if (completedTask.DagWorkflowId.HasValue)
                {
                    await UpdateWorkflowStatusAsync(completedTask.DagWorkflowId.Value, allTasks);
                }

                return triggeredTasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理任务完成事件时发生错误: {TaskId}", completedTaskId);
                return triggeredTasks;
            }
        }

        /// <summary>
        /// 检查任务是否可以执行（依赖是否满足）
        /// </summary>
        public async Task<bool> CanExecuteTaskAsync(ScheduledTask task, IEnumerable<ScheduledTask> allTasks)
        {
            if (task.Dependencies == null || !task.Dependencies.Any())
            {
                return true;
            }

            var taskDict = allTasks.ToDictionary(t => t.Id);
            
            foreach (var dependencyId in task.Dependencies)
            {
                if (!taskDict.ContainsKey(dependencyId))
                {
                    return false;
                }

                var dependencyTask = taskDict[dependencyId];
                if (dependencyTask.DagStatus != DagExecutionStatus.Completed)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取工作流执行状态
        /// </summary>
        public async Task<WorkflowExecutionStatus> GetWorkflowStatusAsync(Guid workflowId)
        {
            if (_workflowStatuses.ContainsKey(workflowId))
            {
                return _workflowStatuses[workflowId];
            }

            return new WorkflowExecutionStatus
            {
                WorkflowId = workflowId,
                Status = WorkflowStatus.NotStarted
            };
        }

        /// <summary>
        /// 暂停工作流执行
        /// </summary>
        public async Task<bool> PauseWorkflowAsync(Guid workflowId)
        {
            if (_workflowStatuses.ContainsKey(workflowId))
            {
                _workflowStatuses[workflowId].Status = WorkflowStatus.Paused;
                _logger.LogInformation("工作流 {WorkflowId} 已暂停", workflowId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 恢复工作流执行
        /// </summary>
        public async Task<bool> ResumeWorkflowAsync(Guid workflowId)
        {
            if (_workflowStatuses.ContainsKey(workflowId))
            {
                _workflowStatuses[workflowId].Status = WorkflowStatus.Running;
                _logger.LogInformation("工作流 {WorkflowId} 已恢复", workflowId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 取消工作流执行
        /// </summary>
        public async Task<bool> CancelWorkflowAsync(Guid workflowId)
        {
            if (_workflowStatuses.ContainsKey(workflowId))
            {
                _workflowStatuses[workflowId].Status = WorkflowStatus.Cancelled;
                _workflowStatuses[workflowId].EndTime = DateTime.UtcNow;
                _logger.LogInformation("工作流 {WorkflowId} 已取消", workflowId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 更新工作流状态
        /// </summary>
        private async Task UpdateWorkflowStatusAsync(Guid workflowId, IEnumerable<ScheduledTask> allTasks)
        {
            if (!_workflowStatuses.ContainsKey(workflowId))
                return;

            var workflowStatus = _workflowStatuses[workflowId];
            var workflowTasks = allTasks.Where(t => t.DagWorkflowId == workflowId).ToList();
            var taskStatusDict = _taskStatuses.GetValueOrDefault(workflowId, new Dictionary<Guid, DagExecutionStatus>());

            // 统计各种状态的任务数量
            workflowStatus.CompletedTasks = workflowTasks.Count(t => t.DagStatus == DagExecutionStatus.Completed);
            workflowStatus.FailedTasks = workflowTasks.Count(t => t.DagStatus == DagExecutionStatus.Failed);
            workflowStatus.RunningTasks = workflowTasks.Count(t => t.DagStatus == DagExecutionStatus.Running);
            workflowStatus.WaitingTasks = workflowTasks.Count(t => t.DagStatus == DagExecutionStatus.Waiting);
            workflowStatus.SkippedTasks = workflowTasks.Count(t => t.DagStatus == DagExecutionStatus.Skipped);

            // 判断工作流状态
            if (workflowStatus.Status == WorkflowStatus.Running)
            {
                if (workflowStatus.FailedTasks > 0 && workflowStatus.CompletedTasks + workflowStatus.FailedTasks == workflowStatus.TotalTasks)
                {
                    workflowStatus.Status = workflowStatus.FailedTasks == workflowStatus.TotalTasks ? 
                        WorkflowStatus.Failed : WorkflowStatus.PartiallyFailed;
                    workflowStatus.EndTime = DateTime.UtcNow;
                }
                else if (workflowStatus.CompletedTasks == workflowStatus.TotalTasks)
                {
                    workflowStatus.Status = WorkflowStatus.Completed;
                    workflowStatus.EndTime = DateTime.UtcNow;
                }
            }

            _logger.LogDebug("工作流 {WorkflowId} 状态更新: 完成={Completed}, 失败={Failed}, 运行中={Running}, 等待={Waiting}",
                workflowId, workflowStatus.CompletedTasks, workflowStatus.FailedTasks, 
                workflowStatus.RunningTasks, workflowStatus.WaitingTasks);
        }
    }
}
