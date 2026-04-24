using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;

namespace ReplantedOnline.Network.Client.Object.Replanted.Components;

/// <inheritdoc/>
internal class PlantSpecialNetworkComponent : PlantNetworkComponent
{
    private enum PlantSpecialRpcs : byte
    {
        DoSpecial = 255
    }

    internal override void Update()
    {
        if (PlantNetworked.AmOwner)
        {
            if (!PlantNetworked._Plant.mIsAsleep &&
                PlantNetworked._Plant.mDoSpecialCountdown < 5)
            {
                SendDoSpecialRpc();
            }
        }
        else
        {
            PlantNetworked._Plant.mDoSpecialCountdown = int.MaxValue;
        }
    }

    internal void SendDoSpecialRpc()
    {
        if (PlantNetworked.State is not PlantState.DoingSpecial)
        {
            PlantNetworked.State = PlantState.DoingSpecial;
            SendNetworkComponentRpc(PlantSpecialRpcs.DoSpecial);
        }
    }

    [RpcHandler(PlantSpecialRpcs.DoSpecial)]
    internal void HandleDoSpecialRpc()
    {
        if (PlantNetworked.State is not PlantState.DoingSpecial)
        {
            PlantNetworked.State = PlantState.DoingSpecial;
            DoSpecial();
        }
    }

    protected virtual void DoSpecial()
    {
        PlantNetworked.Dead = true;
        PlantNetworked._Plant.DoSpecial();
    }
}