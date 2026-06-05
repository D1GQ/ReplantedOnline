using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Network.Routing.Packet;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Bobsled)]
internal sealed class BobsledNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        if (Net.Zombie?.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            UpdatePositionSync();
        }
    }

    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            packetWriter.WriteBool(Net.Zombie == null);
            if (Net.Zombie != null)
            {
                BobsledZombiePatch.BobsledSerialize(Net.Zombie, packetWriter);
            }
        }

        base.Serialize(packetWriter, init);
    }

    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            bool isZombieNull = packetReader.ReadBool();
            if (!isZombieNull && Net.Zombie != null)
            {
                BobsledZombiePatch.BobsledDeserialize(Net.Zombie, packetReader);
            }
        }

        base.Deserialize(packetReader, init);
    }
}
