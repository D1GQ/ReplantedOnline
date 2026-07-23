using HarmonyLib;
using Il2Cpp;
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppSource.DataModels;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus;

[HarmonyPatch]
internal static class SeedChooserPatch
{
    private static readonly List<SeedChooserEntryModel> SeedChooserEntries = [];
    private static readonly List<SeedChooserEntryModel> SeedChooserZombieEntries = [];

    [HarmonyPatch(typeof(SeedChooserDataModel), nameof(SeedChooserDataModel.UpdateEntries))]
    [HarmonyPrefix]
    private static bool SeedChooserEntryModel_UpdateEntries_Prefix(SeedChooserDataModel __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Add all the seeds that are in the seed chooser screen, instead of just the ones that are in the seed chooser data model
            SeedChooserEntries.Clear();
            __instance.m_entriesModel.Clear();
            for (int i = 0; i < __instance.m_seedChooserScreen.mChosenSeeds.Count; i++)
            {
                var plantChosenSeed = __instance.m_seedChooserScreen.mChosenSeeds[i];
                if (plantChosenSeed.mSeedType == SeedType.None) continue;
                if (SeedPacketDefinitions.HideInChooserSeedTypes.Contains(plantChosenSeed.mSeedType)) continue;
                PlantDefinition plantDefinition = Instances.IDataService.GetPlantDefinition(plantChosenSeed.mSeedType);
                if (plantDefinition == null || plantDefinition.VersusBaseRefreshTime == 0) continue;
                SeedChooserEntryModel entry = new(plantDefinition, plantChosenSeed, __instance.m_seedChooserScreen, __instance, false, i);
                __instance.m_entriesModel.Add(i.ToString(), entry);
                SeedChooserEntries.Add(entry);
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
            SeedChooserZombieEntries.Clear();
            __instance.m_zombieEntriesModel.Clear();
            for (int i = 0; i < Instances.GameplayActivity.SeedChooserScreen.mChosenZombies.Count; i++)
            {
                var zombieChosenSeed = Instances.GameplayActivity.SeedChooserScreen.mChosenZombies[i];
                if (zombieChosenSeed.mSeedType == SeedType.None) continue;
                if (SeedPacketDefinitions.HideInChooserSeedTypes.Contains(zombieChosenSeed.mSeedType)) continue;
                PlantDefinition plantDefinition = Instances.IDataService.GetPlantDefinition(zombieChosenSeed.mSeedType);
                if (plantDefinition == null || plantDefinition.VersusBaseRefreshTime == 0) continue;
                SeedChooserEntryModel entry = new(plantDefinition, zombieChosenSeed, Instances.GameplayActivity.SeedChooserScreen, __instance, false, i);
                __instance.m_zombieEntriesModel.Add(i.ToString(), entry);
                SeedChooserZombieEntries.Add(entry);
            }

            return false;
        }

        return true;
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

    static void RepositionAllZombieSeeds()
    {
        var screen = Instances.GameplayActivity.SeedChooserScreen;

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

    private static readonly UnityTimer FlashRequiredTimer = new();
    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.Update))]
    [HarmonyPrefix]
    private static bool SeedChooserScreen_Update_Prefix(SeedChooserScreen __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            bool flash = FlashRequiredTimer.HasElapsed(2f);
            bool resetFlash = FlashRequiredTimer.HasElapsed(1f);

            foreach (var seedEntry in SeedChooserEntries)
            {
                // Update flying plant seedpackets
                var chosenSeed = seedEntry.m_chosenSeed;
                if (chosenSeed.mSeedState == ChosenSeedState.SeedFlyingToBank && chosenSeed.mFlying)
                {
                    chosenSeed.mTimeInMotion += Time.deltaTime * 0.4f;
                    if (chosenSeed.mTimeInMotion >= chosenSeed.mDurationOfMotion)
                    {
                        __instance.LandAllFlyingSeeds();
                    }
                }

                if (flash)
                {
                    FlashRequiredSeedType(chosenSeed);
                }
                else if (resetFlash)
                {
                    chosenSeed.mFlashing = false;
                }

                // Update seed packet recommendation in chooser.
                SetSeedPacketRecommendations(seedEntry, chosenSeed);
            }

            foreach (var seedEntry in SeedChooserZombieEntries)
            {
                // Update flying zombie seedpackets
                var chosenSeed = seedEntry.m_chosenSeed;
                if (chosenSeed.mSeedState == ChosenSeedState.SeedFlyingToBank && chosenSeed.mFlying)
                {
                    chosenSeed.mTimeInMotion += Time.deltaTime * 0.4f;
                    if (chosenSeed.mTimeInMotion >= chosenSeed.mDurationOfMotion)
                    {
                        __instance.LandAllFlyingSeeds();
                    }
                }

                if (flash)
                {
                    FlashRequiredSeedType(chosenSeed);
                }
                else if (resetFlash)
                {
                    chosenSeed.mFlashing = false;
                }

                // Update seed packet recommendation in chooser.
                SetSeedPacketRecommendations(seedEntry, chosenSeed);
            }

            if (flash)
            {
                FlashRequiredTimer.Reset();
            }

            return false;
        }

        return true;
    }

    private static void FlashRequiredSeedType(ChosenSeed chosen)
    {
        if (chosen.mSeedState != ChosenSeedState.SeedInChooser)
            return;

        var seedType = chosen.mSeedType;
        bool isZombie = Challenge.IsZombieSeedType(seedType);
        var flags = IArena.GetCurrentArena().GetSeedTypeCustomRecommentedFlags(seedType);

        if (flags.HasFlag(CustomRecommentedFlags.Required))
        {
            if (isZombie != (VersusState.VersusPhase == VersusPhase.ChooseZombiePacket))
                return;

            if (Plant.IsNocturnal(seedType) && !PvZRUtils.IsSeedTypeInAnySeedBank(SeedType.InstantCoffee) &&
                flags.HasFlag(CustomRecommentedFlags.NotRecommended))
                return;

            if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Contains(seedType))
            {
                foreach (var st in SeedPacketDefinitions.CurrencyProducingSeedTypes)
                {
                    if (Challenge.IsZombieSeedType(st) != isZombie)
                        continue;

                    if (PvZRUtils.IsSeedTypeInAnySeedBank(st))
                    {
                        return;
                    }
                }
            }

            chosen.mFlashing = true;
        }
    }

    private static void SetSeedPacketRecommendations(SeedChooserEntryModel seedChooserEntryModel, ChosenSeed chosen)
    {
        var flags = IArena.GetCurrentArena().GetSeedTypeCustomRecommentedFlags(chosen.mSeedType);

        bool isNotAllowed = flags.HasFlag(CustomRecommentedFlags.NotAllowed);
        bool isNotRecommended = flags.HasFlag(CustomRecommentedFlags.NotRecommended) ||
            chosen.mSeedState != ChosenSeedState.SeedInChooser;

        if (isNotAllowed)
        {
            chosen.mSeedState = ChosenSeedState.SeedPacketHidden;
        }
        else if (chosen.mSeedState == ChosenSeedState.SeedPacketHidden)
        {
            chosen.mSeedState = ChosenSeedState.SeedInChooser;
        }

        chosen.mNotSuggested = isNotAllowed || isNotRecommended;
    }

    [HarmonyPatch(typeof(VSTooltipHider), nameof(VSTooltipHider.Update))]
    [HarmonyPostfix]
    private static void VSTooltipHider_Update_Postfix(VSTooltipHider __instance)
    {
        var seedPacket = __instance.transform;
        var grid = seedPacket.parent;
        var seedChooser = grid.parent;
        if (seedChooser.name == "SeedChooser")
        {
            for (int i = 0; i < SeedChooserEntries.Count; i++)
            {
                SeedChooserEntryModel? entry = SeedChooserEntries[i];
                if (SetSeedPacketDisabled(seedPacket, entry, i))
                {
                    break;
                }
            }
        }
        else
        {
            for (int i = 0; i < SeedChooserZombieEntries.Count; i++)
            {
                SeedChooserEntryModel? entry = SeedChooserZombieEntries[i];
                if (SetSeedPacketDisabled(seedPacket, entry, i))
                {
                    break;
                }
            }
        }
    }

    private static bool SetSeedPacketDisabled(Transform seedPacket, SeedChooserEntryModel seedChooserEntryModel, int index)
    {
        if (index != seedPacket.GetSiblingIndex() - 1)
            return false;

        bool isDisabled = seedChooserEntryModel.m_chosenSeed.mSeedState != ChosenSeedState.SeedInChooser;

        var disableOverlay = seedPacket.Find("Offset/DisabledGameObject");
        if (disableOverlay != null)
        {
            disableOverlay.gameObject.SetActive(isDisabled);

            var disableOverlayImage = disableOverlay.GetComponent<Image>();
            if (disableOverlayImage != null)
            {
                disableOverlayImage.color = new(0f, 0f, 0f, 0.8f);
            }
        }

        return true;
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
