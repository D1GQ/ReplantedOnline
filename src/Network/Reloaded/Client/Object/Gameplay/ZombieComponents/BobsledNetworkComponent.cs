using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Bobsled)]
internal sealed class BobsledNetworkComponent : ZombieNetworkComponent
{
    internal override void UpdatePosition(float distance, bool useNonNetworkLogic = false)
    {
        if (Net.Zombie == null)
            return;

        // Lock passengers positions to leader when in sled
        var leader = Net.Zombie.mBoard.ZombieGet(Net.Zombie.mRelatedZombieID);
        if (leader != null && leader.mZombiePhase != ZombiePhase.ZombieNormal)
        {
            Net.Zombie.mPosX = leader.mPosX + 50 * Net.Zombie.GetBobsledPosition();
            SyncedPosX = null;
            return;
        }

        base.UpdatePosition(distance, useNonNetworkLogic);
    }

    public sealed override void Serialize(PacketWriter packetWriter, bool init)
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

    public sealed override void Deserialize(PacketReader packetReader, bool init)
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
