using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Ladder)]
internal sealed class LadderNetworkComponent : ZombieNetworkComponent
{
    private bool _donePlacingLadder;
    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (zombie.mZombiePhase == ZombiePhase.RisingFromGrave)
            return;

        if (Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.LadderPlacing && Net.Target == null)
            {
                // Send target to place ladder on
                Plant target = zombie.FindPlantTarget(ZombieAttackType.Ladder);
                Net.RpcSetPlantTarget(target);
            }
            else if (zombie.mZombiePhase == ZombiePhase.ZombieNormal)
            {
                // Send the zombie is done placing down ladder
                if (!_donePlacingLadder)
                {
                    _donePlacingLadder = true;
                    RpcDonePlacingLadder();
                }
            }
        }
        else
        {
            if (zombie.mZombiePhase == ZombiePhase.LadderPlacing && zombie.mPhaseCounter == 0)
            {
                if (_donePlacingLadder)
                {
                    zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                    zombie.DetachShield();
                    _donePlacingLadder = false;
                }
            }

            // Rest of non owner logic is handled in LadderZombiePatch.cs
        }

        if (zombie.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            Net.Target = null;
        }
    }

    [MethodRpc(LocalExecution.None)]
    private void RpcDonePlacingLadder()
    {
        _donePlacingLadder = true;
    }
}
