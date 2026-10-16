using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Cactus)]
internal sealed class CactusNetworkComponent : PlantNetworkComponent
{
    private bool _isHigh;
    internal sealed override void OnUpdate(Plant plant)
    {
        if (Net.AmOwner)
        {
            if (plant.mState is PlantState.CactusLow or PlantState.CactusLowering)
            {
                if (_isHigh)
                {
                    RpcSetLow();
                }
            }
            else if (plant.mState is PlantState.CactusHigh or PlantState.CactusRising)
            {
                if (!_isHigh)
                {
                    RpcSetHigh();
                }
            }
        }
        else
        {
            if (_isHigh)
            {
                plant.mState = PlantState.CactusHigh;
            }
            else
            {
                plant.mState = PlantState.CactusLow;
            }
        }

        UpdateHealthSync(plant);
    }

    [MethodRpc(LocalExecution.Before)]
    private void RpcSetHigh()
    {
        _isHigh = true;
    }

    [MethodRpc(LocalExecution.Before)]
    private void RpcSetLow()
    {
        _isHigh = false;
    }
}
