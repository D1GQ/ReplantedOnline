using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Serialization.FastResolvers;

[RegisterFastPacketResolver]
internal class PackedIntResolver : IFastPacketResolver
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(PackedInt);

    /// <inheritdoc/>
    public void UnsafeSerialize(PacketWriter packetWriter, object value) => packetWriter.WritePackedInt((PackedInt)value);

    /// <inheritdoc/>
    public object UnsafeDeserialize(PacketReader packetReader, Type type) => (PackedInt)packetReader.ReadPackedInt();
}