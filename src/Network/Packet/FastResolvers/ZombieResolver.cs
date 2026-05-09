using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Modded;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Reloaded;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Packet.FastResolvers;

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