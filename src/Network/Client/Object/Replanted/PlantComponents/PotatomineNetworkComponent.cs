using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

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
        if (PlantNetworked._Plant.mState == PlantState.Notready)
        {
            UpdateSleeping();
        }

        UpdateHealthSync();
    }

    private void UpdateSleeping()
    {
        if (PlantNetworked.AmOwner)
        {
            if (_isWakingup)
            {
                if (PlantNetworked._Plant.mStateCountdown < 5)
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
                PlantNetworked._Plant.mStateCountdown = int.MaxValue;
            }
        }
    }

    internal void SendWakeupRpc()
    {
        SendNetworkComponentRpc(PotatomineRpcs.Wakeup);
    }

    [RpcHandler(PotatomineRpcs.Wakeup)]
    private void HandleWakeupRpc()
    {
        _isWakingup = false;
        PlantNetworked._Plant.mStateCountdown = 0;
    }

    internal void ExplodeSynced()
    {
        HasExploded = true;
        SendDoSpecialRpc();
        DoSpecial();
    }
}
