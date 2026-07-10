using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Imp)]
internal sealed class ImpNetworkComponent : ZombieNetworkComponent
{
    internal float ImpRandomArc;

    public sealed override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            GargantuarZombiePatch.ImpSerialize(Net, packetWriter);
        }

        base.Serialize(packetWriter, init);
    }

    public sealed override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            GargantuarZombiePatch.ImpDeserialize(Net, packetReader);
        }

        base.Deserialize(packetReader, init);
    }
}
