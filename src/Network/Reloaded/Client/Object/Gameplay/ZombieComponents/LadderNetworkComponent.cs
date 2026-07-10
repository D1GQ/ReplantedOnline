using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Ladder)]
internal sealed class LadderNetworkComponent : ZombieNetworkComponent
{
    private enum LadderRpcs : byte
    {
        DonePlacingLadder
    }

    private bool _donePlacingLadder;
    internal sealed override void Update()
    {
        if (Net.Zombie == null) return;

        if (Net.Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.LadderPlacing && Net.Target == null)
            {
                // Send target to place ladder on
                Plant target = Net.Zombie.FindPlantTarget(ZombieAttackType.Ladder);
                Net.SendSetPlantTargetRpc(target);
            }
            else if (Net.Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
            {
                // Send the zombie is done placing down ladder
                if (!_donePlacingLadder)
                {
                    _donePlacingLadder = true;
                    SendDonePlacingLadderRpc();
                }
            }
        }
        else
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.LadderPlacing && Net.Zombie.mPhaseCounter == 0)
            {
                if (_donePlacingLadder)
                {
                    Net.Zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                    Net.Zombie.DetachShield();
                    _donePlacingLadder = false;
                }
            }

            // Rest of non owner logic is handled in LadderZombiePatch.cs
        }

        if (Net.Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            Net.Target = null;
        }
    }

    private void SendDonePlacingLadderRpc()
    {
        SendNetworkComponentRpc(LadderRpcs.DonePlacingLadder);
    }

    [RpcHandler(LadderRpcs.DonePlacingLadder)]
    private void HandleDonePlacingLadderRpc()
    {
        _donePlacingLadder = true;
    }
}
