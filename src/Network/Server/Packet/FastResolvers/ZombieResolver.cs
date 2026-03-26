using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class ZombieResolver : IFastPacketResolver<Zombie>
{
    public bool CanResolve(Type type) => type == typeof(Zombie);

    public void Serialize(PacketWriter packetWriter, Zombie value)
    {
        var netZombie = value.GetNetworked();
        packetWriter.WriteNetworkObject(netZombie);
    }

    public Zombie Deserialize(PacketReader packetReader, Type type)
    {
        var netZombie = packetReader.ReadNetworkObject<ZombieNetworked>();
        return netZombie._Zombie;
    }
}