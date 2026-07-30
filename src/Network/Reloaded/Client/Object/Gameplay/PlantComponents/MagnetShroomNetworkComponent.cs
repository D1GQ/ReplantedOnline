using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Magnetshroom)]
internal sealed class MagnetShroomNetworkComponent : PlantNetworkComponent
{
    internal sealed override void OnUpdate(Plant plant)
    {
        if (!Net.AmOwner)
        {
            if (Net.Target != null)
            {
                plant.MagnetShroomAttactItemOriginal(Net.Target);
                Net.Target = null;
            }
        }
    }
}
