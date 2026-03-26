using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class ZombieResolver : IFastPacketResolver<Zombie>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(Zombie);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, Zombie value)
    {
        if (value != null)
        {
            var netZombie = value.GetNetworked();
            packetWriter.WriteNetworkObject(netZombie);
        }
        else
        {
            packetWriter.WriteNetworkObject(null);
        }
    }

    /// <inheritdoc/>
    public Zombie Deserialize(PacketReader packetReader, Type type)
    {
        var netZombie = packetReader.ReadNetworkObject<ZombieNetworked>();
        if (netZombie != null)
        {
            return netZombie._Zombie;
        }
        else
        {
            return null;
        }
    }
}