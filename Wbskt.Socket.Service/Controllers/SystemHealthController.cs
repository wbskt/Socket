using Microsoft.AspNetCore.Mvc;
using Wbskt.Common.Contracts;
using Wbskt.Socket.Service.Services;

namespace Wbskt.Socket.Service.Controllers;

[Route("health")]
[ApiController]
// [Authorize(AuthenticationSchemes = Constants.AuthSchemes.CoreServerScheme)] todo: add it back
public class SystemHealthController(ILogger<SystemHealthController> logger, IWebSocketContainer socketContainer) : ControllerBase
{
    [HttpGet("/ping")]
    public IActionResult Ping()
    {
        return Ok();
    }

    [HttpGet]
    public ActionResult<SocketServerHealth> GetHealth()
    {
        var health = new SocketServerHealth
        {
            ActiveConnections = socketContainer.GetActiveClients()
        };
        return health;
    }
}
