using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Wbskt.Common.Contracts;
using Wbskt.Common.Providers;

namespace Wbskt.Socket.Service.Services.Implementation;

public class ServerInfoService(ILogger<ServerInfoService> logger, IServer server, IServerInfoProvider serverInfoProvider) : IServerInfoService
{
    private readonly ILogger<ServerInfoService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServer server = server ?? throw new ArgumentNullException(nameof(server));
    private readonly IServerInfoProvider serverInfoProvider = serverInfoProvider ?? throw new ArgumentNullException(nameof(serverInfoProvider));
    private static bool _registered;

    private static int _serverId;

    public int GetCurrentServerId()
    {
        return _serverId;
    }

    public void RegisterServer()
    {
        while (_registered == false)
        {
            IReadOnlyCollection<ServerInfo> servers;
            try
            {
                servers = serverInfoProvider.GetAll();
            }
            catch (Exception ex)
            {
                logger.LogError("error while trying to register server: {error}", ex.Message);
                logger.LogTrace("error while trying to register server: {error}", ex.ToString());
                continue;
            }

            var host = GetCurrentHostAddresses().First();

            var serverInfo = new ServerInfo
            {
                Address = host,
            };

            logger.LogDebug("current socket-server address is {address}", serverInfo.Address);
            if (servers.Any(s => s.Address == host))
            {
                serverInfo = servers.First(s => s.Address == host);
                _serverId = serverInfo.ServerId;
                // active status will be updated by the core.server
                logger.LogInformation("this server({serverId}) is already registered", _serverId);
                serverInfoProvider.UpdateServerStatus(_serverId, false);
                _registered = true;
            }
            else
            {
                logger.LogInformation("registering current server");
                _serverId = serverInfoProvider.RegisterServer(serverInfo);
                _registered = true;
                logger.LogInformation("registered current server with id {serverId}", _serverId);
            }
        }
    }

    private HostString[] GetCurrentHostAddresses()
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
