using System.Net.WebSockets;
using Wbskt.Common.Contracts;
using Wbskt.Common.Extensions;
using Wbskt.Common.Providers;

namespace Wbskt.Socket.Service.Services.Implementation;

public class WebSocketContainer(ILogger<WebSocketContainer> logger, IChannelsProvider channelsProvider, IClientProvider clientProvider) : IWebSocketContainer
{
    private readonly Dictionary<Guid, HashSet<int>> subscriptionMap = new();
    private readonly Dictionary<int, WebSocket> clientMap = new();

    public async Task Listen(WebSocket webSocket, Guid channelSubscriberId, int clientId, CancellationToken ct)
    {
        clientMap[clientId] = webSocket;

        if (!subscriptionMap.TryGetValue(channelSubscriberId, out var clientIds))
        {
            clientIds = new HashSet<int>();
            subscriptionMap[channelSubscriberId] = clientIds;
        }
        clientIds.Add(clientId);

        try
        {
            logger.LogInformation("connection established to client: {client}", clientId);

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReadAsync(ct).ConfigureAwait(false);
                if (result.ReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("client: {client} requested close.", clientId);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).ConfigureAwait(false);
                    break;
                }
            }

            logger.LogInformation("connection terminated to client: {client}", clientId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "connection errored to client: {client}", clientId);
        }
        finally
        {
            DisposeWebSocket(webSocket, clientId, channelSubscriberId);
        }
    }

    private void DisposeWebSocket(WebSocket webSocket, int clientId, Guid channelSubscriberId)
    {
        try
        {
            webSocket.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "error disposing WebSocket for client: {client}", clientId);
        }

        logger.LogInformation("connection to client: {client} disposed", clientId);
        clientMap.Remove(clientId);
        if (subscriptionMap.TryGetValue(channelSubscriberId, out var clientIds))
        {
            clientIds.Remove(clientId);
            if (clientIds.Count == 0)
            {
                subscriptionMap.Remove(channelSubscriberId);
            }
        }
    }

    public void SendMessage(Guid publisherId, string message)
    {
        var clientIds = channelsProvider.GetChannelByPublisherId(publisherId)
            .SelectMany(c => subscriptionMap.TryGetValue(c.ChannelSubscriberId, out var ids) ? ids : Enumerable.Empty<int>())
            .ToHashSet();

        if (clientIds.Any())
        {
            foreach (var clientId in clientIds)
            {
                if (clientMap.TryGetValue(clientId, out var webSocket))
                {
                    logger.LogDebug("enqueueing send action to processor. Client: {clientId}, Message: {message}", clientId, message);
                    TaskProcessor.Enqueue(webSocket.WriteAsync(message).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            logger.LogError(task.Exception, "failed to send message to client: {clientId}", clientId);
                        }
                        else
                        {
                            logger.LogDebug("message sent to client: {clientId}", clientId);
                        }
                    }));
                }
                else
                {
                    logger.LogWarning("client: {clientId} not found in client map.", clientId);
                }
            }
        }
        else
        {
            logger.LogInformation("no clients subscribed for the publisher: {publisher}", publisherId);
        }
    }

    public Connection[] GetActiveClients()
    {
        var channelIds = subscriptionMap.Keys;
        var channels = channelIds.Select(channelsProvider.GetChannelBySubscriberId).ToList();

        return clientProvider
            .GetClientConnectionsByIds(clientMap.Keys.ToArray())
            .Select(clientId =>
                new Connection
                {
                    ClientName = clientId.ClientName,
                    ChannelName = channels.First(c => c.ChannelSubscriberId == clientId.ChannelSubscriberId).ChannelName
                }).ToArray();
    }
}
