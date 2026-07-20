using HarmonyLib;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppSource.DataModels;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus;

[HarmonyPatch]
internal static class SeedChooserPatch
{
    [HarmonyPatch(typeof(SeedChooserDataModel), nameof(SeedChooserDataModel.UpdateEntries))]
    [HarmonyPrefix]
    private static bool SeedChooserEntryModel_UpdateEntries_Prefix(SeedChooserDataModel __instance)
    {
        if (ReloadedLobby.AmInLobby())
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
        if (ReloadedLobby.AmInLobby())
        {
            AddCustomZombiesToChosen();

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

    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.Update))]
    [HarmonyPostfix]
    private static void SeedChooserScreen_Update_Postfix(SeedChooserScreen __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Update seed packet states in chooser 
            foreach (var chosenPlant in __instance.mChosenSeeds)
            {
#if DEBUG
                if (VersusState.Arena == Enums.Versus.ArenaTypes.Debug)
                {
                    if (chosenPlant.mSeedState == ChosenSeedState.SeedPacketHidden)
                    {
                        chosenPlant.mSeedState = ChosenSeedState.SeedInChooser;
                    }
                    chosenPlant.mNotSuggested = chosenPlant.mSeedState == ChosenSeedState.SeedInBank;
                    continue;
                }
#endif

                var flags = __instance.SeedNotRecommendedToPick(chosenPlant.mSeedType);
                var hasNocturnalFlag = flags.HasFlag(RecommentedFlags.NotRecommentedNocturnal);
                var isAllowed = ICharacterConfig.IsAllowedInArena(chosenPlant.mSeedType, VersusState.Arena) &&
                    (flags.HasFlag(RecommentedFlags.None) || hasNocturnalFlag);

                bool notSuggested = !isAllowed ||
                                    hasNocturnalFlag;

                if (!isAllowed)
                {
                    chosenPlant.mSeedState = ChosenSeedState.SeedPacketHidden;
                }
                else if (chosenPlant.mSeedState == ChosenSeedState.SeedPacketHidden)
                {
                    chosenPlant.mSeedState = ChosenSeedState.SeedInChooser;
                }

                notSuggested = notSuggested ||
                               !flags.HasFlag(RecommentedFlags.None) ||
                               chosenPlant.mSeedState == ChosenSeedState.SeedInBank;

                chosenPlant.mNotSuggested = notSuggested;
            }

            foreach (var chosenZombie in __instance.mChosenZombies)
            {
#if DEBUG
                if (VersusState.Arena == Enums.Versus.ArenaTypes.Debug)
                {
                    if (chosenZombie.mSeedState == ChosenSeedState.SeedPacketHidden)
                    {
                        chosenZombie.mSeedState = ChosenSeedState.SeedInChooser;
                    }
                    chosenZombie.mNotSuggested = chosenZombie.mSeedState == ChosenSeedState.SeedInBank;
                    continue;
                }
#endif

                var isAllowed = ICharacterConfig.IsAllowedInArena(chosenZombie.mSeedType, VersusState.Arena);

                bool notSuggested = !isAllowed;

                if (!isAllowed)
                {
                    chosenZombie.mSeedState = ChosenSeedState.SeedPacketHidden;
                }
                else if (chosenZombie.mSeedState == ChosenSeedState.SeedPacketHidden)
                {
                    chosenZombie.mSeedState = ChosenSeedState.SeedInChooser;
                }

                notSuggested = notSuggested ||
                               chosenZombie.mSeedState == ChosenSeedState.SeedInBank;

                chosenZombie.mNotSuggested = notSuggested;
            }
        }
    }

    private static void AddCustomZombiesToChosen()
    {
        var customSeedsToAdd = CustomSeedType.CustomSeedTypes.ToList();
        foreach (var chosenZombies in Instances.GameplayActivity.SeedChooserScreen.mChosenZombies)
        {
            customSeedsToAdd.Remove(chosenZombies.mSeedType);
        }

        foreach (var customSeedType in customSeedsToAdd)
        {
            if (!Challenge.IsZombieSeedType(customSeedType)) continue;
            if (customSeedType == CustomSeedType.Invalid) continue;

            ChosenSeed chosenSeed = new()
            {
                mSeedState = ChosenSeedState.SeedInChooser,
                mImitaterType = SeedType.None,
                mSeedType = customSeedType
            };
            Instances.GameplayActivity.SeedChooserScreen.mChosenZombies.Add(chosenSeed);
        }

        RepositionAllZombieSeeds();
    }

    private static readonly ChosenSeed HiddenChosenSeed = new()
    {
        mSeedType = SeedType.Gatlingpea, // Need to use unused valid seed type
        mIsImitater = true // For some reason this hides the seed in the chooser
    };

    static void RepositionAllZombieSeeds()
    {
        var screen = Instances.GameplayActivity.SeedChooserScreen;

        screen.mChosenZombies.Remove(HiddenChosenSeed);

        // Sort seeds by cost
        screen.mChosenZombies.Sort((Func<ChosenSeed, ChosenSeed, int>)((cz1, cz2) =>
        {
            bool isGravestone1 = cz1.mSeedType == SeedType.ZombieGravestone;
            bool isGravestone2 = cz2.mSeedType == SeedType.ZombieGravestone;

            if (isGravestone1 == isGravestone2)
            {
                var definition1 = Instances.IDataService.GetPlantDefinition(cz1.mSeedType);
                var definition2 = Instances.IDataService.GetPlantDefinition(cz2.mSeedType);
                return definition1.m_versusCost.CompareTo(definition2.m_versusCost);
            }

            return isGravestone1 ? -1 : 1;
        }));

        // Work around for hard coded index 20 being not suggested.
        screen.mChosenZombies.Insert(20, HiddenChosenSeed);

        var seeds = screen.mChosenZombies;
        bool has7Rows = screen.Has7Rows();

        for (int i = 0; i < seeds.Count; i++)
        {
            var seed = seeds[i];

            int row = i >> 3;
            int col = i & 7;

            if (!has7Rows)
                seed.mX = row * 0x49 + 0x80;
            else
                seed.mX = row * 0x46 + 0x7b;

            seed.mY = col * 0x35 + 0x16;

            seed.mStartX = seed.mX;
            seed.mStartY = seed.mY;
            seed.mEndX = seed.mX;
            seed.mEndY = seed.mY;
        }
    }

    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.SeedNotAllowedToPick))]
    [HarmonyPostfix]
    private static void SeedChooserScreen_SeedNotAllowedToPick_Postfix(ref bool __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.GetChosenSeedFromType))]
    [HarmonyPrefix]
    private static bool SeedChooserScreen_GetChosenSeedFromType_Prefix(SeedChooserScreen __instance, SeedType theSeedType, ref ChosenSeed? __result)
    {
        // Bypass hardcoded index range check to allow CustomSeedType!

        foreach (var chosenSeed in __instance.mChosenSeeds)
        {
            if (chosenSeed.mSeedType == theSeedType)
            {
                __result = chosenSeed;
                return false;
            }
        }

        foreach (var chosenZombie in __instance.mChosenZombies)
        {
            if (chosenZombie.mSeedType == theSeedType)
            {
                __result = chosenZombie;
                return false;
            }
        }

        __result = null;
        return false;
    }
}
