using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Serialization.Messages;

/// <summary>
/// Represents a message for rejecting a network object spawn request from a client.
/// </summary>
internal readonly struct NetworkObjectRejectMessage : IMessage<NetworkObjectRejectMessage, NetworkIdentifier>
{
    /// <summary>
    /// Gets the unique network identifier of the rejected object.
    /// </summary>
    public NetworkIdentifier NetworkId { get; private init; }

    /// <summary>
    /// Serializes a NetworkIdentifier into a reject packet for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    /// <param name="networkId">The network identifier of the object being rejected.</param>
    public void Serialize(PacketWriter packetWriter, NetworkIdentifier networkId)
    {
        packetWriter.WriteNetworkId(networkId);
    }

    /// <summary>
    /// Deserializes a NetworkObjectRejectMessage from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the reject packet data.</param>
    /// <returns>A new NetworkObjectRejectMessage instance with deserialized data.</returns>
    public NetworkObjectRejectMessage Deserialize(PacketReader packetReader)
    {
        NetworkObjectRejectMessage message = new()
        {
            NetworkId = packetReader.ReadNetworkId()
        };

        return message;
    }
}