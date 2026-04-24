using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class LadderNetworkComponent : ZombieNetworkComponent
{
    private enum LadderRpcs : byte
    {
        DonePlacingLadder
    }

    private bool _donePlacingLadder;
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.LadderPlacing && ZombieNetworked.Target == null)
            {
                // Send target to place ladder on
                Plant target = ZombieNetworked._Zombie.FindPlantTarget(ZombieAttackType.Ladder);
                ZombieNetworked.SendSetPlantTargetRpc(target);
            }
            else if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
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
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.LadderPlacing && ZombieNetworked._Zombie.mPhaseCounter == 0)
            {
                if (_donePlacingLadder)
                {
                    ZombieNetworked._Zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                    ZombieNetworked._Zombie.DetachShield();
                    _donePlacingLadder = false;
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

    internal void SendDonePlacingLadderRpc()
    {
        SendNetworkComponentRpc(LadderRpcs.DonePlacingLadder);
    }

    [RpcHandler(LadderRpcs.DonePlacingLadder)]
    private void HandleDonePlacingLadderRpc()
    {
        _donePlacingLadder = true;
    }
}
