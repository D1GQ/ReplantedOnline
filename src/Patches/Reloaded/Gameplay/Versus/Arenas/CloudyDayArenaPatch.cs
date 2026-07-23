using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Modules.Reloaded.Versus.Arenas;
using ReplantedOnline.Network.Reloaded.Client;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Arenas;

[HarmonyPatch]
internal static class CloudyDayArenaPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.PlantInitialize))]
    [HarmonyPostfix]
    private static void Plant_PlantInitialize_Postfix(Plant __instance)
    {
        if (VersusState.ArenaSynced != ArenaType.CloudyDay)
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
    private static bool Plant_GetCost_Prefix(SeedType theSeedType, ref int __result)
    {
        if (VersusState.ArenaSynced != ArenaType.CloudyDay)
            return true;

        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.VersusTimeSynced > 30f && CloudyDayArena.IsRaining)
            {
                var definition = Instances.IDataService.GetPlantDefinition(theSeedType);
                if (definition != null)
                {
                    __result = CloudyDayArena.GetCostReduction(theSeedType, definition.m_versusCost);
                    return false;
                }
            }
        }

        return true;
    }


    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.SeedNotRecommendedToPick))]
    [HarmonyPrefix]
    private static bool SeedChooserScreen_Update_Prefix(SeedType theSeedType, ref RecommentedFlags __result)
    {
        if (VersusState.ArenaSynced != ArenaType.CloudyDay)
            return true;

        if (ReloadedLobby.AmInLobby())
        {
            // Set to recommended
            if (Plant.IsNocturnal(theSeedType))
            {
                __result = RecommentedFlags.None;
                return false;
            }

            // Set to not recommended
            if (theSeedType == SeedType.InstantCoffee)
            {
                __result = RecommentedFlags.NotRecommentedNocturnal;
                return false;
            }
        }

        return true;
    }
}
