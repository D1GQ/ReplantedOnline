using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Pogo)]
internal sealed class PogoNetworkComponent : ZombieNetworkComponent
{
    private enum PogoRpcs : byte
    {
        JumpOver,
        PogoBreak
    }

    private bool _inJump;

    internal sealed override void OnUpdate()
    {
        if (Net.Zombie == null) return;

        if (Net.Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.PogoForwardBounce2 && !_inJump)
            {
                _inJump = true;
                SendJumpOverRpc();
            }
        }
        else
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.PogoForwardBounce2)
            {
                SyncedPosX = null;
            }
        }

        if (Net.Zombie.mZombiePhase != ZombiePhase.PogoForwardBounce2)
        {
            _inJump = false;
        }
    }

    internal override void UpdatePosition(float distance, bool useNonNetworkLogic = false)
    {
        if (Net.Zombie == null)
            return;

        if (!Net.AmOwner)
        {
            if (_inJump)
            {
                base.UpdatePosition(distance, true);
                return;
            }
        }

        base.UpdatePosition(distance, useNonNetworkLogic);
    }

    private void SendJumpOverRpc()
    {
        if (Net.Zombie == null)
            return;

        SendNetworkComponentRpc(PogoRpcs.JumpOver, Net.Zombie.mPhaseCounter);
    }

    [RpcHandler(PogoRpcs.JumpOver)]
    private void HandleJumpOverRpc(int PhaseCounter)
    {
        if (Net.Zombie == null)
            return;

        _inJump = true;
        Net.Zombie.mPhaseCounter = PhaseCounter;
        Net.Zombie.mZombiePhase = ZombiePhase.PogoForwardBounce2;
    }

    internal void SendPogoBreakRpc(DamageFlags damageFlags)
    {
        if (Net.Zombie == null)
            return;

        SendNetworkComponentRpc(PogoRpcs.PogoBreak, damageFlags);
    }

    [RpcHandler(PogoRpcs.PogoBreak)]
    private void HandlePogoBreakRpc(DamageFlags damageFlags)
    {
        if (Net.Zombie == null)
            return;

        Net.Zombie.PogoBreakOriginal(damageFlags);
    }
}
