using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Ladder)]
internal sealed class LadderNetworkComponent : ZombieNetworkComponent
{
    private enum LadderMsgs : byte
    {
        DonePlacingLadder
    }

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
                Net.SendSetPlantTargetMessage(target);
            }
            else if (zombie.mZombiePhase == ZombiePhase.ZombieNormal)
            {
                // Send the zombie is done placing down ladder
                if (!_donePlacingLadder)
                {
                    _donePlacingLadder = true;
                    SendDonePlacingLadderMessage();
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

    private void SendDonePlacingLadderMessage()
    {
        SendMessage(LadderMsgs.DonePlacingLadder);
    }

    [ObjectMessageHandler(LadderMsgs.DonePlacingLadder)]
    private void HandleDonePlacingLadderMessage()
    {
        _donePlacingLadder = true;
    }
}
