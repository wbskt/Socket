using Wbskt.Common.Providers;

namespace Wbskt.Socket.Service.Services.Implementation;

public class ClientService(ILogger<ClientService> logger, IClientProvider clientProvider) : IClientService
{
    private readonly ILogger<ClientService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IClientProvider clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));

    public bool VerifyAndInvalidateToken(int clientId, Guid tokenId)
    {
        var client = clientProvider.GetClientConnectionById(clientId);
        if (client.TokenId != tokenId)
        {
            logger.LogWarning("this token: {tokenId} is already used once", tokenId);
            return false;
        }

        logger.LogDebug("invalidating token: {tokenId} of client: {clientId}", tokenId, clientId);
        clientProvider.InvalidateToken(clientId);
        return true;
    }

    public int GetClientIdByUniqueId(Guid clientUniqueId)
    {
        return clientProvider.FindClientIdByClientUniqueId(clientUniqueId);
    }
}
