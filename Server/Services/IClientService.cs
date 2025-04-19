namespace Wbskt.Server.Services;

public interface IClientService
{
    bool VerifyAndInvalidateToken(int clientId, Guid tokenId);
}