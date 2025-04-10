using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Storage;

namespace MonkeyScheduler.SchedulerService.Controllers
{
    [ApiController]
    [Route("api/task")]
    public class TaskStatusController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;

        public TaskStatusController(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        [HttpPost("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] TaskExecutionResult result)
        {
            try
            {
                var task = _taskRepository.GetTask(result.TaskId);
                if (task == null)
                {
                    return NotFound($"任务 {result.TaskId} 不存在");
                }

                // 更新任务状态
                // TODO: 将执行结果保存到数据库
                Console.WriteLine($"任务 {task.Name} 状态更新: {result.Status}");
                Console.WriteLine($"开始时间: {result.StartTime}, 结束时间: {result.EndTime}");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"错误信息: {result.ErrorMessage}");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"更新任务状态失败: {ex.Message}");
            }
        }
    }
} 