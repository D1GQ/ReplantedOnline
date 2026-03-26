using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class PlantResolver : IFastPacketResolver<Plant>
{
    public bool CanResolve(Type type) => type == typeof(Plant);

    public void Serialize(PacketWriter packetWriter, Plant value)
    {
        var netPlant = value.GetNetworked();
        packetWriter.WriteNetworkObject(netPlant);
    }

    public Plant Deserialize(PacketReader packetReader, Type type)
    {
        var netPlant = packetReader.ReadNetworkObject<PlantNetworked>();
        return netPlant._Plant;
    }
}