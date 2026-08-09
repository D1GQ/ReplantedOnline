using ReplantedOnline.Network.Reloaded.Serialization;

/// <summary>
/// Represents an object that can serialize and deserialize its configuration state for network transmission.
/// </summary>
internal interface INetworkConfigSerializable
{
    /// <summary>
    /// Serializes the configuration state into a packet for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to serialize configuration data into.</param>
    void Serialize(PacketWriter packetWriter);

    /// <summary>
    /// Deserializes the configuration state from a packet received over the network.
    /// </summary>
    /// <param name="packetReader">The packet reader to deserialize configuration data from.</param>
    void Deserialize(PacketReader packetReader);
}