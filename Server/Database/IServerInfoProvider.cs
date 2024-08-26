using Wbskt.Server.Services;

namespace Wbskt.Server.Database
{
    public interface IServerInfoProvider
    {
        int RegisterServer(ServerInfo serverInfo);
    }
}
