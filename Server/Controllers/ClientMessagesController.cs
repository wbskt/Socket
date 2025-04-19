using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wbskt.Common;
using Wbskt.Server.Services;

namespace Wbskt.Server.Controllers;

[ApiController]
[Route("dispatch/{channelSubscriberId}")]
[Authorize(AuthenticationSchemes = Constants.AuthSchemes.CoreServerScheme)]
public class ClientMessagesController(ILogger<ClientMessagesController> logger, IWebSocketContainer socketContainer) : ControllerBase
{
    [HttpGet("")]
    public IActionResult SendMessage(Guid channelSubscriberId)
    {
        var payload = new ClientPayload();
        return SendMessage(channelSubscriberId, payload);
    }

    [HttpPost("")]
    public IActionResult SendMessage(Guid channelSubscriberId, ClientPayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        logger.LogDebug("dispatching message: {payload} to {channelSubscriptionId}", json, channelSubscriberId);
        socketContainer.SendMessage(channelSubscriberId, json);
        return Ok();
    }
}
