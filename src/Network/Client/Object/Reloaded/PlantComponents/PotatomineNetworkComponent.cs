using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.PlantComponents;

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
    internal override void Update()
    {
        if (Net.Plant.mState == PlantState.Notready)
        {
            UpdateSleeping();
        }

        UpdateHealthSync();
    }

    private void UpdateSleeping()
    {
        if (Net.AmOwner)
        {
            if (_isWakingup)
            {
                if (Net.Plant.mStateCountdown < 5)
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
                Net.Plant.mStateCountdown = int.MaxValue;
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
        Net.Plant.mStateCountdown = 0;
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
