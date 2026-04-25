using HarmonyLib;
using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class PlantPatch
{
    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.DoSpecial))]
    internal static void DoSpecialOriginal(this Plant __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}
