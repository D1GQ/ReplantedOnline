using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class EnumResolver : IFastPacketResolver
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type != null && type.IsEnum;

    /// <inheritdoc/>
    public void UnsafeSerialize(PacketWriter packetWriter, object value) => packetWriter.WriteEnum(value as Enum);

    /// <inheritdoc/>
    public object UnsafeDeserialize(PacketReader packetReader, Type type) => packetReader.ReadEnum(type);
}