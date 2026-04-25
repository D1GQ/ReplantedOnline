using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Patches.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal sealed class KernelpultNetworkComponent : PlantNetworkComponent
{
    private PlantWeapon _plantWeapon;
    internal void RandomizeWeapon()
    {
        var rand = Common.Rand(100);
        if (rand > 25)
        {
            _plantWeapon = PlantWeapon.Primary;
        }
        else
        {
            _plantWeapon = PlantWeapon.Secondary;
        }

        SetVisuals(_plantWeapon);
    }

    internal void SetVisuals(PlantWeapon plantWeapon)
    {
        if (plantWeapon == PlantWeapon.Primary)
        {
            PlantNetworked._Plant.mController.AssignRenderGroupToPrefixOriginal(Animations.KERNELPULT_KERNAL_OBJECT, 0);
            PlantNetworked._Plant.mController.AssignRenderGroupToPrefixOriginal(Animations.KERNELPULT_BUTTER_OBJECT, -1);
        }
        else
        {
            PlantNetworked._Plant.mController.AssignRenderGroupToPrefixOriginal(Animations.KERNELPULT_BUTTER_OBJECT, 0);
            PlantNetworked._Plant.mController.AssignRenderGroupToPrefixOriginal(Animations.KERNELPULT_KERNAL_OBJECT, -1);
        }
    }

    internal PlantWeapon GetWeapon()
    {
        return _plantWeapon;
    }
}
