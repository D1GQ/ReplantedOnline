using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Network.Reloaded.Serialization.FastResolvers;

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
