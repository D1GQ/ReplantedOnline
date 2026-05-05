using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
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
        if (PlantNetworked.AmOwner)
        {
            if (PlantNetworked._Plant.mState is PlantState.CactusLow or PlantState.CactusLowering)
            {
                if (_isHigh)
                {
                    _isHigh = false;
                    SendLoweringRpc();
                }
            }
            else if (PlantNetworked._Plant.mState is PlantState.CactusHigh or PlantState.CactusRising)
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
                PlantNetworked._Plant.mState = PlantState.CactusHigh;
            }
            else
            {
                PlantNetworked._Plant.mState = PlantState.CactusLow;
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
