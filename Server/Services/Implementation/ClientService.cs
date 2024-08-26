
using Wbskt.Server.Database;

namespace Wbskt.Server.Services.Implementation
{
    public class ClientService : IClientService
    {
        private readonly ILogger<ClientService> logger;
        private readonly IClientProvider clientProvider;

        public ClientService(ILogger<ClientService> logger, IClientProvider clientProvider)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));
        }

        public bool VerifyAndInvalidateToken(int clientId, Guid tokenId)
        {
            var client = clientProvider.GetClientConenctionById(clientId);
            if (client.TokenId == tokenId)
            {
                clientProvider.InvalidateToken(clientId);
                return true;
            }

            return false;
        }
    }
}
