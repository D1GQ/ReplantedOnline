using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client.Object.Component;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Serialization.Messages;

/// <summary>
/// Represents a network message for invoking object messages on <see cref="INetworkIdentifier"/> targets.
/// </summary>
internal readonly struct ObjectMsgMessage : IMessage<ObjectMsgMessage, INetworkIdentifier, uint, IPacket?>
{
    /// <summary>
    /// Gets the unique network identifier of the target network object.
    /// </summary>
    public NetworkIdentifier NetworkId { get; private init; }

    /// <summary>
    /// Gets the identifier of the message method to invoke on the target object.
    /// </summary>
    public uint MessageId { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the target is a NetworkComponent rather than a direct NetworkObject.
    /// </summary>
    public bool IsComponent { get; private init; }

    /// <summary>
    /// Gets the component index within the parent NetworkObject's component collection.
    /// Returns -1 if this is not a component-based message.
    /// </summary>
    public int ComponentIndex { get; private init; }

    /// <summary>
    /// Serializes a network object message invocation into a packet for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    /// <param name="networkIdentifier">The target INetworkIdentifier instance to serialize.</param>
    /// <param name="messageId">The identifier of the message method to invoke.</param>
    /// <param name="payload">The packet payload.</param>
    public void Serialize(PacketWriter packetWriter, INetworkIdentifier networkIdentifier, uint messageId, IPacket? payload)
    {
        packetWriter.WriteNetworkId(networkIdentifier.NetworkId);
        packetWriter.WritePackedUInt(messageId);
        if (networkIdentifier is NetworkComponent component)
        {
            packetWriter.WriteBool(true);
            packetWriter.WritePackedInt(component.Index);
        }
        else
        {
            packetWriter.WriteBool(false);
        }

        if (payload != null)
        {
            packetWriter.AddBytesToBuffer(payload.GetBytes());
        }
    }

    /// <summary>
    /// Deserializes a message invocation from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the message data.</param>
    /// <returns>A new ObjectMsgMessage instance with deserialized data.</returns>
    public ObjectMsgMessage Deserialize(PacketReader packetReader)
    {
        NetworkIdentifier networkId = packetReader.ReadNetworkId();
        uint messageId = packetReader.ReadPackedUInt();
        bool isComponent = packetReader.ReadBool();

        int componentIndex;
        if (isComponent)
            componentIndex = packetReader.ReadPackedInt();
        else
            componentIndex = -1;

        ObjectMsgMessage message = new()
        {
            NetworkId = networkId,
            MessageId = messageId,
            IsComponent = isComponent,
            ComponentIndex = componentIndex
        };

        return message;
    }
}