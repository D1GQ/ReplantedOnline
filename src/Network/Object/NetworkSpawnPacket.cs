using Il2CppSteamworks;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Object;

/// <summary>
/// Represents a network packet for spawning network objects across clients.
/// Contains essential information for instantiating and initializing network classes.
/// </summary>
internal class NetworkSpawnPacket
{
    /// <summary>
    /// Gets the Steam ID of the client who owns the spawned network object.
    /// Determines which client has authority over the object's behavior.
    /// </summary>
    public SteamId OwnerId { get; private set; }

    /// <summary>
    /// Gets the unique network identifier assigned to the spawned object.
    /// Used to reference this specific object across all connected clients.
    /// </summary>
    public uint NetworkId { get; private set; }

    /// <summary>
    /// Gets the prefab identifier for the network object to spawn.
    /// References a registered prefab in NetworkClass.NetworkPrefabs dictionary.
    /// </summary>
    public byte PrefabId { get; private set; }

    /// <summary>
    /// Serializes a NetworkClass instance into a spawn packet for network transmission.
    /// Includes ownership, network ID, prefab ID, and initial object state data.
    /// </summary>
    /// <param name="networkClass">The network class instance to serialize.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    internal static void SerializePacket(NetworkClass networkClass, PacketWriter packetWriter)
    {
        packetWriter.WriteULong(networkClass.OwnerId);
        packetWriter.WriteUInt(networkClass.NetworkId);
        if (NetworkClass.PrefabIdTypeLookup.TryGetValue(networkClass.GetType(), out var prefabId))
        {
            packetWriter.WriteByte(prefabId);
        }
        else
        {
            packetWriter.WriteByte(NetworkClass.NO_PREFAB_ID);
        }
        networkClass.Serialize(packetWriter, true);
    }

    /// <summary>
    /// Deserializes a NetworkSpawnPacket from incoming network data.
    /// Extracts ownership, network ID, and prefab information from the packet.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the spawn packet data.</param>
    /// <returns>A new NetworkSpawnPacket instance with deserialized data.</returns>
    internal static NetworkSpawnPacket DeserializePacket(PacketReader packetReader)
    {
        NetworkSpawnPacket networkSpawnPacket = new()
        {
            OwnerId = packetReader.ReadULong(),
            NetworkId = packetReader.ReadUInt(),
            PrefabId = packetReader.ReadByte(),
        };

        return networkSpawnPacket;
    }
}