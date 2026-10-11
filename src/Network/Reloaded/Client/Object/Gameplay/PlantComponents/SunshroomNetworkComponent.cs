using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Sunshroom)]
internal sealed class SunshroomNetworkComponent : PlantNetworkComponent
{
    private enum SunshroomMsgs : byte
    {
        Grow
    }

    private bool _hasGrown;

    internal override void OnUpdate(Plant plant)
    {
        if (Net.AmOwner)
        {
            if (plant.mState != PlantState.SunshroomSmall && !_hasGrown)
            {
                _hasGrown = true;
                SendGrowMessage();
            }
        }
        else
        {
            if (plant.mState == PlantState.SunshroomSmall && !_hasGrown)
            {
                plant.mStateCountdown = int.MaxValue;
            }
        }

        UpdateHealthSync(plant);
    }

    internal void SendGrowMessage()
    {
        SendMessage(SunshroomMsgs.Grow);
    }

    [ObjectMessageHandler(SunshroomMsgs.Grow)]
    private void HandleGrowMessage()
    {
        var plant = Net.Plant;
        if (plant == null)
            return;

        if (_hasGrown)
            return;

        _hasGrown = true;
        plant.mState = PlantState.SunshroomSmall;
        plant.mStateCountdown = 1;
    }
}
