using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class PacketWriterResolver : IFastPacketResolver<IPacket>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type.IsAssignableFrom(typeof(IPacket));

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, IPacket value) => packetWriter.WritePacket(value);

    /// <inheritdoc/>
    public IPacket Deserialize(PacketReader packetReader, Type type) => null;
}
