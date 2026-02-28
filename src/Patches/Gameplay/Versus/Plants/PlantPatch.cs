using HarmonyLib;
using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class PlantPatch
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.Fire))]
    internal static void FireOriginal(this Plant __instance, Zombie theTargetZombie, int theRow, PlantWeapon thePlantWeapon)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}
