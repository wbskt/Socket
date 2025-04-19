using Wbskt.Common.Providers;

namespace Wbskt.Server.Services.Implementation;

public class ClientService(ILogger<ClientService> logger, IClientProvider clientProvider) : IClientService
{
    private readonly ILogger<ClientService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IClientProvider clientProvider = clientProvider ?? throw new ArgumentNullException(nameof(clientProvider));

    public bool VerifyAndInvalidateToken(int clientId, Guid tokenId)
    {
        var client = clientProvider.GetClientConnectionById(clientId);
        if (client.TokenId != tokenId)
        {
            return false;
        }

        clientProvider.InvalidateToken(clientId);
        return true;

    }
}
