using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Attributes;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal sealed class ChomperNetworkComponent : PlantNetworkComponent
{
    private enum ChomperRpcs : byte
    {
        ChomperState
    }

    private string _chomperState;
    internal override void Update()
    {
        if (PlantNetworked.AmOwner)
        {
            string plantStateStr = PlantNetworked._Plant.mState.ToString();

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
                if (PlantNetworked._Plant.mState != state)
                {
                    if (state == PlantState.ChomperBiting)
                    {
                        PlantNetworked._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_BITE.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_BITE.Fps, ReanimLoopType.PlayOnce);
                        _chomperState = PlantState.ChomperBitingMissed.ToString();
                    }
                    else if (state == PlantState.ChomperDigesting)
                    {
                        PlantNetworked._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_CHEW.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_CHEW.Fps, ReanimLoopType.Loop);
                    }
                    else if (state == PlantState.ChomperSwallowing)
                    {
                        PlantNetworked._Plant.mState = PlantState.ChomperDigesting;
                        PlantNetworked._Plant.mStateCountdown = 0;
                        return;
                    }

                    PlantNetworked._Plant.mState = state;
                    PlantNetworked._Plant.mStateCountdown = int.MaxValue;
                }
                else if (state == PlantState.Ready)
                {
                    if (!PlantNetworked._Plant.mController.IsAnimationPlaying(Animations.CHOMPER_IDLE.Anim))
                    {
                        PlantNetworked._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_IDLE.Anim, CharacterAnimationTrack.Body, Animations.CHOMPER_IDLE.Fps, ReanimLoopType.Loop);
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
