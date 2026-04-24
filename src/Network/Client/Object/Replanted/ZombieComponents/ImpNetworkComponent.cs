using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class ImpNetworkComponent : ZombieNetworkComponent
{
    internal float ImpRandomArc;
    internal override void Update()
    {
        if (ZombieNetworked._Zombie.mZombiePhase is not (ZombiePhase.ImpGettingThrown or ZombiePhase.ImpLanding))
        {
            UpdatePositionSync();
        }
    }

    internal override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            GargantuarZombiePatch.ImpSerialize(ZombieNetworked, packetWriter);
        }

        base.Serialize(packetWriter, init);
    }

    internal override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            GargantuarZombiePatch.ImpDeserialize(ZombieNetworked, packetReader);
        }

        base.Deserialize(packetReader, init);
    }
}
