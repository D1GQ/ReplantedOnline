using ReplantedOnline.Network.Object;

namespace ReplantedOnline.Network.Packet;

/// <summary>
/// Represents a network packet for despawning network objects across clients.
/// </summary>
internal sealed class NetworkDespawnPacket
{
    /// <summary>
    /// Gets the unique network identifier assigned to the spawned object.
    /// </summary>
    public uint NetworkId { get; private set; }

    /// <summary>
    /// Serializes a NetworkClass instance into a despawn packet for network transmission.
    /// </summary>
    /// <param name="networkObj">The network object instance to serialize.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    internal static void SerializePacket(NetworkObject networkObj, PacketWriter packetWriter)
    {
        packetWriter.WriteUInt(networkObj.NetworkId);
    }

    /// <summary>
    /// Deserializes a NetworkDespawnPacket from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the spawn packet data.</param>
    /// <returns>A new NetworkSpawnPacket instance with deserialized data.</returns>
    internal static NetworkDespawnPacket DeserializePacket(PacketReader packetReader)
    {
        NetworkDespawnPacket networkSpawnPacket = new()
        {
            NetworkId = packetReader.ReadUInt(),
        };

        return networkSpawnPacket;
    }
}