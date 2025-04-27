using System.Net.WebSockets;

namespace Wbskt.Socket.Service.Services;

public interface IWebSocketContainer
{
    Task Listen(WebSocket webSocket, Guid channelSubscriberId, int clientId, CancellationToken ct);

    void SendMessage(Guid publisherId, string message);
}
