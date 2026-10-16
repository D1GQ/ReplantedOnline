using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Pogo)]
internal sealed class PogoNetworkComponent : ZombieNetworkComponent
{
    private bool _inJump;

    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (zombie.mZombiePhase == ZombiePhase.RisingFromGrave)
            return;

        if (Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.PogoForwardBounce2 && !_inJump)
            {
                _inJump = true;
                RpcJumpOver(zombie.mPhaseCounter);
            }
        }
        else
        {
            if (zombie.mZombiePhase == ZombiePhase.PogoForwardBounce2)
            {
                SyncedPosX = null;
            }
        }

        if (zombie.mZombiePhase != ZombiePhase.PogoForwardBounce2)
        {
            _inJump = false;
        }
    }

    protected override void UpdatePosition(Zombie zombie, float distance, bool useNonNetworkLogic = false)
    {
        if (!Net.AmOwner)
        {
            if (_inJump)
            {
                base.UpdatePosition(zombie, distance, true);
                return;
            }
        }

        base.UpdatePosition(zombie, distance, useNonNetworkLogic);
    }

    [MethodRpc(LocalExecution.None)]
    private void RpcJumpOver(int PhaseCounter)
    {
        if (Net.Zombie == null)
            return;

        _inJump = true;
        Net.Zombie.mPhaseCounter = PhaseCounter;
        Net.Zombie.mZombiePhase = ZombiePhase.PogoForwardBounce2;
    }

    [MethodRpc(LocalExecution.None)]
    internal void RpcPogoBreak(DamageFlags damageFlags)
    {
        if (Net.Zombie == null)
            return;

        Net.Zombie.PogoBreakOriginal(damageFlags);
    }
}
