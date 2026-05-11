using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Client.Object.Reloaded.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Umbrella)]
internal sealed class UmbrellaNetworkComponent : PlantSpecialNetworkComponent
{
    private bool _triggerd;
    internal override void Update()
    {
        if (Net.AmOwner)
        {
            if (Net._Plant.mState == PlantState.UmbrellaTriggered && !_triggerd)
            {
                _triggerd = true;
                SendDoSpecialRpc();
            }
            else if (Net._Plant.mState != PlantState.UmbrellaTriggered)
            {
                _triggerd = false;
            }
        }

        UpdateHealthSync();
    }

    protected override void DoSpecial()
    {
        Net._Plant.DoSpecialOriginal();
    }
}
