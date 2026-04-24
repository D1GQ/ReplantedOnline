using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal sealed class ChomperNetworkComponent : PlantNetworkComponent
{
    internal override void Update()
    {
        if (PlantNetworked.AmOwner)
        {
            string plantStateStr = PlantNetworked._Plant.mState.ToString();

            if (PlantNetworked.State?.ToString() != plantStateStr)
            {
                PlantNetworked.State = plantStateStr;
                PlantNetworked.SendSetStateRpc(plantStateStr);
            }
        }
        else
        {
            if (PlantNetworked.State is string stateStr)
            {
                if (Enum.TryParse(stateStr, out PlantState state))
                {
                    if (PlantNetworked._Plant.mState != state)
                    {
                        if (state == PlantState.ChomperBiting)
                        {
                            PlantNetworked._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_BITE, CharacterAnimationTrack.Body, 30f, ReanimLoopType.PlayOnce);
                            PlantNetworked.State = PlantState.ChomperBitingMissed.ToString();
                        }
                        else if (state == PlantState.ChomperDigesting)
                        {
                            PlantNetworked._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_CHEW, CharacterAnimationTrack.Body, 15f, ReanimLoopType.Loop);
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
                        if (!PlantNetworked._Plant.mController.IsAnimationPlaying(Animations.CHOMPER_IDLE))
                        {
                            PlantNetworked._Plant.mController.PlayAnimationOnTrack(Animations.CHOMPER_IDLE, CharacterAnimationTrack.Body, 10.26f, ReanimLoopType.Loop);
                        }
                    }
                }
            }
        }

        UpdateHealthSync();
    }
}
