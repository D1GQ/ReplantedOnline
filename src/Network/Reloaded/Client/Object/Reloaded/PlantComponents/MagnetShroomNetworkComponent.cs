using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Reloaded.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Reloaded.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Magnetshroom)]
internal sealed class MagnetShroomNetworkComponent : PlantNetworkComponent
{
    internal sealed override void Update()
    {
        if (!Net.AmOwner)
        {
            if (Net.Target != null)
            {
                Net.Plant?.MagnetShroomAttactItemOriginal(Net.Target);
                Net.Target = null;
            }
        }
    }
}
