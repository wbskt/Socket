using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Wbskt.Common;
using Wbskt.Common.Contracts;
using Wbskt.Common.Extensions;

namespace Wbskt.Socket.Service.Services;

public class CoreServerConnection(ILogger<CoreServerConnection> logger, IConfiguration configuration)
{
    private ClientWebSocket ws;
    private static ServerInfo _currentServerInfo = new() { Type = Constants.ServerType.SocketServer };

    public async Task Connect(ServerInfo wbsktServerInfo, ServerInfo currentServerInfo, CancellationToken cancellationToken)
    {
        _currentServerInfo = currentServerInfo;
        var token = CreateCoreServerToken();
        var wsUri = new Uri($"wss://{wbsktServerInfo.GetAddressWithFallback()}/ws");
        var webSocket = new ClientWebSocket();
        webSocket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        logger.LogInformation("trying to connect: {wsUri}", wsUri);
        await webSocket.ConnectAsync(wsUri, CancellationToken.None);
        logger.LogInformation("connection established to: {wsUri}", wsUri);

        await webSocket.WriteAsync(currentServerInfo.GetAddressWithFallback(), CancellationToken.None);

        cancellationToken.Register(() => CloseClientConnection(logger, webSocket).Wait(CancellationToken.None));
        while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var (receiveResult, message) = await webSocket.ReadAsync(CancellationToken.None);

            if (receiveResult.MessageType == WebSocketMessageType.Close && webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
            {
                logger.LogInformation("closing connection ({closeStatus})", receiveResult.CloseStatusDescription);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection (socket server ack)", CancellationToken.None);
                break;
            }
        }
    }

    private string CreateCoreServerToken()
    {
        var tokenHandler = new JsonWebTokenHandler();
        var configurationKey = configuration[Constants.JwtKeyNames.SocketServerTokenKey];

        var key = Encoding.UTF8.GetBytes(configurationKey!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(Constants.Claims.SocketServer, $"{_currentServerInfo.ServerId}|{_currentServerInfo.GetAddressWithFallback()}"),
            }),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
            Expires = DateTime.Now.AddMinutes(Constants.ExpiryTimes.ServerTokenExpiry)
        };

        logger.LogDebug("socket server token created");
        return tokenHandler.CreateToken(tokenDescriptor);
    }

    private static async Task CloseClientConnection(ILogger logger, ClientWebSocket ws)
    {
        if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
        {
            logger.LogInformation("closing connection ({closeStatus})", "Closing connection (client initiated)");
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection (client initiated)", CancellationToken.None);
        }
    }
}
