using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Reloaded.Components;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Imp)]
internal sealed class ImpNetworkComponent : ZombieNetworkComponent
{
    internal float ImpRandomArc;
    internal sealed override void Update()
    {
        if (Net.Zombie?.mZombiePhase is not (ZombiePhase.ImpGettingThrown or ZombiePhase.ImpLanding))
        {
            UpdatePositionSync();
        }
    }

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
