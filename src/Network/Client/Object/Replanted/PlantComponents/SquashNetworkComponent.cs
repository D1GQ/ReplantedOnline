using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal sealed class SquashNetworkComponent : PlantNetworkComponent
{
    private enum SquashRpcs : byte
    {
        SquashTarget
    }

    internal void SendSquashTargetRpc(Zombie target)
    {
        if (PlantNetworked.State is not PlantState.DoingSpecial)
        {
            PlantNetworked.State = PlantState.DoingSpecial;
            SendNetworkComponentRpc(SquashRpcs.SquashTarget, target);
        }
    }

    [RpcHandler(SquashRpcs.SquashTarget)]
    internal void HandleSquashTargetRpc(Zombie target)
    {
        if (PlantNetworked.State is not PlantState.DoingSpecial)
        {
            PlantNetworked.State = PlantState.DoingSpecial;
            PlantNetworked._Plant.mTargetZombieID = target.DataID;
            PlantNetworked._Plant.mTargetX = Mathf.FloorToInt(target.mPosX);
            PlantNetworked._Plant.mTargetY = Mathf.FloorToInt(target.mPosY);
            PlantNetworked._Plant.mState = PlantState.SquashLook;
        }
    }
}
