using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class EnumResolver : IFastPacketResolver
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type != null && type.IsEnum;

    /// <inheritdoc/>
    public void UnsafeSerialize(PacketWriter packetWriter, object value)
    {
        packetWriter.WriteInt(Convert.ToInt32(value));
    }

    /// <inheritdoc/>
    public object UnsafeDeserialize(PacketReader packetReader, Type type)
    {
        int enumValue = packetReader.ReadInt();
        return Enum.ToObject(type, enumValue);
    }
}