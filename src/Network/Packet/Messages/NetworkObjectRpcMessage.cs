using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Packet.Messages;

/// <summary>
/// Represents a network message for invoking Remote Procedure Calls (RPCs) on network objects.
/// </summary>
internal readonly struct NetworkObjectRpcMessage : IMessage<NetworkObjectRpcMessage, INetworkObject, byte>
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
    /// Serializes a network object RPC invocation into a packet for network transmission.
    /// </summary>
    /// <param name="networkObj">The target network object instance to serialize.</param>
    /// <param name="rpcId">The identifier of the RPC method to invoke.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    public void Serialize(INetworkObject networkObj, byte rpcId, PacketWriter packetWriter)
    {
        packetWriter.WriteUInt(networkObj.NetworkId);
        packetWriter.WriteByte(rpcId);
    }

    /// <summary>
    /// Deserializes an RPC invocation message from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the RPC message data.</param>
    /// <returns>A new NetworkObjectRpcMessage instance with deserialized data.</returns>
    public NetworkObjectRpcMessage Deserialize(PacketReader packetReader)
    {
        NetworkObjectRpcMessage message = new()
        {
            NetworkId = packetReader.ReadUInt(),
            RpcId = packetReader.ReadByte()
        };

        return message;
    }
}