using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Packet.Messages;

/// <summary>
/// Represents a network message for despawning network objects across clients.
/// </summary>
internal readonly struct NetworkObjectDespawnMessage : IMessage<NetworkObjectDespawnMessage, INetworkObject>
{
    /// <summary>
    /// Gets the unique network identifier assigned to the spawned object.
    /// </summary>
    public uint NetworkId { get; private init; }

    /// <summary>
    /// Serializes a NetworkObject instance into a despawn packet for network transmission.
    /// </summary>
    /// <param name="networkObj">The network object instance to serialize.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    public void Serialize(INetworkObject networkObj, PacketWriter packetWriter)
    {
        packetWriter.WriteUInt(networkObj.NetworkId);
    }

    /// <summary>
    /// Deserializes a NetworkDespawnPacket from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the spawn packet data.</param>
    /// <returns>A new NetworkSpawnPacket instance with deserialized data.</returns>
    public NetworkObjectDespawnMessage Deserialize(PacketReader packetReader)
    {
        NetworkObjectDespawnMessage message = new()
        {
            NetworkId = packetReader.ReadUInt(),
        };

        return message;
    }
}