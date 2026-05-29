using ReplantedOnline.Network.Routing.Packet;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Represents an object that can be serialized and deserialized for network transmission.
/// </summary>
internal interface INetworkSerializable
{
    /// <summary>
    /// Serializes the network state into a packet for network transmission.
    /// Handles both initial state serialization and incremental updates.
    /// </summary>
    /// <param name="packetWriter">The packet writer to serialize data into.</param>
    /// <param name="init">Whether this is initial serialization (true) or update serialization (false).</param>
    void Serialize(PacketWriter packetWriter, bool init);

    /// <summary>
    /// Deserializes the network state from a packet received over the network.
    /// Handles both initial state deserialization and incremental updates.
    /// </summary>
    /// <param name="packetReader">The packet reader to deserialize data from.</param>
    /// <param name="init">Whether this is initial deserialization (true) or update deserialization (false).</param>
    void Deserialize(PacketReader packetReader, bool init);
}
