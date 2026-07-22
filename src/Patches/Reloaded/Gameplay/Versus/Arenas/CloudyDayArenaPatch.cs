using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Modules.Reloaded.Versus.Arenas;
using ReplantedOnline.Network.Reloaded.Client;
using UnityEngine;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Arenas;

[HarmonyPatch]
internal static class CloudyDayArenaPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.PlantInitialize))]
    [HarmonyPostfix]
    private static void Plant_PlantInitialize_Postfix(Plant __instance)
    {
        if (VersusState.Arena != ArenaTypes.CloudyDay)
            return;

        if (ReloadedLobby.AmInLobby())
        {
            if (Plant.IsNocturnal(__instance.mSeedType))
            {
                __instance.SetSleeping(!CloudyDayArena.IsRaining);
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.GetCost))]
    [HarmonyPrefix]
    private static bool Plant_GetCost_Prefix(GameplayActivity gLawnApp, SeedType theSeedType, ref int __result)
    {
        if (VersusState.Arena != ArenaTypes.CloudyDay)
            return true;

        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.VersusTimeSynced > 30f && CloudyDayArena.IsRaining)
            {
                var definition = Instances.IDataService.GetPlantDefinition(theSeedType);
                if (definition != null)
                {
                    __result = (int)(Math.Round(definition.m_versusCost * 0.5f / 5, MidpointRounding.AwayFromZero) * 5);
                    return false;
                }
            }
        }

        return true;
    }

    internal static void ApplyRefreshTimeReduction(ref int refreshTime)
    {
        if (VersusState.Arena != ArenaTypes.CloudyDay)
            return;

        if (VersusState.VersusTimeSynced <= 30f)
            return;

        if (!CloudyDayArena.IsRaining)
            return;

        refreshTime = Mathf.Min(100, (int)(Math.Pow(refreshTime, 0.7f) * 1.5f));
    }
}
