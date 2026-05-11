using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Chomper)]
internal sealed class ChomperNetworkComponent : PlantNetworkComponent
{
    private enum ChomperRpcs : byte
    {
        ChomperState
    }

    private string _chomperState;
    internal override void Update()
    {
        if (Net.AmOwner)
        {
            string plantStateStr = Net._Plant.mState.ToString();

            if (_chomperState != plantStateStr)
            {
                _chomperState = plantStateStr;
                SendChomperStateRpc(plantStateStr);
            }
        }
        else
        {
            if (Enum.TryParse(_chomperState, out PlantState state))
            {
                if (Net._Plant.mState != state)
                {
                    if (state == PlantState.ChomperBiting)
                    {
                        Net._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_BITE.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_BITE.Fps, ReanimLoopType.PlayOnce);
                        _chomperState = PlantState.ChomperBitingMissed.ToString();
                    }
                    else if (state == PlantState.ChomperDigesting)
                    {
                        Net._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_CHEW.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_CHEW.Fps, ReanimLoopType.Loop);
                    }
                    else if (state == PlantState.ChomperSwallowing)
                    {
                        Net._Plant.mState = PlantState.ChomperDigesting;
                        Net._Plant.mStateCountdown = 0;
                        return;
                    }

                    Net._Plant.mState = state;
                    Net._Plant.mStateCountdown = int.MaxValue;
                }
                else if (state == PlantState.Ready)
                {
                    if (!Net._Plant.mController.IsAnimationPlaying(Animations.CHOMPER_IDLE.Anim))
                    {
                        Net._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_IDLE.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_IDLE.Fps, ReanimLoopType.Loop);
                    }
                }
            }
        }

        UpdateHealthSync();
    }

    private void SendChomperStateRpc(string state)
    {
        SendNetworkComponentRpc(ChomperRpcs.ChomperState, state);
    }

    [RpcHandler(ChomperRpcs.ChomperState)]
    private void HandleChomperStateRpc(string state)
    {
        _chomperState = state;
    }
}
