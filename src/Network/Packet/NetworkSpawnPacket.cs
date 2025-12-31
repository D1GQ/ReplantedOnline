using Il2CppSteamworks;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Network.Packet;

/// <summary>
/// Represents a network packet for spawning network objects across clients.
/// Contains essential information for instantiating and initializing network objects.
/// </summary>
internal sealed class NetworkSpawnPacket
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
    /// <param name="networkObj">The network object instance to serialize.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    internal static void SerializePacket(NetworkObject networkObj, PacketWriter packetWriter)
    {
        packetWriter.WriteULong(networkObj.OwnerId);
        packetWriter.WriteUInt(networkObj.NetworkId);
        if (RuntimePrefab.Prefabs.TryGetValue(networkObj.GUID, out var prefab) && prefab is NetworkObject netprefab)
        {
            packetWriter.WriteByte(netprefab.PrefabId);
        }
        else
        {
            throw new Exception($"[NetworkSpawnPacket] Unable to find prefab by GUID: {networkObj.GUID}");
        }
        networkObj.Serialize(packetWriter, true);

        var count = Math.Min(networkObj.ChildNetworkObjects.Count, ReplantedOnlineMod.Constants.MAX_NETWORK_CHILDREN - 1);
        packetWriter.WriteInt(count);
        if (count > 0)
        {
            uint nextId = 1;
            for (int i = 0; i < count; i++)
            {
                var child = networkObj.ChildNetworkObjects[i];
                child.OwnerId = networkObj.OwnerId;
                child.NetworkId = networkObj.NetworkId + nextId;
                child.Serialize(packetWriter, true);
                NetLobby.LobbyData.OnNetworkObjectSpawn(child);
                nextId++;
            }
        }
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

    /// <summary>
    /// Deserializes a NetworkClass instance and its children from network packet data.
    /// Reconstructs the object state and hierarchy based on serialized information.
    /// </summary>
    internal static void DeserializeNetworkObject(NetworkObject networkObj, PacketReader packetReader)
    {
        networkObj.Deserialize(packetReader, true);
        int childCount = packetReader.ReadInt();
        if (childCount > 0)
        {
            uint nextId = 1;
            for (int i = 0; i < childCount; i++)
            {
                if (i >= ReplantedOnlineMod.Constants.MAX_NETWORK_CHILDREN) break;

                var child = networkObj.ChildNetworkObjects[i];
                child.OwnerId = networkObj.OwnerId;
                child.NetworkId = networkObj.NetworkId + nextId;
                child.Deserialize(packetReader, true);
                NetLobby.LobbyData.OnNetworkObjectSpawn(child);
                nextId++;
            }
        }
    }
}