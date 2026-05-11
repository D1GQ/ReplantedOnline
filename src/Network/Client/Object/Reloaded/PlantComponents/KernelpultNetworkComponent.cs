using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Client.Object.Reloaded.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Kernelpult)]
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
            Net._Plant.mController.AssignRenderGroupToPrefixOriginal(Animations.KERNELPULT_KERNAL_OBJECT, 0);
            Net._Plant.mController.AssignRenderGroupToPrefixOriginal(Animations.KERNELPULT_BUTTER_OBJECT, -1);
        }
        else
        {
            Net._Plant.mController.AssignRenderGroupToPrefixOriginal(Animations.KERNELPULT_BUTTER_OBJECT, 0);
            Net._Plant.mController.AssignRenderGroupToPrefixOriginal(Animations.KERNELPULT_KERNAL_OBJECT, -1);
        }
    }

    internal PlantWeapon GetWeapon()
    {
        return _plantWeapon;
    }
}
