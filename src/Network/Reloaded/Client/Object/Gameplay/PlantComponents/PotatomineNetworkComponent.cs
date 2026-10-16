using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Potatomine)]
internal sealed class PotatomineNetworkComponent : PlantSpecialNetworkComponent
{
    internal bool HasExploded;
    private bool _isWakingup = true;
    internal sealed override void OnUpdate(Plant plant)
    {
        if (plant.mState == PlantState.Notready)
        {
            UpdateSleeping(plant);
        }

        UpdateHealthSync(plant);
    }

    private void UpdateSleeping(Plant plant)
    {
        if (Net.AmOwner)
        {
            if (_isWakingup)
            {
                if (plant.mStateCountdown < 5)
                {
                    _isWakingup = false;
                    RpcWakeup();
                }
            }
        }
        else
        {
            if (_isWakingup)
            {
                plant.mStateCountdown = int.MaxValue;
            }
        }
    }

    [MethodRpc(LocalExecution.None)]
    private void RpcWakeup()
    {
        if (!_isWakingup)
        {
            return;
        }

        _isWakingup = false;

        Net.Plant?.mStateCountdown = 0;
    }

    internal void ExplodeSynced()
    {
        if (!HasExploded)
        {
            DoSpecial();
            RpcDoSpecial();
        }
    }

    protected override void DoSpecial()
    {
        HasExploded = true;
        base.DoSpecial();
    }
}
