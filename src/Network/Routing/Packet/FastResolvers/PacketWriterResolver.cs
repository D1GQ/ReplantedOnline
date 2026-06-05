using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Routing.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class PacketWriterResolver : IFastPacketResolver<IPacket>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type.IsAssignableFrom(typeof(IPacket));

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, IPacket value) => packetWriter.WritePacketToBuffer(value);

    /// <inheritdoc/>
    public IPacket Deserialize(PacketReader packetReader, Type type) => default!;
}
