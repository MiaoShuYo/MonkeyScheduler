using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MonkeyScheduler.Core.Services;

namespace MonkeyScheduler.SchedulerService.Controllers
{
    /// <summary>
    /// 任务处理器管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TaskHandlersController : ControllerBase
    {
        private readonly ITaskHandlerFactory _handlerFactory;
        private readonly ILogger<TaskHandlersController> _logger;

        public TaskHandlersController(ITaskHandlerFactory handlerFactory, ILogger<TaskHandlersController> logger)
        {
            _handlerFactory = handlerFactory;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有支持的任务类型
        /// </summary>
        /// <returns>任务类型列表</returns>
        [HttpGet("types")]
        public ActionResult<IEnumerable<string>> GetSupportedTaskTypes()
        {
            var types = _handlerFactory.GetSupportedTaskTypes();
            return Ok(types);
        }

        /// <summary>
        /// 获取任务处理器配置
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <returns>处理器配置</returns>
        [HttpGet("config/{taskType}")]
        public ActionResult<object> GetHandlerConfiguration(string taskType)
        {
            try
            {
                var handler = _handlerFactory.GetHandler(taskType);
                var config = handler.GetConfiguration();
                return Ok(config);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 验证任务参数
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <param name="parameters">任务参数</param>
        /// <returns>验证结果</returns>
        [HttpPost("validate/{taskType}")]
        public async Task<ActionResult<bool>> ValidateParameters(string taskType, [FromBody] object parameters)
        {
            try
            {
                var handler = _handlerFactory.GetHandler(taskType);
                var isValid = await handler.ValidateParametersAsync(parameters);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取所有任务处理器的配置信息
        /// </summary>
        /// <returns>所有处理器配置</returns>
        [HttpGet("configs")]
        public ActionResult<Dictionary<string, object>> GetAllHandlerConfigurations()
        {
            var configs = new Dictionary<string, object>();
            
            foreach (var taskType in _handlerFactory.GetSupportedTaskTypes())
            {
                try
                {
                    var handler = _handlerFactory.GetHandler(taskType);
                    var config = handler.GetConfiguration();
                    configs[taskType] = config;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取任务处理器配置失败: {TaskType}", taskType);
                    configs[taskType] = new { error = ex.Message };
                }
            }
            
            return Ok(configs);
        }

        /// <summary>
        /// 检查任务类型是否支持
        /// </summary>
        /// <param name="taskType">任务类型</param>
        /// <returns>是否支持</returns>
        [HttpGet("supported/{taskType}")]
        public ActionResult<bool> IsTaskTypeSupported(string taskType)
        {
            var isSupported = _handlerFactory.IsTaskTypeSupported(taskType);
            return Ok(isSupported);
        }
    }
}
