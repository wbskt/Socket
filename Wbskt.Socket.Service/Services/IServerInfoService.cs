namespace Wbskt.Socket.Service.Services;

public interface IServerInfoService
{
    Task RegisterServer();

    int GetCurrentServerId();
}
