using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Sunshroom)]
internal sealed class SunshroomNetworkComponent : PlantNetworkComponent
{
    private enum SunshroomRpcs : byte
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
                SendGrowRpc();
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

    internal void SendGrowRpc()
    {
        SendNetworkComponentRpc(SunshroomRpcs.Grow);
    }

    [RpcHandler(SunshroomRpcs.Grow)]
    private void HandleGrowRpc()
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
