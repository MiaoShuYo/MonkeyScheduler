using MonkeyScheduler.Core.Models;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.Core.Services
{
    /// <summary>
    /// DAG依赖检查器实现
    /// 负责检查任务依赖关系、循环依赖检测等
    /// </summary>
    public class DagDependencyChecker : IDagDependencyChecker
    {
        private readonly ILogger<DagDependencyChecker> _logger;

        public DagDependencyChecker(ILogger<DagDependencyChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 检查任务依赖关系是否有效
        /// </summary>
        public async Task<DependencyCheckResult> CheckDependenciesAsync(ScheduledTask task, IEnumerable<ScheduledTask> allTasks)
        {
            var result = new DependencyCheckResult();
            
            if (task.Dependencies == null || !task.Dependencies.Any())
            {
                result.IsValid = true;
                return result;
            }

            var taskDict = allTasks.ToDictionary(t => t.Id);
            var missingDependencies = new List<Guid>();
            var invalidDependencies = new List<Guid>();

            foreach (var dependencyId in task.Dependencies)
            {
                if (!taskDict.ContainsKey(dependencyId))
                {
                    missingDependencies.Add(dependencyId);
                }
                else
                {
                    var dependencyTask = taskDict[dependencyId];
                    if (!dependencyTask.Enabled)
                    {
                        invalidDependencies.Add(dependencyId);
                    }
                }
            }

            result.MissingDependencies = missingDependencies;
            result.InvalidDependencies = invalidDependencies;
            result.IsValid = !missingDependencies.Any() && !invalidDependencies.Any();

            if (!result.IsValid)
            {
                result.ErrorMessage = $"任务 '{task.Name}' 的依赖检查失败：" +
                    $"缺失依赖: {string.Join(", ", missingDependencies)}，" +
                    $"无效依赖: {string.Join(", ", invalidDependencies)}";
                
                _logger.LogWarning("任务依赖检查失败: {TaskName}, 错误: {ErrorMessage}", 
                    task.Name, result.ErrorMessage);
            }

            return result;
        }

        /// <summary>
        /// 检测循环依赖
        /// </summary>
        public async Task<CycleDetectionResult> DetectCyclesAsync(IEnumerable<ScheduledTask> tasks)
        {
            var result = new CycleDetectionResult();
            var taskDict = tasks.ToDictionary(t => t.Id);
            var visited = new HashSet<Guid>();
            var recursionStack = new HashSet<Guid>();
            var cycles = new List<List<Guid>>();

            foreach (var task in tasks)
            {
                if (!visited.Contains(task.Id))
                {
                    var path = new List<Guid>();
                    if (HasCycleUtil(task.Id, taskDict, visited, recursionStack, path, cycles))
                    {
                        result.HasCycle = true;
                    }
                }
            }

            result.Cycles = cycles;
            
            if (result.HasCycle)
            {
                result.ErrorMessage = $"检测到 {cycles.Count} 个循环依赖";
                _logger.LogError("检测到循环依赖: {Cycles}", 
                    string.Join("; ", cycles.Select(c => string.Join(" -> ", c))));
            }

            return result;
        }

        /// <summary>
        /// 递归检测循环依赖的辅助方法
        /// </summary>
        private bool HasCycleUtil(Guid taskId, Dictionary<Guid, ScheduledTask> taskDict, 
            HashSet<Guid> visited, HashSet<Guid> recursionStack, List<Guid> path, List<List<Guid>> cycles)
        {
            if (!taskDict.ContainsKey(taskId))
                return false;

            if (recursionStack.Contains(taskId))
            {
                // 找到循环，记录循环路径
                var cycleStartIndex = path.IndexOf(taskId);
                if (cycleStartIndex >= 0)
                {
                    var cycle = path.Skip(cycleStartIndex).ToList();
                    cycle.Add(taskId); // 闭合循环
                    cycles.Add(cycle);
                }
                return true;
            }

            if (visited.Contains(taskId))
                return false;

            visited.Add(taskId);
            recursionStack.Add(taskId);
            path.Add(taskId);

            var task = taskDict[taskId];
            if (task.Dependencies != null)
            {
                foreach (var dependencyId in task.Dependencies)
                {
                    if (HasCycleUtil(dependencyId, taskDict, visited, recursionStack, path, cycles))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(taskId);
            path.RemoveAt(path.Count - 1);
            return false;
        }

        /// <summary>
        /// 获取任务的所有依赖路径
        /// </summary>
        public async Task<List<List<Guid>>> GetDependencyPathsAsync(Guid taskId, IEnumerable<ScheduledTask> allTasks)
        {
            var taskDict = allTasks.ToDictionary(t => t.Id);
            var paths = new List<List<Guid>>();
            var currentPath = new List<Guid>();

            if (taskDict.ContainsKey(taskId))
            {
                GetDependencyPathsUtil(taskId, taskDict, currentPath, paths);
            }

            return paths;
        }

        /// <summary>
        /// 递归获取依赖路径的辅助方法
        /// </summary>
        private void GetDependencyPathsUtil(Guid taskId, Dictionary<Guid, ScheduledTask> taskDict, 
            List<Guid> currentPath, List<List<Guid>> paths)
        {
            if (currentPath.Contains(taskId))
            {
                // 避免循环依赖
                return;
            }

            currentPath.Add(taskId);
            var task = taskDict[taskId];

            if (task.Dependencies == null || !task.Dependencies.Any())
            {
                // 到达根节点，记录路径
                paths.Add(new List<Guid>(currentPath));
            }
            else
            {
                foreach (var dependencyId in task.Dependencies)
                {
                    if (taskDict.ContainsKey(dependencyId))
                    {
                        GetDependencyPathsUtil(dependencyId, taskDict, currentPath, paths);
                    }
                }
            }

            currentPath.RemoveAt(currentPath.Count - 1);
        }

        /// <summary>
        /// 验证DAG工作流的完整性
        /// </summary>
        public async Task<WorkflowValidationResult> ValidateWorkflowAsync(Guid workflowId, IEnumerable<ScheduledTask> tasks)
        {
            var result = new WorkflowValidationResult();
            var workflowTasks = tasks.Where(t => t.DagWorkflowId == workflowId).ToList();

            if (!workflowTasks.Any())
            {
                result.IsValid = false;
                result.ErrorMessage = $"工作流 {workflowId} 中没有找到任何任务";
                return result;
            }

            // 检查循环依赖
            var cycleResult = await DetectCyclesAsync(workflowTasks);
            if (cycleResult.HasCycle)
            {
                result.IsValid = false;
                result.ErrorMessage = $"工作流 {workflowId} 存在循环依赖: {cycleResult.ErrorMessage}";
                return result;
            }

            // 找到入口任务（没有依赖的任务）
            result.EntryTasks = workflowTasks
                .Where(t => t.Dependencies == null || !t.Dependencies.Any())
                .Select(t => t.Id)
                .ToList();

            // 找到出口任务（没有后续任务的任务）
            var allNextTaskIds = workflowTasks
                .Where(t => t.NextTaskIds != null)
                .SelectMany(t => t.NextTaskIds!)
                .ToHashSet();

            result.ExitTasks = workflowTasks
                .Where(t => !allNextTaskIds.Contains(t.Id))
                .Select(t => t.Id)
                .ToList();

            // 计算执行层级
            result.ExecutionLevels = CalculateExecutionLevels(workflowTasks);

            // 验证工作流完整性
            if (!result.EntryTasks.Any())
            {
                result.IsValid = false;
                result.ErrorMessage = $"工作流 {workflowId} 没有入口任务";
                return result;
            }

            if (!result.ExitTasks.Any())
            {
                result.IsValid = false;
                result.ErrorMessage = $"工作流 {workflowId} 没有出口任务";
                return result;
            }

            result.IsValid = true;
            _logger.LogInformation("工作流 {WorkflowId} 验证通过，入口任务: {EntryTasks}, 出口任务: {ExitTasks}, 层级数: {LevelCount}",
                workflowId, string.Join(", ", result.EntryTasks), 
                string.Join(", ", result.ExitTasks), result.ExecutionLevels.Count);

            return result;
        }

        /// <summary>
        /// 计算任务执行层级
        /// </summary>
        private Dictionary<int, List<Guid>> CalculateExecutionLevels(List<ScheduledTask> tasks)
        {
            var levels = new Dictionary<int, List<Guid>>();
            var taskDict = tasks.ToDictionary(t => t.Id);
            var inDegree = new Dictionary<Guid, int>();
            var queue = new Queue<Guid>();

            // 初始化入度
            foreach (var task in tasks)
            {
                inDegree[task.Id] = 0;
            }

            // 计算入度
            foreach (var task in tasks)
            {
                if (task.Dependencies != null)
                {
                    foreach (var dependencyId in task.Dependencies)
                    {
                        if (taskDict.ContainsKey(dependencyId))
                        {
                            inDegree[task.Id]++;
                        }
                    }
                }
            }

            // 找到入度为0的任务（入口任务）
            foreach (var task in tasks)
            {
                if (inDegree[task.Id] == 0)
                {
                    queue.Enqueue(task.Id);
                }
            }

            int level = 0;
            while (queue.Count > 0)
            {
                var levelSize = queue.Count;
                var currentLevel = new List<Guid>();

                for (int i = 0; i < levelSize; i++)
                {
                    var taskId = queue.Dequeue();
                    currentLevel.Add(taskId);
                    levels[level] = currentLevel;

                    var task = taskDict[taskId];
                    if (task.NextTaskIds != null)
                    {
                        foreach (var nextTaskId in task.NextTaskIds)
                        {
                            if (taskDict.ContainsKey(nextTaskId))
                            {
                                inDegree[nextTaskId]--;
                                if (inDegree[nextTaskId] == 0)
                                {
                                    queue.Enqueue(nextTaskId);
                                }
                            }
                        }
                    }
                }

                level++;
            }

            return levels;
        }
    }
}
