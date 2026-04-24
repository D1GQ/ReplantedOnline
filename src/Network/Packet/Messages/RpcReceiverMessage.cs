using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Component;

namespace ReplantedOnline.Network.Packet.Messages;

/// <summary>
/// Represents a network message for invoking Remote Procedure Calls (RPCs) on network objects.
/// Handles both direct NetworkObject RPCs and Component-based RPCs.
/// </summary>
internal readonly struct RpcReceiverMessage : IMessage<RpcReceiverMessage, IRpcReceiver, byte>
{
    /// <summary>
    /// Gets the unique network identifier of the target network object.
    /// </summary>
    public uint NetworkId { get; private init; }

    /// <summary>
    /// Gets the identifier of the RPC method to invoke on the target object.
    /// </summary>
    public byte RpcId { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the target is a NetworkComponent rather than a direct NetworkObject.
    /// </summary>
    public bool IsComponent { get; private init; }

    /// <summary>
    /// Gets the component index within the parent NetworkObject's component collection.
    /// Returns -1 if this is not a component-based RPC.
    /// </summary>
    public int ComponentIndex { get; private init; }

    /// <summary>
    /// Serializes a network object RPC invocation into a packet for network transmission.
    /// Handles both direct IRpcReceiver objects and NetworkComponent types.
    /// </summary>
    /// <param name="rpcReceiver">The target IRpcReceiver instance to serialize.</param>
    /// <param name="rpcId">The identifier of the RPC method to invoke.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    public void Serialize(IRpcReceiver rpcReceiver, byte rpcId, PacketWriter packetWriter)
    {
        packetWriter.WriteUInt(rpcReceiver.NetworkId);
        packetWriter.WriteByte(rpcId);
        if (rpcReceiver is NetworkComponent component)
        {
            packetWriter.WriteBool(true);
            packetWriter.WriteInt(component.Index);
        }
        else
        {
            packetWriter.WriteBool(false);
        }
    }

    /// <summary>
    /// Deserializes an RPC invocation message from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the RPC message data.</param>
    /// <returns>A new NetworkObjectRpcMessage instance with deserialized data.</returns>
    public RpcReceiverMessage Deserialize(PacketReader packetReader)
    {
        uint networkId = packetReader.ReadUInt(); ;
        byte rpcId = packetReader.ReadByte();
        bool isComponent = packetReader.ReadBool();

        int componentIndex;
        if (isComponent)
            componentIndex = packetReader.ReadInt();
        else
            componentIndex = -1;

        RpcReceiverMessage message = new()
        {
            NetworkId = networkId,
            RpcId = rpcId,
            IsComponent = isComponent,
            ComponentIndex = componentIndex
        };

        return message;
    }
}