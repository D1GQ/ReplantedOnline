using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.PlantComponents;

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
    internal override void Update()
    {
        if (Net.AmOwner)
        {
            if (Net._Plant.mState == PlantState.SquashLook && !_looking)
            {
                _looking = true;
                Zombie target = Net._Plant.mBoard.ZombieGet(Net._Plant.mTargetZombieID);
                Net.SendSetZombieTargetRpc(target);
                SendLookRpc();
            }
            else if (Net._Plant.mState == PlantState.SquashPreLaunch && !_jump)
            {
                _jump = true;
                Zombie target = Net._Plant.mBoard.ZombieGet(Net._Plant.mTargetZombieID);
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
        if (_looking) return;
        _looking = true;

        var target = Net.Target;
        Net._Plant.mTargetZombieID = target.DataID;

        Net._Plant.mState = PlantState.SquashLook;
        Net._Plant.mStateCountdown = int.MaxValue;

        Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_SQUASH_HMM);
        if (target.mX > Net._Plant.mX)
        {
            Net._Plant.mController.PlayAnimationOnTrack(Animations.SQUASH_LOOKRIGHT.Anim, CharacterAnimationTrack.Body, Animations.SQUASH_LOOKLEFT.Fps, ReanimLoopType.PlayOnceFullLastFrameAndHold);
        }
        else
        {
            Net._Plant.mController.PlayAnimationOnTrack(Animations.SQUASH_LOOKLEFT.Anim, CharacterAnimationTrack.Body, Animations.SQUASH_LOOKLEFT.Fps, ReanimLoopType.PlayOnceFullLastFrameAndHold);
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

        var target = Net.Target;
        Net._Plant.mTargetZombieID = target.DataID;

        Net._Plant.mTargetX = Mathf.FloorToInt(target.mPosX);
        Net._Plant.mTargetY = Mathf.FloorToInt(target.mPosY);
        Net._Plant.mState = PlantState.SquashLook;
        Net._Plant.mStateCountdown = 0;
    }
}
