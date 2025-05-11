using Microsoft.AspNetCore.Mvc;

namespace Wbskt.Socket.Service.Controllers;

[ApiController]
public class SystemHealthController(ILogger<SystemHealthController> logger) : ControllerBase
{
    [HttpGet("/ping")]
    public IActionResult Ping()
    {
        logger.LogTrace("Ping received at {Time}", DateTime.UtcNow);
        return Ok();
    }
}
