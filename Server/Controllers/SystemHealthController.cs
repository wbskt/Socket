using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wbskt.Common;

namespace Wbskt.Server.Controllers;

[Route("ping")]
[ApiController]
[Authorize(AuthenticationSchemes = Constants.AuthSchemes.CoreServerScheme)]
public class SystemHealthController(ILogger<SystemHealthController> logger) : ControllerBase
{
    private readonly ILogger<SystemHealthController> logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpGet]
    public IActionResult Ping()
    {
        logger.LogDebug("ping-pong");
        return Ok("pong");
    }
}
