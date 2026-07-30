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
    protected override void UpdatePosition(Zombie zombie, float distance, bool useNonNetworkLogic = false)
    {
        // Lock passengers positions to leader when in sled
        var leader = zombie.mBoard.ZombieGet(zombie.mRelatedZombieID);
        if (leader != null && leader.mZombiePhase != ZombiePhase.ZombieNormal)
        {
            zombie.mPosX = leader.mPosX + 50 * zombie.GetBobsledPosition();
            SyncedPosX = null;
            return;
        }

        base.UpdatePosition(zombie, distance, useNonNetworkLogic);
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
