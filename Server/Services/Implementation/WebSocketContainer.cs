using System.Net.WebSockets;
using Wbskt.Common;
using Wbskt.Common.Extensions;

namespace Wbskt.Server.Services.Implementation;

public class WebSocketContainer(ILogger<WebSocketContainer> logger, IClientService clientService) : IWebSocketContainer
{
    private readonly ILogger<WebSocketContainer> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IClientService clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
    private readonly Dictionary<Guid, HashSet<int>> subscriptionMap = new(); // init load all subscriptions assigned to this socket server
    private readonly Dictionary<(int ClientId, Guid SubacriptionId), WebSocket> clientMap = new();

    public async Task Listen(WebSocket webSocket, Guid channelSubscriberId, int clientId)
    {
        clientMap.Add((clientId, channelSubscriberId), webSocket);

        // todo: validate channel id and it's assigned to this socketserver.
        if (subscriptionMap.TryGetValue(channelSubscriberId, out var clientIds))
        {
            clientIds.Add(clientId);
        }
        else
        {
            subscriptionMap.Add(channelSubscriberId, new HashSet<int> { clientId });
        }

        try
        {
            logger.LogDebug("connection established to client: {client}", clientId);
            await Task.Run(() =>
            {
                while (webSocket.State == WebSocketState.Open)
                {
                }
            });
            logger.LogDebug("connection terminated to client: {client}", clientId);
        }
        catch (Exception ex)
        {
            logger.LogError("connection errored to client: {client}", clientId);
        }
        finally
        {
            clientMap.Remove((clientId, channelSubscriberId));
            subscriptionMap[channelSubscriberId].Remove(clientId);
        }
    }

    public void SendMessage(Guid channelSubscriberId, string message)
    {
        var clientIds = subscriptionMap[channelSubscriberId];
        foreach (var clientId in clientIds)
        {
            TaskProcessor.Enqueue(clientMap[(clientId, channelSubscriberId)].WriteAsync(message));
        }
    }
}
