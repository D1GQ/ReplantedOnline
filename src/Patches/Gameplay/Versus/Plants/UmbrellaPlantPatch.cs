using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class UmbrellaPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.DoSpecial))]
    [HarmonyPrefix]
    private static bool Plant_DoSpecial_Prefix(Plant __instance)
    {
        if (__instance.mSeedType != SeedType.Umbrella) return true;

        if (ReplantedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }
}
