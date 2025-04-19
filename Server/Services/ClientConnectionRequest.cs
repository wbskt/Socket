namespace Wbskt.Server.Services;

public class ClientConnectionRequest
{
    /// <summary>
    /// Used for a client to connect to a channel
    /// </summary>
    public Guid ChannelSubscriberId { get; set; }

    /// <summary>
    /// Human-readable name for the client
    /// </summary>
    public required string ClientName { get; set; }

    /// <summary>
    /// This id is unique for a particular client. This is an identifier for this specific client throughout the platform
    /// </summary>
    public required Guid ClientUniqueId { get; set;}

    /// <summary>
    /// This is a secret string provided by the user while creation of a channel
    /// </summary>
    public required string ChannelSecret { get; set; }
}

public class ClientConnection : ClientConnectionRequest
{
    /// <summary>
    /// Internal ID
    /// </summary>
    public int ClientId { get; set; }

    /// <summary>
    /// ID of the JWT token given to a client. Used mainly for blacklisting tokens
    /// </summary>
    public Guid TokenId { get; set; }

    /// <summary>
    /// TODO
    /// </summary>
    public bool Disabled { get; set; }
}
