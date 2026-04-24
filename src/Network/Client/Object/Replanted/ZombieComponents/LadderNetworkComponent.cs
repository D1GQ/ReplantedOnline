using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class LadderNetworkComponent : ZombieNetworkComponent
{
    private bool hasPlacedLadder;
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.LadderPlacing && ZombieNetworked.Target == null)
            {
                // Send target to place ladder
                Plant target = ZombieNetworked._Zombie.FindPlantTarget(ZombieAttackType.Ladder);
                ZombieNetworked.SendSetPlantTargetRpc(target);
            }
            else if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
            {
                if (!hasPlacedLadder)
                {
                    hasPlacedLadder = true;
                    ZombieNetworked.SendSetStateRpc(NetStates.LADDER_ZOMBIE_PLACED_LADDER);
                }
            }
        }
        else
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.LadderPlacing && ZombieNetworked._Zombie.mPhaseCounter == 0)
            {
                if (ZombieNetworked.State is NetStates.LADDER_ZOMBIE_PLACED_LADDER)
                {
                    ZombieNetworked._Zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                    ZombieNetworked._Zombie.DetachShield();
                    ZombieNetworked.State = null;
                }
            }

            // Rest of non owner logic is handled in LadderZombiePatch.cs
        }

        if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            ZombieNetworked.Target = null;
        }

        UpdatePositionSync();
    }
}
