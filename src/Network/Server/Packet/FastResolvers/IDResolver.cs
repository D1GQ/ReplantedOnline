using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class IDResolver : IFastPacketResolver<ID>
{
    public bool CanResolve(Type type) => type == typeof(ID);
    public void Serialize(PacketWriter packetWriter, ID value) => packetWriter.WriteID(value);
    public ID Deserialize(PacketReader packetReader, Type type) => packetReader.ReadID();
}
