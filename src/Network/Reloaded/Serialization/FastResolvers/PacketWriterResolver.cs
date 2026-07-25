using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Reloaded.Serialization.FastResolvers;

[RegisterFastPacketResolver]
internal class PacketWriterResolver : IFastPacketResolver<IPacket>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type.IsAssignableFrom(typeof(IPacket));

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, IPacket value) => packetWriter.AddBytesToBuffer(value.GetBytes());

    /// <inheritdoc/>
    public IPacket Deserialize(PacketReader packetReader, Type type) => default!;
}
