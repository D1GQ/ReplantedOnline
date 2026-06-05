using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Routing.Packet.Messages;

/// <summary>
/// Represents a network message header for invoking Remote Procedure Calls (RPCs).
/// </summary>
internal readonly struct PacketHeaderMessage : IMessage<PacketHeaderMessage, PacketHandlerType, IPacket>
{
    /// <summary>
    /// Gets the packet handler type of message this header contains.
    /// </summary>
    public PacketHandlerType HandlerType { get; private init; }

    /// <summary>
    /// Gets the signature hash used for validating message integrity and origin.
    /// </summary>
    public uint SignatureHash { get; private init; }

    /// <summary>
    /// Serializes the header message into a packet buffer.
    /// </summary>
    /// <param name="packetHandlerType">The packet handler type tag to write to the packet.</param>
    /// <param name="payload">The packet containing the RPC payload data.</param>
    /// <param name="packetWriter">The target packet writer to write the serialized header data to.</param>
    public void Serialize(PacketHandlerType packetHandlerType, IPacket? payload, PacketWriter packetWriter)
    {
        packetWriter.AddTag(packetHandlerType);
        if (payload != null)
        {
            packetWriter.WritePacketToBuffer(payload);
        }
        packetWriter.EncryptBuffer();
    }

    /// <summary>
    /// Deserializes a header message from incoming network data.
    /// </summary>
    /// <param name="packetReader">The packet reader positioned at the start of a header message.</param>
    /// <returns>A new HeaderMessage instance with deserialized data.</returns>
    public PacketHeaderMessage Deserialize(PacketReader packetReader)
    {
        uint signatureHash = packetReader.UnencryptBuffer();
        PacketHandlerType tag = packetReader.GetTag();
        PacketHeaderMessage message = new()
        {
            HandlerType = tag,
            SignatureHash = signatureHash
        };

        return message;
    }
}