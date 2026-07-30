using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Chomper)]
internal sealed class ChomperNetworkComponent : PlantNetworkComponent
{
    private enum ChomperRpcs : byte
    {
        ChomperState
    }

    private PlantState _chomperState = PlantState.Notready;
    internal sealed override void OnUpdate(Plant plant)
    {
        if (Net.AmOwner)
        {
            var plantState = plant.mState;

            if (_chomperState != plantState)
            {
                _chomperState = plantState;
                bool hasTarget = plant.FindTargetZombieOriginal(plant.mRow, PlantWeapon.Primary) != null;
                SendChomperStateRpc(plantState, hasTarget);
            }
        }
        else
        {
            if (plant.mState != _chomperState && _chomperState != PlantState.Notready)
            {
                if (_chomperState == PlantState.ChomperBiting)
                {
                    plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_BITE.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_BITE.Fps, ReanimLoopType.PlayOnce);
                    _chomperState = PlantState.ChomperBitingMissed;
                }
                else if (_chomperState == PlantState.ChomperDigesting)
                {
                    plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_CHEW.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_CHEW.Fps, ReanimLoopType.Loop);
                }
                else if (_chomperState == PlantState.ChomperSwallowing)
                {
                    plant.mState = PlantState.ChomperDigesting;
                    plant.mStateCountdown = 0;
                    return;
                }

                plant.mState = _chomperState;
                plant.mStateCountdown = int.MaxValue;
            }
            else if (_chomperState == PlantState.Ready)
            {
                if (!plant.mController.IsAnimationPlaying(Animations.CHOMPER_IDLE.Anim))
                {
                    plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_IDLE.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_IDLE.Fps, ReanimLoopType.Loop);
                }
            }
        }

        UpdateHealthSync();
    }

    private void SendChomperStateRpc(PlantState plantState, bool hasTarget)
    {
        SendNetworkComponentRpc(ChomperRpcs.ChomperState, plantState, hasTarget);
    }

    [RpcHandler(ChomperRpcs.ChomperState)]
    private void HandleChomperStateRpc(PlantState plantState, bool hasTarget)
    {
        _chomperState = plantState;

        switch (_chomperState)
        {
            case PlantState.ChomperBitingGotOne:
                Instances.GameplayActivity.m_audioService.PlayFoley(Il2CppReloaded.Services.FoleyType.BigChomp);
                break;
            case PlantState.ChomperBitingMissed:
                Instances.GameplayActivity.m_audioService.PlayFoley(Il2CppReloaded.Services.FoleyType.BigChomp);
                if (hasTarget)
                {
                    Instances.GameplayActivity.m_audioService.PlayFoley(Il2CppReloaded.Services.FoleyType.Splat);
                }
                break;
        }
    }
}
