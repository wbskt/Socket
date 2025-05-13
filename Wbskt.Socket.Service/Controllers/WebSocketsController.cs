using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wbskt.Common;
using Wbskt.Common.Extensions;
using Wbskt.Socket.Service.Services;

namespace Wbskt.Socket.Service.Controllers;

[Route("ws")]
[ApiController]
[Authorize(AuthenticationSchemes = Constants.AuthSchemes.ClientScheme)]
public class WebSocketsController(
    ILogger<WebSocketsController> logger,
    IWebSocketContainer webSocketContainer,
    IServerInfoService serverInfo) : ControllerBase
{
    [HttpGet]
    public async Task ConnectAsync()
    {
        var csid = User.GetChannelSubscriberId();
        var cid = User.GetClientId();
        var sid = User.GetSocketServerId();
        var cname = User.GetClientName();

        if (sid != serverInfo.GetCurrentServerId()) // verify and invalidate token
        {
            logger.LogWarning("client is not authorized to make connect to this server({serverId})", cname);
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        else if (webSocketContainer.ConnectionExists(cid))
        {
            logger.LogWarning("client connection already exists with: {cname}", cname);
            logger.LogInformation("adding existing connection to the given list of channles");
            webSocketContainer.AddChannelsForClient([csid], cid);
            HttpContext.Response.StatusCode = StatusCodes.Status302Found;
        }
        else if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            try
            {
                await webSocketContainer.Listen(webSocket, [csid], cid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "unexpected error while keeping the connection:{clientName}-{clientId} : {error}", cname, cid, ex.Message);
            }
        }
        else
        {
            logger.LogWarning("attempted request by client:{clientName} is not a websocket request", cname);
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
