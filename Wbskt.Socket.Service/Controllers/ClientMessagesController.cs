using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wbskt.Common;
using Wbskt.Common.Contracts;
using Wbskt.Socket.Service.Services;

namespace Wbskt.Socket.Service.Controllers;

[ApiController]
[Route("dispatch/{publisherId:guid}")]
[Authorize(AuthenticationSchemes = Constants.AuthSchemes.CoreServerScheme)]
public class ClientMessagesController(ILogger<ClientMessagesController> logger, IWebSocketContainer socketContainer) : ControllerBase
{
    [HttpPost("")]
    public IActionResult SendMessage(Guid publisherId, ClientPayload payload)
    {
        socketContainer.SendMessage(publisherId, payload);
        return Ok();
    }
}
