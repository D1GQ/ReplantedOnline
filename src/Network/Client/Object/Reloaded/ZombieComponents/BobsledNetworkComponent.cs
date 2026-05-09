using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
internal sealed class BobsledNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            UpdatePositionSync();
        }
    }

    internal override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            BobsledZombiePatch.BobsledSerialize(ZombieNetworked._Zombie, packetWriter);
        }

        base.Serialize(packetWriter, init);
    }

    internal override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            BobsledZombiePatch.BobsledDeserialize(ZombieNetworked._Zombie, packetReader);
        }

        base.Deserialize(packetReader, init);
    }
}
