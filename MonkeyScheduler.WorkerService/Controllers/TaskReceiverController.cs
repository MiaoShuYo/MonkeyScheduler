using Microsoft.AspNetCore.Mvc;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.WorkerService.Services;
using Microsoft.Extensions.Logging;

namespace MonkeyScheduler.WorkerService.Controllers
{
    [ApiController]
    [Route("api/task")]
    public class TaskReceiverController : ControllerBase
    {
        private readonly ITaskExecutor _executor;
        private readonly IStatusReporterService _statusReporter;
        private readonly ILogger<TaskReceiverController> _logger;

        public TaskReceiverController(
            ITaskExecutor executor, 
            IStatusReporterService statusReporter,
            ILogger<TaskReceiverController> logger)
        {
            _executor = executor;
            _statusReporter = statusReporter;
            _logger = logger;
        }

        [HttpPost("execute")]
        public async Task<IActionResult> Execute([FromBody] ScheduledTask? task)
        {
            if (task == null)
            {
                return BadRequest("任务不能为空");
            }

            try
            {
                _logger.LogInformation("收到任务执行请求: {TaskName}", task.Name);
                
                await _executor.ExecuteAsync(task, async result =>
                {
                    await _statusReporter.ReportStatusAsync(result);
                });
                
                _logger.LogInformation("任务执行完成: {TaskName}", task.Name);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "任务执行失败: {TaskName}", task.Name);
                
                // 上报失败状态
                await _statusReporter.ReportStatusAsync(new TaskExecutionResult
                {
                    TaskId = task.Id,
                    Status = ExecutionStatus.Failed,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    ErrorMessage = ex.Message,
                    Success = false,
                    WorkerNodeUrl = string.Empty,
                    StackTrace = ex.StackTrace ?? string.Empty
                });
                
                return StatusCode(500, $"任务执行失败: {ex.Message}");
            }
        }
    }
} 