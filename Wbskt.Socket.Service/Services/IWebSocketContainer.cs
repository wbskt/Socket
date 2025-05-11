using System.Net.WebSockets;
using Wbskt.Common.Contracts;

namespace Wbskt.Socket.Service.Services;

public interface IWebSocketContainer
{
    Task Listen(WebSocket webSocket, Guid channelSubscriberId, int clientId, CancellationToken ct);

    void SendMessage(ClientPayload payload);

    Connection[] GetActiveClients();
}
