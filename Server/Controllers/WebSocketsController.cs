using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wbskt.Common;
using Wbskt.Common.Extensions;
using Wbskt.Server.Services;

namespace Wbskt.Server.Controllers;

[Route("ws")]
[ApiController]
[Authorize(AuthenticationSchemes = Constants.AuthSchemes.ClientScheme)]
public class WebSocketsController(ILogger<WebSocketsController> logger, IWebSocketContainer webSocketContainer, IClientService clientService, IServerInfoService serverInfo) : ControllerBase
{
    private readonly ILogger<WebSocketsController> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IWebSocketContainer webSocketContainer = webSocketContainer ?? throw new ArgumentNullException(nameof(webSocketContainer));
    private readonly IClientService clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
    private readonly IServerInfoService serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));

    [HttpGet]
    public async Task ConnectAsync()
    {
        var tid = User.GetTokenId();
        var csid = User.GetChannelSubscriberId();
        var cid = User.GetClientId();
        var sid = User.GetSocketServerId();
        var cname = User.GetClientName();

        if (sid != serverInfo.GetCurrentServerId() && !clientService.VerifyAndInvalidateToken(cid, tid)) // verify and invalidate token
        {
            logger.LogWarning("client is not authorized to make connect to this server({serverId}), or the provided token is expired or used", cname);
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        else if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            try
            {
                await webSocketContainer.Listen(webSocket, csid, cid);
            }
            catch (Exception ex)
            {
                logger.LogError("unexpected error while keeping the connection:{clientName}-{clientId} : {error}", cname, cid , ex.Message);
                logger.LogTrace("unexpected error while keeping the connection:{clientName}-{clientId} : {error}", cname, cid , ex.ToString());
            }
        }
        else
        {
            logger.LogWarning("attempted request by client:{clientName} is not a websocket request", cname);
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
