using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object;

namespace ReplantedOnline.Network.Routing.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class NetworkObjectResolver : IFastPacketResolver<NetworkObject>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => typeof(NetworkObject).IsAssignableFrom(type);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, NetworkObject value) => packetWriter.WriteNetworkObject(value);

    /// <inheritdoc/>
    public NetworkObject Deserialize(PacketReader packetReader, Type type) => packetReader.ReadNetworkObject();
}
