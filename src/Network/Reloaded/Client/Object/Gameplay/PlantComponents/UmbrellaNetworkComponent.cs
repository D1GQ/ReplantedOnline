using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Umbrella)]
internal sealed class UmbrellaNetworkComponent : PlantSpecialNetworkComponent
{
    private bool _triggerd;
    internal sealed override void Update()
    {
        if (Net.AmOwner)
        {
            if (Net.Plant!.mState == PlantState.UmbrellaTriggered && !_triggerd)
            {
                _triggerd = true;
                SendDoSpecialRpc();
            }
            else if (Net.Plant.mState != PlantState.UmbrellaTriggered)
            {
                _triggerd = false;
            }
        }

        UpdateHealthSync();
    }

    protected sealed override void DoSpecial()
    {
        Net.Plant?.DoSpecialOriginal();
    }
}
