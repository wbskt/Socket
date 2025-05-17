using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Wbskt.Common.Contracts;
using Wbskt.Common.Extensions;
using Wbskt.Common.Providers;
using Wbskt.Common.Services;
using Wbskt.Common.Utilities;

namespace Wbskt.Socket.Service.Services.Implementation;

public class WebSocketContainer(ILogger<WebSocketContainer> logger, ICachedChannelsProvider channelsProvider, ICancellationService cancellationService) : IWebSocketContainer
{
    /// <summary>
    /// Channel(sub) to client map. given a channel sub id, it will find all the client ids that subscribes to it.
    /// </summary>
    private readonly ConcurrentDictionary<int, ConcurrentKeys<int>> channelClientsMap = new();

    /// <summary>
    /// Client to Socket map each client will have only one socket connected. even if the client is subscribed to multiple channels
    /// </summary>
    private readonly ConcurrentDictionary<int, WebSocket> clientMap = new();

    public bool ConnectionExists(int clientId)
    {
        return clientMap.ContainsKey(clientId);
    }

    public void AddChannelsForClient(int[] channelIds, int clientId)
    {
        foreach (var channelId in channelIds)
        {
            channelClientsMap.GetOrAdd(channelId, new ConcurrentKeys<int>([clientId]));
        }
    }

    public async Task Listen(WebSocket webSocket, int[] channelIds, int clientId)
    {
        clientMap[clientId] = webSocket;

        foreach (var channelId in channelIds)
        {
            channelClientsMap.GetOrAdd(channelId, new ConcurrentKeys<int>([clientId]));
        }

        try
        {
            logger.LogInformation("connection established to client: {client}", clientId);
            cancellationService.InvokeOnShutdown(() => CloseClientConnection(webSocket).Wait(CancellationToken.None));
            while (!cancellationService.GetToken().IsCancellationRequested && webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReadAsync();
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
            DisposeWebSocket(webSocket, clientId);
        }
    }

    private async Task CloseClientConnection(WebSocket ws)
    {
        if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived || ws.State == WebSocketState.CloseSent)
        {
            logger.LogInformation("Closing connection ({closeStatus})", "Closing connection (server initiated)");
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection (server initiated)", CancellationToken.None);
        }
    }

    private void DisposeWebSocket(WebSocket webSocket, int clientId)
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
        foreach (var clientIds in channelClientsMap.Values)
        {
            clientIds.Remove(clientId);
        }
    }

    public void SendMessage(ClientPayload payload)
    {
        var channels = channelsProvider.GetAllByChannelPublisherId(payload.PublisherId);
        var payloads = channels.Select(c => new ClientPayload
        {
            ChannelSubscriberId = c.ChannelSubscriberId,
            ChannelId = c.ChannelId, // internal;
            Data = payload.Data,
            EnsureDelivery = payload.EnsureDelivery,
            PayloadId = payload.PayloadId,
            PublisherId = payload.PublisherId
        });

        // payload - cli[]
        var payloadClientIdsArr = payloads.Select<ClientPayload, (ClientPayload Payload, ConcurrentKeys<int> ClientIds)>(cp => (cp, channelClientsMap[cp.ChannelId])).ToArray();

        if (payloadClientIdsArr.Length == 0)
        {
            logger.LogInformation("no clients subscribed for the publisher: {publisher}", payload.PublisherId);
        }
        else
        {
            foreach (var cpcids in payloadClientIdsArr)
            {
                var jsonPayload = JsonSerializer.Serialize(cpcids.Payload);
                foreach (var clientId in cpcids.ClientIds.GetKeys())
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
}
