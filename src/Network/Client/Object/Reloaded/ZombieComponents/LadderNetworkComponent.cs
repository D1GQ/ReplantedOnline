using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Ladder)]
internal sealed class LadderNetworkComponent : ZombieNetworkComponent
{
    private enum LadderRpcs : byte
    {
        DonePlacingLadder
    }

    private bool _donePlacingLadder;
    internal override void Update()
    {
        if (Net._Zombie == null) return;

        if (Net._Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (Net.AmOwner)
        {
            if (Net._Zombie.mZombiePhase == ZombiePhase.LadderPlacing && Net.Target == null)
            {
                // Send target to place ladder on
                Plant target = Net._Zombie.FindPlantTarget(ZombieAttackType.Ladder);
                Net.SendSetPlantTargetRpc(target);
            }
            else if (Net._Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
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
            if (Net._Zombie.mZombiePhase == ZombiePhase.LadderPlacing && Net._Zombie.mPhaseCounter == 0)
            {
                if (_donePlacingLadder)
                {
                    Net._Zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                    Net._Zombie.DetachShield();
                    _donePlacingLadder = false;
                }
            }

            // Rest of non owner logic is handled in LadderZombiePatch.cs
        }

        if (Net._Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            Net.Target = null;
        }

        UpdatePositionSync();
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
