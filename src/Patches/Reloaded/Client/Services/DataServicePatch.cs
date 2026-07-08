using HarmonyLib;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Network.Reloaded.Client;

namespace ReplantedOnline.Patches.Reloaded.Client.Services;

[Harmony]
internal class DataServicePatch
{
    [HarmonyPatch(typeof(DataService), nameof(DataService.GetPlantDefinition))]
    [HarmonyPrefix]
    private static bool DataService_GetPlantDefinition_Prefix(DataService __instance, SeedType seedType, ref PlantDefinition __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Bypass hardcoded index range check to allow CustomSeedType!
            if (__instance.m_plantDataLoader.TryGetValue(seedType, out var definition))
            {
                __result = definition;
            }

            return false;
        }

        return true;
    }
}
