using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class NetworkObjectResolver : IFastPacketResolver<NetworkObject>
{
    public bool CanResolve(Type type) => type == typeof(NetworkObject);
    public void Serialize(PacketWriter packetWriter, NetworkObject value) => packetWriter.WriteNetworkObject(value);
    public NetworkObject Deserialize(PacketReader packetReader, Type type) => packetReader.ReadNetworkObject();
}
