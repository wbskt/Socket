namespace Wbskt.Socket.Service.Services;

public interface IServerInfoService
{
    Task RegisterServer(CancellationToken ct);

    int GetCurrentServerId();
}
