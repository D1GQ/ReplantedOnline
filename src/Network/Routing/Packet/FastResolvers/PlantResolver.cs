using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Reloaded;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Routing.Packet.FastResolvers;

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
            var plantNetworked = value.GetNetworked();
            packetWriter.WriteNetworkObject(plantNetworked);
        }
        else
        {
            packetWriter.WriteNetworkObject(null);
        }
    }

    /// <inheritdoc/>
    public Plant Deserialize(PacketReader packetReader, Type type)
    {
        var plantNetworked = packetReader.ReadNetworkObject<PlantNetworked>();
        if (plantNetworked != null)
        {
            return plantNetworked.Plant;
        }
        else
        {
            return null;
        }
    }
}