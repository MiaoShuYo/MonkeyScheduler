using Microsoft.AspNetCore.Mvc;
using MonkeyScheduler.Core.Models;
using MonkeyScheduler.Core.Services;
using MonkeyScheduler.SchedulerService.Models;
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
        /// <summary>
        /// 初始化任务控制器
        /// </summary>
        /// <param name="taskRepository">任务仓储接口</param>
        /// <param name="taskExecutionResult">任务执行结果仓储接口</param>
        /// <exception cref="ArgumentNullException">当 taskRepository 为 null 时抛出</exception>
        public TasksController(ITaskRepository taskRepository,ITaskExecutionResult taskExecutionResult)
        {
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            _executionResult = taskExecutionResult ?? throw new ArgumentNullException(nameof(taskExecutionResult));
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
                Enabled = true,
                NextRunTime = DateTime.UtcNow
            };

            _taskRepository.AddTask(task);
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
            _taskRepository.UpdateTask(task);
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
            _taskRepository.UpdateTask(task);
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
    }
} 