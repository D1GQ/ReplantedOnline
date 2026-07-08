using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Serialization.FastResolvers;

[RegisterFastPacketResolver]
internal class IDResolver : IFastPacketResolver<ID>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(ID);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, ID value) => packetWriter.WriteID(value);

    /// <inheritdoc/>
    public ID Deserialize(PacketReader packetReader, Type type) => packetReader.ReadID();
}
