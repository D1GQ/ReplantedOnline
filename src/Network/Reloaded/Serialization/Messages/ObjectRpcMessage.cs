using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Modded;
using static ReplantedOnline.Network.Reloaded.Client.Routing.Packet.ObjectRpcPacket;

namespace ReplantedOnline.Network.Reloaded.Serialization.Messages;

/// <summary>
/// Represents a network RPC for invoking object RPCs on <see cref="INetworkIdentifier"/> targets.
/// </summary>
internal readonly struct ObjectRpcMessage : IMessage<ObjectRpcMessage, CatchedNetworkIdentifier, uint, IPacket?>
{
    /// <summary>
    /// Gets the unique network identifier of the target network object.
    /// </summary>
    public NetworkIdentifier NetworkId { get; private init; }

    /// <summary>
    /// Gets the identifier of the RPC method to invoke on the target object.
    /// </summary>
    public uint RpcId { get; private init; }

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
    /// </summary>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    /// <param name="networkIdentifier">The target INetworkIdentifier instance to serialize.</param>
    /// <param name="rpcId">The identifier of the RPC method to invoke.</param>
    /// <param name="payload">The packet payload.</param>
    public void Serialize(PacketWriter packetWriter, CatchedNetworkIdentifier networkIdentifier, uint rpcId, IPacket? payload)
    {
        packetWriter.WriteNetworkId(networkIdentifier.NetworkId);
        packetWriter.WriteUInt(rpcId);
        if (networkIdentifier.NetworkComponentIndex != ReplantedOnlineMod.Constants.Network.NULL_NETWORK_COMPONENT_INDEX)
        {
            packetWriter.WriteBool(true);
            packetWriter.WritePackedInt(networkIdentifier.NetworkComponentIndex);
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
    /// Deserializes an RPC invocation from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the RPC data.</param>
    /// <returns>A new ObjectRpcMessage instance with deserialized data.</returns>
    public ObjectRpcMessage Deserialize(PacketReader packetReader)
    {
        NetworkIdentifier networkId = packetReader.ReadNetworkId();
        uint rpcId = packetReader.ReadUInt();
        bool isComponent = packetReader.ReadBool();

        int componentIndex;
        if (isComponent)
            componentIndex = packetReader.ReadPackedInt();
        else
            componentIndex = ReplantedOnlineMod.Constants.Network.NULL_NETWORK_COMPONENT_INDEX;

        ObjectRpcMessage message = new()
        {
            NetworkId = networkId,
            RpcId = rpcId,
            IsComponent = isComponent,
            ComponentIndex = componentIndex
        };

        return message;
    }
}