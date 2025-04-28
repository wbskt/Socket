using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Wbskt.Common.Contracts;
using Wbskt.Common.Providers;

namespace Wbskt.Socket.Service.Services.Implementation;

public class ServerInfoService(ILogger<ServerInfoService> logger, IServer server, IServerInfoProvider serverInfoProvider, IHostEnvironment environment) : IServerInfoService
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

    public async Task RegisterServer()
    {
        while (_registered == false)
        {
            IReadOnlyCollection<ServerInfo> servers;
            HostString host;
            try
            {
                servers = serverInfoProvider.GetAll();
                host = (await GetCurrentHostAddresses()).First();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "error while trying to register server: {error}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(10));
                continue;
            }


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

    private async Task<HostString[]> GetCurrentHostAddresses()
    {
        var host = await GetIpAddress();
        var addresses = server.Features.Get<IServerAddressesFeature>()!.Addresses;
        if (addresses.Count == 0)
        {
            throw new InvalidOperationException("server address feature is not initialized yet");
        }

        var ports = addresses.Select(a => new Uri(a).Port).ToArray();

        var hostStrings = new HostString[ports.Length];

        for (var i = 0; i < ports.Length; i++)
        {
            hostStrings[i] = new HostString(host, ports[i]);
        }

        return hostStrings;
    }

    private async Task<string> GetIpAddress()
    {
        if (environment.IsProduction())
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("https://ifconfig.me"),
            };

            var result = await client.GetAsync("ip");
            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadAsStringAsync();
            }

            return string.Empty;
        }

        var hostName = Dns.GetHostName();
        var hostEntry = await Dns.GetHostEntryAsync(hostName);
        logger.LogInformation("the domain name of this server is: {domainName}", hostEntry.HostName);
        var host = hostEntry.AddressList[1].MapToIPv4().ToString();
        return host;
    }
}
