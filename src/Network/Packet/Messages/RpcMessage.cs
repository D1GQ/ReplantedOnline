using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;

/// <summary>
/// Represents a network message that wraps an RPC call with its invocation type.
/// </summary>
internal readonly struct RpcMessage : IMessage<RpcMessage, RpcType>
{
    /// <summary>
    /// Gets the delivery type for the RPC invocation.
    /// </summary>
    public RpcType RpcType { get; private init; }

    /// <summary>
    /// Serializes an RPC type enumeration into a packet for network transmission.
    /// </summary>
    /// <param name="rpcType">The RPC type configuration to serialize.</param>
    /// <param name="packetWriter">The packet writer to write the serialized data to.</param>
    public void Serialize(RpcType rpcType, PacketWriter packetWriter)
    {
        packetWriter.WriteEnum(rpcType);
    }

    /// <summary>
    /// Deserializes an RPC type message from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader containing the RPC type data.</param>
    /// <returns>A new RpcMessage instance with deserialized data.</returns>
    public RpcMessage Deserialize(PacketReader packetReader)
    {
        RpcMessage message = new()
        {
            RpcType = packetReader.ReadEnum<RpcType>()
        };

        return message;
    }
}