using HarmonyLib;
using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class PlantPatch
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    internal static Zombie? FindTargetZombieOriginal(this Plant __instance, int theRow, PlantWeapon thePlantWeapon)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.DoSpecial))]
    internal static void DoSpecialOriginal(this Plant __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}
