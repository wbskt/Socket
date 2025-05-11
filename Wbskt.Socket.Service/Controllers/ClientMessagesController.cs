using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wbskt.Common;
using Wbskt.Common.Contracts;
using Wbskt.Socket.Service.Services;

namespace Wbskt.Socket.Service.Controllers;

[ApiController]
[Route("dispatch/")]
[Authorize(AuthenticationSchemes = Constants.AuthSchemes.CoreServerScheme)]
public class ClientMessagesController(ILogger<ClientMessagesController> logger, IWebSocketContainer socketContainer) : ControllerBase
{
    [HttpPost("")]
    public IActionResult SendMessage(ClientPayload payload)
    {
        socketContainer.SendMessage(payload);
        return Ok();
    }
}
