using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Object;

/// <summary>
/// Represents a network packet for spawning network objects across clients.
/// Contains essential information for instantiating and initializing network classes.
/// </summary>
internal class NetworkSyncPacket
{
    /// <summary>
    /// Gets a value indicating whether the initialization process.
    /// </summary>
    public bool Init { get; private set; }

    /// <summary>
    /// Gets the unique network identifier assigned to the spawned object.
    /// Used to reference this specific object across all connected clients.
    /// </summary>
    public uint NetworkId { get; private set; }

    /// <summary>
    /// Gets the bit field indicating which properties have been modified since the last reset or synchronization.
    /// </summary>
    public uint DirtyBits { get; private set; }

    /// <summary>
    /// Serializes a NetworkClass instance into a spawn packet for network transmission.
    /// Includes ownership, network ID, prefab ID, and initial object state data.
    /// </summary>
    /// <param name="networkClass">The network class instance to serialize.</param>
    /// <param name="init">If this is to init the network class.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    internal static void SerializePacket(NetworkClass networkClass, bool init, PacketWriter packetWriter)
    {
        packetWriter.WriteUInt(networkClass.NetworkId);
        packetWriter.WriteUInt(networkClass.DirtyBits);
        packetWriter.WriteBool(init);
        networkClass.Serialize(packetWriter, init);
    }

    /// <summary>
    /// Deserializes a NetworkSpawnPacket from incoming network data.
    /// Extracts ownership, network ID, and prefab information from the packet.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the spawn packet data.</param>
    /// <returns>A new NetworkSpawnPacket instance with deserialized data.</returns>
    internal static NetworkSyncPacket DeserializePacket(PacketReader packetReader)
    {
        NetworkSyncPacket networkSyncPacket = new()
        {
            NetworkId = packetReader.ReadUInt(),
            DirtyBits = packetReader.ReadUInt(),
            Init = packetReader.ReadBool()
        };

        return networkSyncPacket;
    }
}