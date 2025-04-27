namespace Wbskt.Socket.Service.Services;

public interface IClientService
{
    bool VerifyAndInvalidateToken(int clientId, Guid tokenId);
    int GetClientIdByUniqueId(Guid clientUniqueId);
}
