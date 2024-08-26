namespace Wbskt.Server.Services
{
    public interface IServerInfoService
    {
        void RegisterServer();

        int GetCurrentServerId();
    }
}
