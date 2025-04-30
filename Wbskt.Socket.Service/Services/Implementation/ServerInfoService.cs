using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using Wbskt.Common.Contracts;
using Wbskt.Common.Providers;

namespace Wbskt.Socket.Service.Services.Implementation;

public class ServerInfoService(ILogger<ServerInfoService> logger, IOptionsMonitor<SocketServerConfiguration> optionsMonitor, IServer server, IServerInfoProvider serverInfoProvider, IHostEnvironment environment) : IServerInfoService
{
    private static bool _registered;
    private static ServerInfo _currentServerInfo = new();
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

            _currentServerInfo.Address = host;

            logger.LogDebug("current socket-server address is {address}", _currentServerInfo.Address);
            if (servers.Any(s => s.Address == host))
            {
                //replace serverInfo with the one from the db
                _currentServerInfo = servers.First(s => s.Address == host);
                _serverId = _currentServerInfo.ServerId;
                // active status will be updated by the core.server
                CheckAndUpdatePublicDomainAddressFromDb(_currentServerInfo);
                logger.LogInformation("this server({serverId}) is already registered", _serverId);
                serverInfoProvider.UpdateServerStatus(_serverId, false);
                CheckAndUpdatePublicDomainAddressFromAppSettings(optionsMonitor.CurrentValue);
                _registered = true;
            }
            else
            {
                logger.LogInformation("registering current server");
                _serverId = serverInfoProvider.RegisterServer(_currentServerInfo);
                CheckAndUpdatePublicDomainAddressFromAppSettings(optionsMonitor.CurrentValue);
                _currentServerInfo.ServerId = _serverId;
                _registered = true;
                logger.LogInformation("registered current server with id {serverId}", _serverId);
            }
        }
        optionsMonitor.OnChange(CheckAndUpdatePublicDomainAddressFromAppSettings);
    }

    private void CheckAndUpdatePublicDomainAddressFromAppSettings(SocketServerConfiguration configuration)
    {
        var host = _currentServerInfo.Address.Host;
        string publicHost;
        try
        {
            publicHost = Dns.GetHostAddresses(configuration.PublicDomainName).First().ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "could not resolve the address: {publicAddr}", configuration.PublicDomainName);
            return;
        }

        logger.LogInformation("system ip address is: {host}", host);
        logger.LogInformation("configured domain name is: {domain}", configuration.PublicDomainName);
        logger.LogInformation("{domain} point to: {ip}", configuration.PublicDomainName, publicHost);
        if (string.Equals(host, publicHost, StringComparison.InvariantCultureIgnoreCase))
        {
            logger.LogWarning("updating db with the new configured domain address: {addr}", configuration.PublicDomainName);
            serverInfoProvider.UpdatePublicDomainName(_currentServerInfo.ServerId, configuration.PublicDomainName);
            _currentServerInfo.PublicDomainName = configuration.PublicDomainName;
        }
        else
        {
            logger.LogWarning("mismatch between domain name and current ipaddress (skipping db update)");
        }
    }

    private void CheckAndUpdatePublicDomainAddressFromDb(ServerInfo serverInfo)
    {
        var host = serverInfo.Address.Host;
        var publicHost = Dns.GetHostAddresses(serverInfo.PublicDomainName).First().ToString();
        logger.LogInformation("system ip address is: {host}", host);
        logger.LogInformation("configured domain name is: {domain}", serverInfo.PublicDomainName);
        logger.LogInformation("{domain} point to: {ip}", serverInfo.PublicDomainName , publicHost);
        if (string.Equals(host, publicHost, StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        logger.LogWarning("mismatch between domain name and current ipaddress (updating dbo.server)");
        serverInfoProvider.UpdatePublicDomainName(serverInfo.ServerId, string.Empty);
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
        var host = hostEntry.AddressList[1].MapToIPv4().ToString();
        return host;
    }
}
