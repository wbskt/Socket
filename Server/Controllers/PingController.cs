using Microsoft.AspNetCore.Mvc;

namespace Wbskt.Server.Controllers
{
    [Route("ping")]
    [ApiController]
    public class PingController : ControllerBase
    {
        private readonly ILogger<PingController> logger;

        public PingController(ILogger<PingController> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Ping()
        {
            logger.LogInformation("ping-pong");
            return Ok("pong");
        }
    }
}
