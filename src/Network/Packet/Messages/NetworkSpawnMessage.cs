using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Packet.Messages;

/// <summary>
/// Represents a message for spawning network objects across clients.
/// </summary>
internal sealed class NetworkSpawnMessage : IMessage<NetworkSpawnMessage, NetworkObject>
{
    /// <summary>
    /// Gets the ID of the client who owns the spawned network object.
    /// </summary>
    public ID OwnerId { get; private set; }

    /// <summary>
    /// Gets the unique network identifier assigned to the spawned object.
    /// </summary>
    public uint NetworkId { get; private set; }

    /// <summary>
    /// Gets the prefab identifier for the network object to spawn.
    /// </summary>
    public byte PrefabId { get; private set; }

    /// <summary>
    /// Serializes a NetworkClass instance into a spawn packet for network transmission.
    /// </summary>
    /// <param name="networkObj">The network object instance to serialize.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    public void Serialize(NetworkObject networkObj, PacketWriter packetWriter)
    {
        networkObj.gameObject.name = networkObj.GetObjectName();

        packetWriter.WriteID(networkObj.OwnerId);
        packetWriter.WriteUInt(networkObj.NetworkId);
        if (RuntimePrefab.Prefabs.TryGetValue(networkObj.GUID, out var prefab) && prefab is NetworkObject netprefab)
        {
            packetWriter.WriteByte(netprefab.PrefabId);
        }
        else
        {
            throw new Exception($"[NetworkSpawnPacket] Unable to find prefab by GUID: {networkObj.GUID}");
        }

        ReplantedLobby.LobbyData.OnNetworkObjectSpawn(networkObj);
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
                ReplantedLobby.LobbyData.OnNetworkObjectSpawn(child);
                child.Serialize(packetWriter, true);
                nextId++;
            }
        }
    }

    /// <summary>
    /// Deserializes a NetworkSpawnPacket from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the spawn packet data.</param>
    /// <returns>A new NetworkSpawnPacket instance with deserialized data.</returns>
    public NetworkSpawnMessage Deserialize(PacketReader packetReader)
    {
        NetworkSpawnMessage networkSpawnPacket = new()
        {
            OwnerId = packetReader.ReadID(),
            NetworkId = packetReader.ReadUInt(),
            PrefabId = packetReader.ReadByte(),
        };

        return networkSpawnPacket;
    }

    /// <summary>
    /// Deserializes a NetworkObject instance and its children from network packet data.
    /// </summary>
    public void DeserializeNetworkObject(NetworkObject networkObj, PacketReader packetReader)
    {
        ReplantedLobby.LobbyData.OnNetworkObjectSpawn(networkObj);
        networkObj.Deserialize(packetReader, true);
        networkObj.gameObject.name = networkObj.GetObjectName();

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
                ReplantedLobby.LobbyData.OnNetworkObjectSpawn(child);
                child.Deserialize(packetReader, true);
                nextId++;
            }
        }
    }
}