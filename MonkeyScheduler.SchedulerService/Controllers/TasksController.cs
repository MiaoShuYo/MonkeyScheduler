using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService.Models;
using MonkeyScheduler.SchedulerService.Services;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.SchedulerService.Controllers
{
    /// <summary>
    /// 任务管理控制器，提供任务的 CRUD 操作和状态管理
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskExecutionResult _executionResult;
        private readonly IEnhancedTaskRetryManager _retryManager;
        private readonly ILogger<TasksController> _logger;

        /// <summary>
        /// 初始化任务控制器
        /// </summary>
        /// <param name="taskRepository">任务仓储接口</param>
        /// <param name="taskExecutionResult">任务执行结果仓储接口</param>
        /// <param name="retryManager">增强的重试管理器</param>
        /// <param name="logger">日志记录器</param>
        /// <exception cref="ArgumentNullException">当 taskRepository 为 null 时抛出</exception>
        public TasksController(
            ITaskRepository taskRepository,
            ITaskExecutionResult taskExecutionResult,
            IEnhancedTaskRetryManager retryManager,
            ILogger<TasksController> logger)
        {
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _executionResult = taskExecutionResult ?? throw new ArgumentNullException(nameof(taskExecutionResult));
            _retryManager = retryManager ?? throw new ArgumentNullException(nameof(retryManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建新任务
        /// </summary>
        /// <param name="request">创建任务的请求参数，包含任务名称和 Cron 表达式</param>
        /// <returns>
        /// 200 OK - 返回创建成功的任务信息
        /// 400 Bad Request - 当请求参数无效时返回
        /// </returns>
        [HttpPost]
        public IActionResult CreateTask([FromBody] CreateTaskRequest? request)
        {
            if (request == null)
                return BadRequest("请求体不能为空");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("任务名称不能为空");

            if (string.IsNullOrWhiteSpace(request.CronExpression))
                return BadRequest("Cron表达式不能为空");

            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                CronExpression = request.CronExpression,
                Description = request.Description ?? string.Empty,
                TaskType = request.TaskType ?? string.Empty,
                TaskParameters = request.TaskParameters ?? string.Empty,
                Enabled = true,
                EnableRetry = request.EnableRetry ?? true,
                MaxRetryCount = request.MaxRetryCount ?? 3,
                RetryIntervalSeconds = request.RetryIntervalSeconds ?? 60,
                RetryStrategy = request.RetryStrategy ?? RetryStrategy.Exponential,
                TimeoutSeconds = request.TimeoutSeconds ?? 300,
                NextRunTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _taskRepository.AddTask(task);
            _logger.LogInformation("创建新任务: {TaskName} (ID: {TaskId})", task.Name, task.Id);
            return Ok(task);
        }

        /// <summary>
        /// 启用指定任务
        /// </summary>
        /// <param name="id">要启用的任务ID</param>
        /// <returns>
        /// 200 OK - 任务启用成功
        /// 404 Not Found - 当指定ID的任务不存在时返回
        /// </returns>
        [HttpPut("{id}/enable")]
        public IActionResult EnableTask(Guid id)
        {
            var task = _taskRepository.GetTask(id);
            if (task == null)
                return NotFound($"任务 {id} 不存在");

            task.Enabled = true;
            task.LastModifiedAt = DateTime.UtcNow;
            _taskRepository.UpdateTask(task);
            
            _logger.LogInformation("启用任务: {TaskName} (ID: {TaskId})", task.Name, task.Id);
            return Ok();
        }

        /// <summary>
        /// 禁用指定任务
        /// </summary>
        /// <param name="id">要禁用的任务ID</param>
        /// <returns>
        /// 200 OK - 任务禁用成功
        /// 404 Not Found - 当指定ID的任务不存在时返回
        /// </returns>
        [HttpPut("{id}/disable")]
        public IActionResult DisableTask(Guid id)
        {
            var task = _taskRepository.GetTask(id);
            if (task == null)
                return NotFound($"任务 {id} 不存在");

            task.Enabled = false;
            task.LastModifiedAt = DateTime.UtcNow;
            _taskRepository.UpdateTask(task);
            
            _logger.LogInformation("禁用任务: {TaskName} (ID: {TaskId})", task.Name, task.Id);
            return Ok();
        }

        /// <summary>
        /// 获取所有任务列表
        /// </summary>
        /// <returns>200 OK - 返回所有任务的列表</returns>
        [HttpGet]
        public IActionResult GetTasks()
        {
            var tasks = _taskRepository.GetAllTasks();
            return Ok(tasks);
        }

        /// <summary>
        /// 获取指定ID的任务详情
        /// </summary>
        /// <param name="id">要获取的任务ID</param>
        /// <returns>
        /// 200 OK - 返回指定任务的详细信息
        /// 404 Not Found - 当指定ID的任务不存在时返回
        /// </returns>
        [HttpGet("{id}")]
        public IActionResult GetTask(Guid id)
        {
            var task = _taskRepository.GetTask(id);
            if (task == null)
                return NotFound($"任务 {id} 不存在");

            return Ok(task);
        }

        /// <summary>
        /// 删除指定ID的任务
        /// </summary>
        /// <param name="id">要删除的任务ID</param>
        /// <returns>
        /// 200 OK - 任务删除成功
        /// 404 Not Found - 当指定ID的任务不存在时返回
        /// </returns>
        [HttpDelete("{id}")]
        public IActionResult DeleteTask(Guid id)
        {
            var task = _taskRepository.GetTask(id);
            if (task == null)
                return NotFound($"任务 {id} 不存在");

            _taskRepository.DeleteTask(id);
            _logger.LogInformation("删除任务: {TaskName} (ID: {TaskId})", task.Name, task.Id);
            return Ok();
        }
        
        /// <summary>
        /// 上报任务执行结果
        /// </summary>
        /// <param name="result">任务执行结果，包含执行状态、开始时间、结束时间等信息</param>
        /// <returns>
        /// 200 OK - 结果上报成功
        /// 400 Bad Request - 当请求参数无效时返回
        /// </returns>
        [HttpPost("status")]
        public IActionResult ReportTaskStatus([FromBody] TaskExecutionResult? result)
        {
            if (result == null)
                return BadRequest("请求体不能为空");

            // 处理任务执行结果
            // 保存数据库
            _executionResult.AddExecutionResultAsync(result);
            return Ok();
        }

        /// <summary>
        /// 重置任务的重试状态
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns>
        /// 200 OK - 重试状态重置成功
        /// 404 Not Found - 当指定ID的任务不存在时返回
        /// </returns>
        [HttpPost("{id}/reset-retry")]
        public IActionResult ResetTaskRetry(Guid id)
        {
            var task = _taskRepository.GetTask(id);
            if (task == null)
                return NotFound($"任务 {id} 不存在");

            _retryManager.ResetRetryState(task);
            task.LastModifiedAt = DateTime.UtcNow;
            _taskRepository.UpdateTask(task);
            
            _logger.LogInformation("重置任务重试状态: {TaskName} (ID: {TaskId})", task.Name, task.Id);
            return Ok(new { message = "重试状态已重置" });
        }

        /// <summary>
        /// 手动重试任务
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns>
        /// 200 OK - 任务重试成功
        /// 404 Not Found - 当指定ID的任务不存在时返回
        /// 400 Bad Request - 当任务无法重试时返回
        /// </returns>
        [HttpPost("{id}/retry")]
        public async Task<IActionResult> RetryTask(Guid id)
        {
            var task = _taskRepository.GetTask(id);
            if (task == null)
                return NotFound($"任务 {id} 不存在");

            if (!task.EnableRetry)
            {
                return BadRequest("该任务未启用重试机制");
            }

            if (task.CurrentRetryCount >= task.MaxRetryCount)
            {
                return BadRequest("任务已达到最大重试次数");
            }

            try
            {
                // 手动触发重试
                var retrySuccess = await _retryManager.RetryTaskAsync(task, string.Empty);
                task.LastModifiedAt = DateTime.UtcNow;
                _taskRepository.UpdateTask(task);

                if (retrySuccess)
                {
                    _logger.LogInformation("手动重试任务成功: {TaskName} (ID: {TaskId})", task.Name, task.Id);
                    return Ok(new { message = "任务重试成功" });
                }
                else
                {
                    _logger.LogWarning("手动重试任务失败: {TaskName} (ID: {TaskId})", task.Name, task.Id);
                    return BadRequest("任务重试失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手动重试任务时发生错误: {TaskName} (ID: {TaskId})", task.Name, task.Id);
                return StatusCode(500, "重试过程中发生错误");
            }
        }

        /// <summary>
        /// 获取任务的重试信息
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <returns>
        /// 200 OK - 返回任务的重试信息
        /// 404 Not Found - 当指定ID的任务不存在时返回
        /// </returns>
        [HttpGet("{id}/retry-info")]
        public IActionResult GetTaskRetryInfo(Guid id)
        {
            var task = _taskRepository.GetTask(id);
            if (task == null)
                return NotFound($"任务 {id} 不存在");

            var retryInfo = new
            {
                TaskId = task.Id,
                TaskName = task.Name,
                EnableRetry = task.EnableRetry,
                MaxRetryCount = task.MaxRetryCount,
                CurrentRetryCount = task.CurrentRetryCount,
                RetryIntervalSeconds = task.RetryIntervalSeconds,
                RetryStrategy = task.RetryStrategy.ToString(),
                NextRetryTime = task.NextRetryTime,
                CanRetry = _retryManager.ShouldRetryTask(task),
                NextRetryTimeCalculated = _retryManager.CalculateNextRetryTime(task)
            };

            return Ok(retryInfo);
        }

        /// <summary>
        /// 更新任务的重试配置
        /// </summary>
        /// <param name="id">任务ID</param>
        /// <param name="retryConfig">重试配置</param>
        /// <returns>
        /// 200 OK - 重试配置更新成功
        /// 404 Not Found - 当指定ID的任务不存在时返回
        /// 400 Bad Request - 当重试配置无效时返回
        /// </returns>
        [HttpPut("{id}/retry-config")]
        public IActionResult UpdateTaskRetryConfig(Guid id, [FromBody] TaskRetryConfigRequest retryConfig)
        {
            if (retryConfig == null)
                return BadRequest("重试配置不能为空");

            var task = _taskRepository.GetTask(id);
            if (task == null)
                return NotFound($"任务 {id} 不存在");

            // 验证重试配置
            if (retryConfig.MaxRetryCount.HasValue && retryConfig.MaxRetryCount.Value < 0)
            {
                return BadRequest("最大重试次数不能为负数");
            }

            if (retryConfig.RetryIntervalSeconds.HasValue && retryConfig.RetryIntervalSeconds.Value < 0)
            {
                return BadRequest("重试间隔不能为负数");
            }

            // 更新重试配置
            if (retryConfig.EnableRetry.HasValue)
                task.EnableRetry = retryConfig.EnableRetry.Value;

            if (retryConfig.MaxRetryCount.HasValue)
                task.MaxRetryCount = retryConfig.MaxRetryCount.Value;

            if (retryConfig.RetryIntervalSeconds.HasValue)
                task.RetryIntervalSeconds = retryConfig.RetryIntervalSeconds.Value;

            if (retryConfig.RetryStrategy.HasValue)
                task.RetryStrategy = retryConfig.RetryStrategy.Value;

            if (retryConfig.TimeoutSeconds.HasValue)
                task.TimeoutSeconds = retryConfig.TimeoutSeconds.Value;

            task.LastModifiedAt = DateTime.UtcNow;
            _taskRepository.UpdateTask(task);

            _logger.LogInformation("更新任务重试配置: {TaskName} (ID: {TaskId})", task.Name, task.Id);
            return Ok(new { message = "重试配置已更新" });
        }
    }

    /// <summary>
    /// 任务重试配置请求模型
    /// </summary>
    public class TaskRetryConfigRequest
    {
        public bool? EnableRetry { get; set; }
        public int? MaxRetryCount { get; set; }
        public int? RetryIntervalSeconds { get; set; }
        public RetryStrategy? RetryStrategy { get; set; }
        public int? TimeoutSeconds { get; set; }
    }
} 