using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

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
