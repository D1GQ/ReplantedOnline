using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class UmbrellaPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.DoSpecial))]
    [HarmonyPrefix]
    private static bool Plant_DoSpecial_Prefix(Plant __instance)
    {
        if (__instance.mSeedType != SeedType.Umbrella) return true;

        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }
}
