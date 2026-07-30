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
    internal sealed override void OnUpdate(Plant plant)
    {
        if (Net.AmOwner)
        {
            if (plant.mState == PlantState.SquashLook && !_looking)
            {
                _looking = true;
                Zombie target = plant.mBoard.ZombieGet(plant.mTargetZombieID);
                Net.SendSetZombieTargetRpc(target);
                SendLookRpc();
            }
            else if (plant.mState == PlantState.SquashPreLaunch && !_jump)
            {
                _jump = true;
                Zombie target = plant.mBoard.ZombieGet(plant.mTargetZombieID);
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
        var plant = Net.Plant;
        if (plant == null)
            return;

        if (_looking)
            return;

        _looking = true;

        var target = Net.Target;
        if (target == null)
            return;

        plant.mTargetZombieID = target.DataID;

        plant.mState = PlantState.SquashLook;
        plant.mStateCountdown = int.MaxValue;

        Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_SQUASH_HMM);
        if (target.mX > plant.mX)
        {
            plant.mController.PlayAnimationOnTrack(Animations.SQUASH_LOOKRIGHT.Anim, CharacterAnimationTrack.Body, Animations.SQUASH_LOOKLEFT.Fps, ReanimLoopType.PlayOnceFullLastFrameAndHold);
        }
        else
        {
            plant.mController.PlayAnimationOnTrack(Animations.SQUASH_LOOKLEFT.Anim, CharacterAnimationTrack.Body, Animations.SQUASH_LOOKLEFT.Fps, ReanimLoopType.PlayOnceFullLastFrameAndHold);
        }
    }

    private void SendJumpRpc()
    {
        SendNetworkComponentRpc(SquashRpcs.Jump);
    }

    [RpcHandler(SquashRpcs.Jump)]
    private void HandleJumpRpc()
    {
        var plant = Net.Plant;
        if (plant == null)
            return;

        if (_jump)
            return;

        _jump = true;

        var target = Net.Target;
        if (target == null)
            return;

        plant.mTargetZombieID = target.DataID;

        plant.mTargetX = Mathf.FloorToInt(target.mPosX);
        plant.mTargetY = Mathf.FloorToInt(target.mPosY);
        plant.mState = PlantState.SquashLook;
        plant.mStateCountdown = 0;
    }
}
