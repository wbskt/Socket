using System.Net.WebSockets;
using Wbskt.Common;
using Wbskt.Common.Extensions;
using Wbskt.Common.Providers;

namespace Wbskt.Server.Services.Implementation;

public class WebSocketContainer(ILogger<WebSocketContainer> logger, IChannelsProvider channelsProvider) : IWebSocketContainer
{
    private readonly ILogger<WebSocketContainer> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Dictionary<Guid, HashSet<int>> subscriptionMap = new();
    private readonly Dictionary<int, WebSocket> clientMap = new();

    public async Task Listen(WebSocket webSocket, Guid channelSubscriberId, int clientId)
    {
        clientMap.Add(clientId, webSocket);

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
            logger.LogInformation("connection established to client: {client}", clientId);
            await Task.Run(() =>
            {
                while (webSocket.State == WebSocketState.Open)
                {
                }
            });
            logger.LogInformation("connection terminated to client: {client}", clientId);
        }
        catch (Exception ex)
        {
            logger.LogError("connection errored to client: {client}, error: {error}", clientId, ex.Message);
            logger.LogTrace("connection errored to client: {client}, error: {error}", clientId, ex.ToString());
        }
        finally
        {
            clientMap.Remove(clientId);
            subscriptionMap[channelSubscriberId].Remove(clientId);
        }
    }

    public void SendMessage(Guid publisherId, string message)
    {
        var subscriberIds = channelsProvider.GetChannelPublisherId(publisherId).Select(c => c.ChannelSubscriberId).ToList();
        var clientIds = new List<int>();
        foreach (var subscriberId in subscriberIds)
        {
            if (subscriptionMap.TryGetValue(subscriberId, out var ids))
            {
                clientIds.AddRange(ids);
            }
            else
            {
                logger.LogInformation("no clients subscribed for the publisher: {publisher}", publisherId);
            }
        }

        var clientIdSet = clientIds.ToHashSet();
        foreach (var clientId in clientIdSet)
        {
            logger.LogDebug("enqueueing send action to processor. client: {clientId}, message: {message}", clientId, message);
            TaskProcessor.Enqueue(clientMap[clientId].WriteAsync(message).ContinueWith(_ =>
            {
                logger.LogDebug("message send to client: {clientId}", clientId);
            }));
        }
    }
}
