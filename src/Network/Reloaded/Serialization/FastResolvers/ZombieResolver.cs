using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client.Object.Reloaded;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Serialization.FastResolvers;

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
            return zombieNetworked.Zombie!;
        }
        else
        {
            return default!;
        }
    }
}