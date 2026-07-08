using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Squash)]
internal sealed class SquashNetworkComponent : PlantNetworkComponent
{
    private enum SquashRpcs : byte
    {
        Look,
        Jump
    }

    private bool _looking;
    private bool _jump;
    internal sealed override void Update()
    {
        if (Net.Plant! == null) return;

        if (Net.AmOwner)
        {
            if (Net.Plant.mState == PlantState.SquashLook && !_looking)
            {
                _looking = true;
                Zombie target = Net.Plant.mBoard.ZombieGet(Net.Plant.mTargetZombieID);
                Net.SendSetZombieTargetRpc(target);
                SendLookRpc();
            }
            else if (Net.Plant.mState == PlantState.SquashPreLaunch && !_jump)
            {
                _jump = true;
                Zombie target = Net.Plant.mBoard.ZombieGet(Net.Plant.mTargetZombieID);
                Net.SendSetZombieTargetRpc(target);
                SendJumpRpc();
            }
        }

        UpdateHealthSync();
    }

    private void SendLookRpc()
    {
        SendNetworkComponentRpc(SquashRpcs.Look);
    }

    [RpcHandler(SquashRpcs.Look)]
    private void HandleLookRpc()
    {
        if (Net.Plant! == null) return;
        if (_looking) return;
        _looking = true;

        var target = Net.Target;
        if (target == null) return;
        Net.Plant.mTargetZombieID = target.DataID;

        Net.Plant.mState = PlantState.SquashLook;
        Net.Plant.mStateCountdown = int.MaxValue;

        Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_SQUASH_HMM);
        if (target.mX > Net.Plant.mX)
        {
            Net.Plant.mController.PlayAnimationOnTrack(Animations.SQUASH_LOOKRIGHT.Anim, CharacterAnimationTrack.Body, Animations.SQUASH_LOOKLEFT.Fps, ReanimLoopType.PlayOnceFullLastFrameAndHold);
        }
        else
        {
            Net.Plant.mController.PlayAnimationOnTrack(Animations.SQUASH_LOOKLEFT.Anim, CharacterAnimationTrack.Body, Animations.SQUASH_LOOKLEFT.Fps, ReanimLoopType.PlayOnceFullLastFrameAndHold);
        }
    }

    private void SendJumpRpc()
    {
        SendNetworkComponentRpc(SquashRpcs.Jump);
    }

    [RpcHandler(SquashRpcs.Jump)]
    private void HandleJumpRpc()
    {
        if (Net.Plant! == null) return;
        if (_jump) return;
        _jump = true;

        var target = Net.Target;
        if (target == null) return;
        Net.Plant.mTargetZombieID = target.DataID;

        Net.Plant.mTargetX = Mathf.FloorToInt(target.mPosX);
        Net.Plant.mTargetY = Mathf.FloorToInt(target.mPosY);
        Net.Plant.mState = PlantState.SquashLook;
        Net.Plant.mStateCountdown = 0;
    }
}
