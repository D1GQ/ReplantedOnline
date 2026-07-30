using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.DolphinRider)]
internal sealed class DolphinRiderNetworkComponent : ZombieNetworkComponent
{
    private enum DolphinRiderRpcs : byte
    {
        Jump
    }

    private bool _hasJumped;
    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.DolphinInJump && !_hasJumped)
            {
                _hasJumped = true;
                SendJumpRpc();
            }
        }
        else
        {
            if (zombie.mZombiePhase == ZombiePhase.DolphinInJump)
            {
                SyncedPosX = null;
            }
        }
    }

    protected override void UpdatePosition(Zombie zombie, float distance, bool useNonNetworkLogic = false)
    {
        if (!Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.DolphinInJump)
            {
                base.UpdatePosition(zombie, distance, true);
                return;
            }
        }

        base.UpdatePosition(zombie, distance, useNonNetworkLogic);
    }

    private void SendJumpRpc()
    {
        if (Net.Zombie == null)
            return;

        SendNetworkComponentRpc(DolphinRiderRpcs.Jump);
    }

    [RpcHandler(DolphinRiderRpcs.Jump)]
    private void HandleJumpRpc()
    {
        if (Net.Zombie == null)
            return;

        if (!_hasJumped)
        {
            _hasJumped = true;
            Instances.GameplayActivity.m_audioService.PlayFoley(FoleyType.DolphinBeforeJumping);
            Net.Zombie.mZombiePhase = ZombiePhase.DolphinInJump;
            Net.Zombie.mVelX = 0.5f;
            Net.Zombie.PlayZombieReanim(Animations.DOLPHINRIDER_JUMP.Anim, ReanimLoopType.PlayOnceAndHold, Animations.DOLPHINRIDER_JUMP.Blend, Animations.DOLPHINRIDER_JUMP.Fps);
        }
    }
}
