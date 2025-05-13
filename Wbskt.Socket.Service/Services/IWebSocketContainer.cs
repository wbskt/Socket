using System.Net.WebSockets;
using Wbskt.Common.Contracts;

namespace Wbskt.Socket.Service.Services;

public interface IWebSocketContainer
{
    Task Listen(WebSocket webSocket, Guid[] channelSubscriberIds, int clientId);

    void SendMessage(ClientPayload payload);

    bool ConnectionExists(int clientId);

    void AddChannelsForClient(Guid[] channelSubscriberIds, int clientId);
}
