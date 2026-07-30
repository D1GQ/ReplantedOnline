using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Potatomine)]
internal sealed class PotatomineNetworkComponent : PlantSpecialNetworkComponent
{
    private enum PotatomineRpcs : byte
    {
        Wakeup
    }

    internal bool HasExploded;
    private bool _isWakingup = true;
    internal sealed override void OnUpdate(Plant plant)
    {
        if (plant.mState == PlantState.Notready)
        {
            UpdateSleeping(plant);
        }

        UpdateHealthSync();
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
                    SendWakeupRpc();
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

    private void SendWakeupRpc()
    {
        SendNetworkComponentRpc(PotatomineRpcs.Wakeup);
    }

    [RpcHandler(PotatomineRpcs.Wakeup)]
    private void HandleWakeupRpc()
    {
        _isWakingup = false;
        Net.Plant?.mStateCountdown = 0;
    }

    internal void ExplodeSynced()
    {
        if (!HasExploded)
        {
            HasExploded = true;
            SendDoSpecialRpc();
            DoSpecial();
            Net.DespawnAndDestroyWhenDeadOrNull(true);
        }
    }

    protected override void DoSpecial()
    {
        HasExploded = true;
        base.DoSpecial();
    }
}
