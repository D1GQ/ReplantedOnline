using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Cactus)]
internal sealed class CactusNetworkComponent : PlantNetworkComponent
{
    private enum CactusRpcs : byte
    {
        High,
        Lowering
    }

    private bool _isHigh;
    internal sealed override void OnUpdate(Plant plant)
    {
        if (Net.AmOwner)
        {
            if (plant.mState is PlantState.CactusLow or PlantState.CactusLowering)
            {
                if (_isHigh)
                {
                    _isHigh = false;
                    SendLoweringRpc();
                }
            }
            else if (plant.mState is PlantState.CactusHigh or PlantState.CactusRising)
            {
                if (!_isHigh)
                {
                    _isHigh = true;
                    SendHighRpc();
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

    private void SendHighRpc()
    {
        SendNetworkComponentRpc(CactusRpcs.High);
    }

    [RpcHandler(CactusRpcs.High)]
    private void HandleHighRpc()
    {
        _isHigh = true;
    }

    private void SendLoweringRpc()
    {
        SendNetworkComponentRpc(CactusRpcs.Lowering);
    }

    [RpcHandler(CactusRpcs.Lowering)]
    private void HandleLoweringRpc()
    {
        _isHigh = false;
    }
}
