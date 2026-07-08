using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Serialization.Messages;

/// <summary>
/// Represents a network message for despawning network objects across clients.
/// </summary>
internal readonly struct NetworkObjectDespawnMessage : IMessage<NetworkObjectDespawnMessage, NetworkObject, bool>
{
    /// <summary>
    /// Gets the unique network identifier assigned to the spawned object.
    /// </summary>
    public NetworkIdentifier NetworkId { get; private init; }

    /// <summary>
    /// Get if the network object should wait locally to despawn.
    /// </summary>
    public bool WaitToBeReady { get; private init; }

    /// <summary>
    /// Serializes a NetworkObject instance into a despawn packet for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    /// <param name="networkObj">The network object instance to serialize.</param>
    /// <param name="waitToBeReady">Indicate whether the network object should wait until locally want to despawn on the other side .</param>
    public void Serialize(PacketWriter packetWriter, NetworkObject networkObj, bool waitToBeReady)
    {
        packetWriter.WriteNetworkId(networkObj.NetworkId);
        packetWriter.WriteBool(waitToBeReady);
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
            NetworkId = packetReader.ReadNetworkId(),
            WaitToBeReady = packetReader.ReadBool()
        };

        return message;
    }
}