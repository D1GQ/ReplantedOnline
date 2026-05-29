using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.PlantComponents;

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
    internal override void Update()
    {
        if (Net.AmOwner)
        {
            if (Net.Plant.mState is PlantState.CactusLow or PlantState.CactusLowering)
            {
                if (_isHigh)
                {
                    _isHigh = false;
                    SendLoweringRpc();
                }
            }
            else if (Net.Plant.mState is PlantState.CactusHigh or PlantState.CactusRising)
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
                Net.Plant.mState = PlantState.CactusHigh;
            }
            else
            {
                Net.Plant.mState = PlantState.CactusLow;
            }
        }

        UpdateHealthSync();
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
