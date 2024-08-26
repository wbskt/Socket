using System.Net.WebSockets;

namespace Wbskt.Server.Services.Implementation
{
    public class WebSocketContainer : IWebSocketContainer
    {
        private readonly ILogger<WebSocketContainer> logger;
        private readonly IClientService clientService;
        private readonly IDictionary<Guid, IList<int>> subscriptionMap;
        private readonly IDictionary<(int ClientId, Guid SubacriptionId), WebSocket> clientMap;

        public WebSocketContainer(ILogger<WebSocketContainer> logger, IClientService clientService)
        {
            subscriptionMap = new Dictionary<Guid, IList<int>>(); // init load all subscriptions assigned to this socket server
            clientMap = new Dictionary<(int, Guid), WebSocket>();
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clientService = clientService ?? throw new ArgumentNullException(nameof(clientService));
        }

        public async Task Listen(WebSocket webSocket, Guid channelSubscriberId, int clientId)
        {
            // todo: validate channel id and it's assigned to this socketserver.
            clientMap.Add((clientId, channelSubscriberId), webSocket);
            subscriptionMap[channelSubscriberId].Add(clientId);

            await Task.Run(() => { while (webSocket.State != WebSocketState.Open) ; });

            clientMap.Remove((clientId, channelSubscriberId));
            subscriptionMap[channelSubscriberId].Remove(clientId);
        }

        public void SendMessage(Guid channelSubscriberId, string message)
        {   
            var clientIds = subscriptionMap[channelSubscriberId];
            foreach (var clientId in clientIds)
            {
                TaskExcecuter.Enqueue(clientMap[(clientId, channelSubscriberId)].WriteAsync(message));
            }
        }
    }
}
