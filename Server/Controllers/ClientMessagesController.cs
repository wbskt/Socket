using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wbskt.Common;
using Wbskt.Common.Contracts;
using Wbskt.Server.Services;

namespace Wbskt.Server.Controllers;

[ApiController]
[Route("dispatch/{publisherId:guid}")]
[Authorize(AuthenticationSchemes = Constants.AuthSchemes.CoreServerScheme)]
public class ClientMessagesController(ILogger<ClientMessagesController> logger, IWebSocketContainer socketContainer) : ControllerBase
{
    [HttpGet("")]
    public IActionResult SendMessage(Guid publisherId)
    {
        var payload = new ClientPayload();
        return SendMessage(publisherId, payload);
    }

    [HttpPost("")]
    public IActionResult SendMessage(Guid publisherId, ClientPayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        logger.LogDebug("dispatching message: {payload} to {publisherId}", json, publisherId);
        socketContainer.SendMessage(publisherId, json);
        return Ok();
    }
}
