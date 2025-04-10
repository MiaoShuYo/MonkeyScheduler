using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MonkeyScheduler.SchedulerService;

namespace MonkeyScheduler.SchedulerService.Controllers
{
    [ApiController]
    [Route("api/worker")]
    public class WorkerApiController : ControllerBase
    {
        private readonly NodeRegistry _nodeRegistry;

        public WorkerApiController(NodeRegistry nodeRegistry)
        {
            _nodeRegistry = nodeRegistry;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] string nodeUrl)
        {
            _nodeRegistry.Register(nodeUrl);
            return Ok();
        }

        [HttpPost("heartbeat")]
        public IActionResult Heartbeat([FromBody] string nodeUrl)
        {
            _nodeRegistry.Heartbeat(nodeUrl);
            return Ok();
        }
    }
} 