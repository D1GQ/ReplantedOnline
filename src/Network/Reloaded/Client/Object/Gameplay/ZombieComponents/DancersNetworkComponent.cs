using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Dancer)]
[RegisterNetworkComponent(ZombieType.BackupDancer)]
internal sealed class DancersNetworkComponent : ZombieNetworkComponent
{
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            packetWriter.WriteBool(Net.Zombie == null);
            if (Net.Zombie != null)
            {
                DancersZombiePatch.DancersSerialize(Net.Zombie, packetWriter);
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
                DancersZombiePatch.DancersDeserialize(Net.Zombie, packetReader);
            }
        }

        base.Deserialize(packetReader, init);
    }
}
