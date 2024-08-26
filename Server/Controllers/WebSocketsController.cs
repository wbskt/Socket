using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wbskt.Server.Services;

namespace Wbskt.Server.Controllers
{
    [Route("ws")]
    [ApiController]
    [Authorize]
    public class WebSocketsController : ControllerBase
    {
        private readonly ILogger<WebSocketsController> logger;
        private readonly IWebSocketContainer webSocketContainer;
        private readonly IClientService clientService;
        private readonly IServerInfoService serverInfo;

        public WebSocketsController(ILogger<WebSocketsController> logger, IWebSocketContainer webSocketContainer, IClientService clientService, IServerInfoService serverInfo)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.webSocketContainer = webSocketContainer ?? throw new ArgumentNullException(nameof(webSocketContainer));
            this.clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
            this.serverInfo = serverInfo ?? throw new ArgumentNullException(nameof(serverInfo));
        }

        public async Task ConnectAsync()
        {
            var tid = User.GetTokenId();
            var csid = User.GetChannelSubscriberId();
            var cid = User.GetClientId();
            var sid = User.GetServerId();

            if (sid != serverInfo.GetCurrentServerId() && !clientService.VerifyAndInvalidateToken(cid, tid)) // verify and invalidate token
            {
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
                    logger.LogError(ex.Message);
                    logger.LogTrace(new EventId(0), ex, ex.Message);
                }
                finally
                {
                    webSocket.Dispose();
                }
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
