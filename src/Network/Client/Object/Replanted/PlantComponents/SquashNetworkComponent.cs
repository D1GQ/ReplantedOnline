using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Attributes;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal sealed class SquashNetworkComponent : PlantNetworkComponent
{
    private enum SquashRpcs : byte
    {
        Look,
        Jump
    }

    private bool _looking;
    private bool _jump;
    internal override void Update()
    {
        if (PlantNetworked.AmOwner)
        {
            if (PlantNetworked._Plant.mState == PlantState.SquashLook && !_looking)
            {
                _looking = true;
                Zombie target = PlantNetworked._Plant.mBoard.ZombieGet(PlantNetworked._Plant.mTargetZombieID);
                PlantNetworked.SendSetZombieTargetRpc(target);
                SendLookRpc(true);
            }
            else if (PlantNetworked._Plant.mState == PlantState.SquashPreLaunch && !_jump)
            {
                _jump = true;
                Zombie target = PlantNetworked._Plant.mBoard.ZombieGet(PlantNetworked._Plant.mTargetZombieID);
                PlantNetworked.SendSetZombieTargetRpc(target);
                SendJumpRpc();
            }
        }

        UpdateHealthSync();
    }

    private void SendLookRpc(bool look)
    {
        SendNetworkComponentRpc(SquashRpcs.Look, look);
    }

    [RpcHandler(SquashRpcs.Look)]
    private void HandleLookRpc()
    {
        if (_looking) return;
        _looking = true;

        var target = PlantNetworked.Target;
        PlantNetworked._Plant.mTargetZombieID = target.DataID;

        PlantNetworked._Plant.mState = PlantState.SquashLook;
        PlantNetworked._Plant.mStateCountdown = int.MaxValue;

        Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_SQUASH_HMM);
        if (target.mX > PlantNetworked._Plant.mX)
        {
            PlantNetworked._Plant.mController.PlayAnimationOnTrack(Animations.SQUASH_LOOKRIGHT.Anim, CharacterAnimationTrack.Body, Animations.SQUASH_LOOKLEFT.Fps, ReanimLoopType.PlayOnceFullLastFrameAndHold);
        }
        else
        {
            PlantNetworked._Plant.mController.PlayAnimationOnTrack(Animations.SQUASH_LOOKLEFT.Anim, CharacterAnimationTrack.Body, Animations.SQUASH_LOOKLEFT.Fps, ReanimLoopType.PlayOnceFullLastFrameAndHold);
        }
    }

    private void SendJumpRpc()
    {
        SendNetworkComponentRpc(SquashRpcs.Jump);
    }

    [RpcHandler(SquashRpcs.Jump)]
    private void HandleJumpRpc()
    {
        if (_jump) return;
        _jump = true;

        var target = PlantNetworked.Target;
        PlantNetworked._Plant.mTargetZombieID = target.DataID;

        PlantNetworked._Plant.mTargetX = Mathf.FloorToInt(target.mPosX);
        PlantNetworked._Plant.mTargetY = Mathf.FloorToInt(target.mPosY);
        PlantNetworked._Plant.mState = PlantState.SquashLook;
        PlantNetworked._Plant.mStateCountdown = 0;
    }
}
