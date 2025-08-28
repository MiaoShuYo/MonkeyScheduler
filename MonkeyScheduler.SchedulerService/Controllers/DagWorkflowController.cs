using Microsoft.AspNetCore.Mvc;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.Core.Models;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.SchedulerService.Controllers
{
    /// <summary>
    /// DAG工作流控制器
    /// 提供DAG工作流管理的API接口
    /// </summary>
    [ApiController]
    [Route("api/dag-workflow")]
    public class DagWorkflowController : ControllerBase
    {
        private readonly IDagDependencyChecker _dependencyChecker;
        private readonly IDagExecutionManager _executionManager;
        private readonly ILogger<DagWorkflowController> _logger;

        public DagWorkflowController(
            IDagDependencyChecker dependencyChecker,
            IDagExecutionManager executionManager,
            ILogger<DagWorkflowController> logger)
        {
            _dependencyChecker = dependencyChecker ?? throw new ArgumentNullException(nameof(dependencyChecker));
            _executionManager = executionManager ?? throw new ArgumentNullException(nameof(executionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 验证DAG工作流
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="tasks">工作流任务列表</param>
        /// <returns>验证结果</returns>
        [HttpPost("validate")]
        public async Task<ActionResult<WorkflowValidationResult>> ValidateWorkflow(
            [FromQuery] Guid workflowId,
            [FromBody] List<ScheduledTask> tasks)
        {
            try
            {
                var result = await _dependencyChecker.ValidateWorkflowAsync(workflowId, tasks);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证DAG工作流 {WorkflowId} 失败", workflowId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 检测循环依赖
        /// </summary>
        /// <param name="tasks">任务列表</param>
        /// <returns>循环依赖检测结果</returns>
        [HttpPost("detect-cycles")]
        public async Task<ActionResult<CycleDetectionResult>> DetectCycles([FromBody] List<ScheduledTask> tasks)
        {
            try
            {
                var result = await _dependencyChecker.DetectCyclesAsync(tasks);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测循环依赖失败");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 检查任务依赖关系
        /// </summary>
        /// <param name="request">依赖检查请求</param>
        /// <returns>依赖检查结果</returns>
        [HttpPost("check-dependencies")]
        public async Task<ActionResult<DependencyCheckResult>> CheckDependencies([FromBody] DependencyCheckRequest request)
        {
            try
            {
                var result = await _dependencyChecker.CheckDependenciesAsync(request.Task, request.AllTasks);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查任务依赖关系失败");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取任务依赖路径
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="tasks">所有任务列表</param>
        /// <returns>依赖路径列表</returns>
        [HttpPost("dependency-paths")]
        public async Task<ActionResult<List<List<Guid>>>> GetDependencyPaths(
            [FromQuery] Guid taskId,
            [FromBody] List<ScheduledTask> tasks)
        {
            try
            {
                var paths = await _dependencyChecker.GetDependencyPathsAsync(taskId, tasks);
                return Ok(paths);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取任务依赖路径失败: {TaskId}", taskId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 启动DAG工作流
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="tasks">工作流任务列表</param>
        /// <returns>执行结果</returns>
        [HttpPost("start")]
        public async Task<ActionResult<DagExecutionResult>> StartWorkflow(
            [FromQuery] Guid workflowId,
            [FromBody] List<ScheduledTask> tasks)
        {
            try
            {
                var result = await _executionManager.StartWorkflowAsync(workflowId, tasks);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动DAG工作流 {WorkflowId} 失败", workflowId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取工作流执行状态
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>工作流状态</returns>
        [HttpGet("status/{workflowId}")]
        public async Task<ActionResult<WorkflowExecutionStatus>> GetWorkflowStatus(Guid workflowId)
        {
            try
            {
                var status = await _executionManager.GetWorkflowStatusAsync(workflowId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取工作流状态失败: {WorkflowId}", workflowId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 暂停工作流执行
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>操作结果</returns>
        [HttpPost("pause/{workflowId}")]
        public async Task<ActionResult<bool>> PauseWorkflow(Guid workflowId)
        {
            try
            {
                var result = await _executionManager.PauseWorkflowAsync(workflowId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停工作流失败: {WorkflowId}", workflowId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 恢复工作流执行
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>操作结果</returns>
        [HttpPost("resume/{workflowId}")]
        public async Task<ActionResult<bool>> ResumeWorkflow(Guid workflowId)
        {
            try
            {
                var result = await _executionManager.ResumeWorkflowAsync(workflowId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复工作流失败: {WorkflowId}", workflowId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 取消工作流执行
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>操作结果</returns>
        [HttpPost("cancel/{workflowId}")]
        public async Task<ActionResult<bool>> CancelWorkflow(Guid workflowId)
        {
            try
            {
                var result = await _executionManager.CancelWorkflowAsync(workflowId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消工作流失败: {WorkflowId}", workflowId);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    /// <summary>
    /// 依赖检查请求
    /// </summary>
    public class DependencyCheckRequest
    {
        /// <summary>
        /// 要检查的任务
        /// </summary>
        public ScheduledTask Task { get; set; } = new();

        /// <summary>
        /// 所有任务列表
        /// </summary>
        public List<ScheduledTask> AllTasks { get; set; } = new();
    }
}
