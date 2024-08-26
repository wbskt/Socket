using Wbskt.Server.Services;

namespace Wbskt.Server.Database
{
    public interface IClientProvider
    {
        ClientConenction GetClientConenctionById(int clientId);

        void InvalidateToken(int clientId);
    }
}
