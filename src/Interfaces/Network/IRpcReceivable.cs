using ReplantedOnline.Structs;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Represents an object that can receive and handle Remote Procedure Calls (RPCs) over the network.
/// Implemented by NetworkObject and NetworkComponent to enable RPC communication.
/// </summary>
internal interface IRpcReceiver
{
    /// <summary>
    /// Gets the unique identifier of the client that owns this object.
    /// </summary>
    ID OwnerId { get; }

    /// <summary>
    /// Gets the unique network identifier for this object within the network.
    /// </summary>
    uint NetworkId { get; }
}