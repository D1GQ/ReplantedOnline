using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class PacketWriterResolver : IFastPacketResolver<IPacket>
{
    public bool CanResolve(Type type) => type.IsAssignableFrom(typeof(IPacket));
    public void Serialize(PacketWriter packetWriter, IPacket value) => packetWriter.WritePacket(value);
    public IPacket Deserialize(PacketReader packetReader, Type type) => null;
}
