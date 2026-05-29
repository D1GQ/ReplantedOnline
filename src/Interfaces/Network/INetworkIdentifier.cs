using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Represents an object that can be identified on the network.
/// </summary>
internal interface INetworkIdentifier
{
    /// <summary>
    /// Gets the unique identifier of the client that owns this object.
    /// </summary>
    ID OwnerId { get; }

    /// <summary>
    /// Gets the unique network identifier for this object within the network.
    /// </summary>
    NetworkIdentifier NetworkId { get; }
}