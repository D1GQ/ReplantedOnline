using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object;

namespace ReplantedOnline.Network.Packet.Messages;

/// <summary>
/// Represents a network message for despawning network objects across clients.
/// </summary>
internal sealed class NetworkDespawnMessage : IMessage<NetworkDespawnMessage, NetworkObject>
{
    /// <summary>
    /// Gets the unique network identifier assigned to the spawned object.
    /// </summary>
    public uint NetworkId { get; private set; }

    /// <summary>
    /// Serializes a NetworkObject instance into a despawn packet for network transmission.
    /// </summary>
    /// <param name="networkObj">The network object instance to serialize.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    public void Serialize(NetworkObject networkObj, PacketWriter packetWriter)
    {
        packetWriter.WriteUInt(networkObj.NetworkId);
    }

    /// <summary>
    /// Deserializes a NetworkDespawnPacket from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the spawn packet data.</param>
    /// <returns>A new NetworkSpawnPacket instance with deserialized data.</returns>
    public NetworkDespawnMessage Deserialize(PacketReader packetReader)
    {
        NetworkDespawnMessage networkSpawnPacket = new()
        {
            NetworkId = packetReader.ReadUInt(),
        };

        return networkSpawnPacket;
    }
}