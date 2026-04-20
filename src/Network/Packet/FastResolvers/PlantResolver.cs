using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class PlantResolver : IFastPacketResolver<Plant>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(Plant);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, Plant value)
    {
        if (value != null)
        {
            var netPlant = value.GetNetworked();
            packetWriter.WriteNetworkObject(netPlant);
        }
        else
        {
            packetWriter.WriteNetworkObject(null);
        }
    }

    /// <inheritdoc/>
    public Plant Deserialize(PacketReader packetReader, Type type)
    {
        var netPlant = packetReader.ReadNetworkObject<PlantNetworked>();
        if (netPlant != null)
        {
            return netPlant._Plant;
        }
        else
        {
            return null;
        }
    }
}