namespace Wbskt.Socket.Service.Services;

public interface IServerInfoService
{
    void RegisterServer();

    int GetCurrentServerId();
}
