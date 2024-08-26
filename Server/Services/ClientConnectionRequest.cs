namespace Wbskt.Server.Services
{
    public class ClientConnectionRequest
    {
        public Guid ChannelSubscriberId { get; set; }

        public required string ClientName { get; set; }

        public required string ClientUniqueId { get; set;}
    }

    public class ClientConenction : ClientConnectionRequest
    {
        public int ClientId { get; set; } 

        public Guid TokenId { get; set; }

        public bool Disabled { get; set; }
    }
}
