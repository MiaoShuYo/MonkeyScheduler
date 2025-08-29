using Microsoft.AspNetCore.Mvc;
using MonkeyScheduler.SchedulerService.Services;

namespace MonkeyScheduler.SchedulerService.Controllers
{
    /// <summary>
    /// 工作节点 API 控制器，用于管理工作节点的注册和心跳检测
    /// </summary>
    [ApiController]
    [Route("api/worker")]
    public class WorkerApiController : ControllerBase
    {
        private readonly INodeRegistry _nodeRegistry;

        /// <summary>
        /// 初始化工作节点 API 控制器
        /// </summary>
        /// <param name="nodeRegistry">节点注册表服务</param>
        /// <exception cref="ArgumentNullException">当 nodeRegistry 为 null 时抛出</exception>
        public WorkerApiController(INodeRegistry nodeRegistry)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
        }

        /// <summary>
        /// 注册新的工作节点
        /// </summary>
        /// <param name="nodeUrl">工作节点的 URL 地址</param>
        /// <returns>
        /// 200 OK - 节点注册成功
        /// 400 Bad Request - 当节点 URL 为空或无效时返回
        /// </returns>
        [HttpPost("register")]
        public IActionResult Register([FromBody] string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
            {
                return BadRequest();
            }

            _nodeRegistry.Register(nodeUrl);
            return Ok();
        }

        /// <summary>
        /// 接收工作节点的心跳信号
        /// </summary>
        /// <param name="nodeUrl">发送心跳的工作节点 URL 地址</param>
        /// <returns>
        /// 200 OK - 心跳接收成功
        /// 400 Bad Request - 当节点 URL 为空或无效时返回
        /// </returns>
        [HttpPost("heartbeat")]
        public IActionResult Heartbeat([FromBody] string nodeUrl)
        {
            if (string.IsNullOrWhiteSpace(nodeUrl))
            {
                return BadRequest();
            }

            _nodeRegistry.Heartbeat(nodeUrl);
            return Ok();
        }
    }
} 