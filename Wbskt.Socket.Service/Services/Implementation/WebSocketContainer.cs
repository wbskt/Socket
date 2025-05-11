using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Wbskt.Common.Contracts;
using Wbskt.Common.Extensions;
using Wbskt.Common.Providers;

namespace Wbskt.Socket.Service.Services.Implementation;

public class WebSocketContainer(ILogger<WebSocketContainer> logger, IChannelsProvider channelsProvider, IClientProvider clientProvider) : IWebSocketContainer
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, byte>> subscriptionMap = new();
    private readonly ConcurrentDictionary<int, WebSocket> clientMap = new();

    public async Task Listen(WebSocket webSocket, Guid channelSubscriberId, int clientId, CancellationToken ct)
    {
        clientMap[clientId] = webSocket;

        var clientIds = subscriptionMap.GetOrAdd(channelSubscriberId, _ => new ConcurrentDictionary<int, byte>());
        clientIds.TryAdd(clientId, 0);

        try
        {
            logger.LogInformation("connection established to client: {client}", clientId);

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReadAsync(ct);
                if (result.ReceiveResult.MessageType == WebSocketMessageType.Close && webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
                {
                    logger.LogInformation("client: {client} requested close.", clientId);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }
            }

            logger.LogInformation("connection terminated to client: {client}", clientId);
        }
        catch (Exception ex)
        {
            logger.LogError("connection errored to client: {client} with message: {error}", clientId, ex.Message);
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
        clientMap.Remove(clientId, out _);
        if (subscriptionMap.TryGetValue(channelSubscriberId, out var clientIds))
        {
            clientIds.TryRemove(clientId, out _);
            if (clientIds.IsEmpty)
            {
                subscriptionMap.TryRemove(channelSubscriberId, out _);
            }
        }
    }

    public void SendMessage(ClientPayload payload)
    {
        var channels = channelsProvider.GetChannelByPublisherId(payload.PublisherId);
        var payloads = channels.Select(c => new ClientPayload
        {
            ChannelSubscriberId = c.ChannelSubscriberId,
            Data = payload.Data,
            EnsureDelivery = payload.EnsureDelivery,
            PayloadId = payload.PayloadId,
            PublisherId = payload.PublisherId
        });

        // payload - cli[]
        var payloadClientIdsArr = payloads.Select<ClientPayload, (ClientPayload Payload, ConcurrentDictionary<int, byte> ClientIds)>(cp => (cp, subscriptionMap[cp.ChannelSubscriberId])).ToArray();

        if (payloadClientIdsArr.Length == 0)
        {
            logger.LogInformation("no clients subscribed for the publisher: {publisher}", payload.PublisherId);
        }
        else
        {
            foreach (var cpcids in payloadClientIdsArr)
            {
                var jsonPayload = JsonSerializer.Serialize(cpcids.Payload);
                foreach (var clientId in cpcids.ClientIds.Keys)
                {
                    if (clientMap.TryGetValue(clientId, out var webSocket))
                    {
                        EnqueueTask(jsonPayload, clientId, webSocket);
                    }
                    else
                    {
                        logger.LogWarning("client: {clientId} not found in client map.", clientId);
                    }
                }
            }
        }
    }

    private void EnqueueTask(string jsonPayload, int clientId, WebSocket webSocket)
    {
        logger.LogDebug("enqueueing send action to processor. Client: {clientId}, Message: {message}", clientId, jsonPayload);
        TaskProcessor.Enqueue(webSocket.WriteAsync(jsonPayload).ContinueWith(task =>
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
