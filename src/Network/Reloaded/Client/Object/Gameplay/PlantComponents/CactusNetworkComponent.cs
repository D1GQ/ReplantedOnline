using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Cactus)]
internal sealed class CactusNetworkComponent : PlantNetworkComponent
{
    private enum CactusMsgs : byte
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
                    SendLoweringMessage();
                }
            }
            else if (plant.mState is PlantState.CactusHigh or PlantState.CactusRising)
            {
                if (!_isHigh)
                {
                    _isHigh = true;
                    SendHighMessage();
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

    private void SendHighMessage()
    {
        SendMessage(CactusMsgs.High);
    }

    [ObjectMessageHandler(CactusMsgs.High)]
    private void HandleHighMessage()
    {
        _isHigh = true;
    }

    private void SendLoweringMessage()
    {
        SendMessage(CactusMsgs.Lowering);
    }

    [ObjectMessageHandler(CactusMsgs.Lowering)]
    private void HandleLoweringMessage()
    {
        _isHigh = false;
    }
}
