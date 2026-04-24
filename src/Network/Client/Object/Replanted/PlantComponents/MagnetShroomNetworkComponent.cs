using ReplantedOnline.Patches.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal sealed class MagnetShroomNetworkComponent : PlantNetworkComponent
{
    internal override void Update()
    {
        if (!PlantNetworked.AmOwner)
        {
            if (PlantNetworked.Target != null)
            {
                PlantNetworked._Plant.MagnetShroomAttactItemOriginal(PlantNetworked.Target);
                PlantNetworked.Target = null;
            }
        }
    }
}
