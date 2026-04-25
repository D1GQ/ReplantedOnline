using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Patches.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal sealed class UmbrellaNetworkComponent : PlantSpecialNetworkComponent
{
    private bool _triggerd;
    internal override void Update()
    {
        if (PlantNetworked.AmOwner)
        {
            if (PlantNetworked._Plant.mState == PlantState.UmbrellaTriggered && !_triggerd)
            {
                _triggerd = true;
                SendDoSpecialRpc();
            }
            else if (PlantNetworked._Plant.mState != PlantState.UmbrellaTriggered)
            {
                _triggerd = false;
            }
        }

        UpdateHealthSync();
    }

    protected override void DoSpecial()
    {
        PlantNetworked._Plant.DoSpecialOriginal();
    }
}
