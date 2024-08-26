using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using System.Net;
using Wbskt.Server.Database;

namespace Wbskt.Server.Services.Implementation
{
    public class ServerInfoService : IServerInfoService
    {
        private readonly ILogger<ServerInfoService> logger;
        private readonly IServer server;
        private readonly IServerInfoProvider serverInfoProvider;
        
        private bool registered;
        private static int serverId;

        public ServerInfoService(ILogger<ServerInfoService> logger, IServer server, IServerInfoProvider serverInfoProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.serverInfoProvider = serverInfoProvider ?? throw new ArgumentNullException(nameof(serverInfoProvider));
        }

        public int GetCurrentServerId()
        {
            return serverId;
        }

        public void RegisterServer()
        {
            if (registered)
            {
                return;
            }
            var host = GetLocalFileServerHosts().First();

            var server = new ServerInfo
            {
                Active = true,
                Address = host,
            };

            serverId = serverInfoProvider.RegisterServer(server);
            registered = true;
        }

        private IEnumerable<HostString> GetLocalFileServerHosts()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName()).AddressList[1].MapToIPv4().ToString();
            var addresses = server.Features.Get<IServerAddressesFeature>()!.Addresses;
            if (addresses.Count == 0)
            {
                throw new InvalidOperationException();
            }

            var ports = addresses.Select(a => new Uri(a).Port).ToArray();

            var hostStrings = new HostString[ports.Length];

            for (var i = 0; i < ports.Length; i++)
            {
                hostStrings[i] = new HostString(host, ports[i]);
            }

            return hostStrings;
        }
    }
}
