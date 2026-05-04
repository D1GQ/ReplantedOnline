using HarmonyLib;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppSource.DataModels;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class SeedChooserPatch
{
    [HarmonyPatch(typeof(SeedChooserDataModel), nameof(SeedChooserDataModel.UpdateEntries))]
    [HarmonyPrefix]
    private static bool SeedChooserEntryModel_UpdateEntries_Prefix(SeedChooserDataModel __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Add all the seeds that are in the seed chooser screen, instead of just the ones that are in the seed chooser data model
            __instance.m_entriesModel.Clear();
            for (int i = 0; i < Instances.GameplayActivity.SeedChooserScreen.mChosenSeeds.Count; i++)
            {
                var plantChosenSeed = Instances.GameplayActivity.SeedChooserScreen.mChosenSeeds[i];
                if (SeedPacketDefinitions.HideInChooserSeedTypes.Contains(plantChosenSeed.mSeedType)) continue;
                PlantDefinition plantDefinition = Instances.IDataService.GetPlantDefinition(plantChosenSeed.mSeedType);
                if (plantDefinition == null || plantDefinition.VersusBaseRefreshTime == 0) continue;
                SeedChooserEntryModel entry = new(plantDefinition, plantChosenSeed, Instances.GameplayActivity.SeedChooserScreen, __instance, false, i);
                __instance.m_entriesModel.Add(i.ToString(), entry);
            }

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(SeedChooserDataModel), nameof(SeedChooserDataModel.UpdateZombieEntries))]
    [HarmonyPrefix]
    private static bool SeedChooserEntryModel_UpdateZombieEntries_Prefix(SeedChooserDataModel __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Add all the seeds that are in the seed chooser screen, instead of just the ones that are in the seed chooser data model
            __instance.m_zombieEntriesModel.Clear();
            for (int i = 0; i < Instances.GameplayActivity.SeedChooserScreen.mChosenZombies.Count; i++)
            {
                var zombieChosenSeed = Instances.GameplayActivity.SeedChooserScreen.mChosenZombies[i];
                if (SeedPacketDefinitions.HideInChooserSeedTypes.Contains(zombieChosenSeed.mSeedType)) continue;
                PlantDefinition plantDefinition = Instances.IDataService.GetPlantDefinition(zombieChosenSeed.mSeedType);
                if (plantDefinition == null || plantDefinition.VersusBaseRefreshTime == 0) continue;
                SeedChooserEntryModel entry = new(plantDefinition, zombieChosenSeed, Instances.GameplayActivity.SeedChooserScreen, __instance, false, i);
                __instance.m_zombieEntriesModel.Add(i.ToString(), entry);
            }

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.SeedNotAllowedToPick))]
    [HarmonyPostfix]
    private static void SeedChooserScreen_SeedNotAllowedToPick_Postfix(SeedType theSeedType, ref bool __result)
    {
        if (ReplantedLobby.AmInLobby())
        {
            __result = false;
        }
    }
}
