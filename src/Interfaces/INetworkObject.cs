using Il2CppSteamworks;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Interfaces;

/// <summary>
/// Defines the contract for network-synchronized classes in ReplantedOnline.
/// Provides methods and properties for serialization, deserialization, and network state management.
/// </summary>
internal interface INetworkObject
{
    /// <summary>
    /// Gets the synchronized bits that track which properties have been changed and need network updates.
    /// Used for delta compression to only send changed data.
    /// </summary>
    SyncedBits SyncedBits { get; }

    /// <summary>
    /// Gets the dirty bits flag indicating which properties have been modified since last sync.
    /// Helps determine what data needs to be sent over the network.
    /// </summary>
    uint DirtyBits { get; }

    /// <summary>
    /// Gets the unique network identifier for this network object instance.
    /// Used to reference this specific object across all connected clients.
    /// </summary>
    uint NetworkId { get; }

    /// <summary>
    /// Gets the Steam ID of the client who owns and controls this network object.
    /// Determines which client has authority over this object's state.
    /// </summary>
    SteamId OwnerId { get; }

    /// <summary>
    /// Serializes the network object state into a packet for network transmission.
    /// Handles both initial state serialization and incremental updates.
    /// </summary>
    /// <param name="packetWriter">The packet writer to serialize data into.</param>
    /// <param name="init">Whether this is initial serialization (true) or update serialization (false).</param>
    public void Serialize(PacketWriter packetWriter, bool init);

    /// <summary>
    /// Deserializes the network object state from a packet received over the network.
    /// Handles both initial state deserialization and incremental updates.
    /// </summary>
    /// <param name="packetReader">The packet reader to deserialize data from.</param>
    /// <param name="init">Whether this is initial deserialization (true) or update deserialization (false).</param>
    public void Deserialize(PacketReader packetReader, bool init);
}