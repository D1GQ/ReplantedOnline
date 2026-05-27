using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Reloaded;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Routing.Packet.FastResolvers;

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
            var zombieNetworked = value.GetNetworked();
            packetWriter.WriteNetworkObject(zombieNetworked);
        }
        else
        {
            packetWriter.WriteNetworkObject(null);
        }
    }

    /// <inheritdoc/>
    public Zombie Deserialize(PacketReader packetReader, Type type)
    {
        var zombieNetworked = packetReader.ReadNetworkObject<ZombieNetworked>();
        if (zombieNetworked != null)
        {
            return zombieNetworked._Zombie;
        }
        else
        {
            return null;
        }
    }
}