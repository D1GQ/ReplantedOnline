using ReplantedOnline.Attributes;

namespace ReplantedOnline.Network.Client.Object.Replanted.Components;

/// <inheritdoc/>
internal class PlantSpecialNetworkComponent : PlantNetworkComponent
{
    private enum PlantSpecialRpcs : byte
    {
        DoSpecial = 255
    }

    private bool _isDoingSpecial;
    internal override void Update()
    {
        if (PlantNetworked.AmOwner)
        {
            if (!PlantNetworked._Plant.mIsAsleep &&
                PlantNetworked._Plant.mDoSpecialCountdown < 5)
            {
                if (!_isDoingSpecial)
                {
                    SendDoSpecialRpc();
                    PlantNetworked.Dead = true;
                    PlantNetworked.DespawnAndDestroyWhenDeadOrNull();
                }
            }
        }
        else
        {
            PlantNetworked._Plant.mDoSpecialCountdown = int.MaxValue;
        }
    }

    internal void SendDoSpecialRpc()
    {
        if (!_isDoingSpecial)
        {
            _isDoingSpecial = true;
            SendNetworkComponentRpc(PlantSpecialRpcs.DoSpecial);
        }
    }

    [RpcHandler(PlantSpecialRpcs.DoSpecial)]
    internal void HandleDoSpecialRpc()
    {
        if (!_isDoingSpecial)
        {
            _isDoingSpecial = true;
            DoSpecial();
        }
    }

    protected virtual void DoSpecial()
    {
        PlantNetworked.Dead = true;
        PlantNetworked._Plant.DoSpecial();
    }
}