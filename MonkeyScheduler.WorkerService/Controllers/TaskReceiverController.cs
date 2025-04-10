using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MonkeyScheduler.Core;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.WorkerService.Services;

namespace MonkeyScheduler.WorkerService.Controllers
{
    [ApiController]
    [Route("api/task")]
    public class TaskReceiverController : ControllerBase
    {
        private readonly ITaskExecutor _executor;
        private readonly StatusReporterService _statusReporter;

        public TaskReceiverController(ITaskExecutor executor, StatusReporterService statusReporter)
        {
            _executor = executor;
            _statusReporter = statusReporter;
        }

        [HttpPost("execute")]
        public async Task<IActionResult> Execute([FromBody] ScheduledTask task)
        {
            try
            {
                await _executor.ExecuteAsync(task, async result =>
                {
                    await _statusReporter.ReportStatusAsync(result);
                });
                return Ok();
            }
            catch (Exception ex)
            {
                // 上报失败状态
                await _statusReporter.ReportStatusAsync(new TaskExecutionResult
                {
                    TaskId = task.Id,
                    Status = ExecutionStatus.Failed,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    ErrorMessage = ex.Message
                });
                return StatusCode(500, $"任务执行失败: {ex.Message}");
            }
        }
    }
} 