using HarmonyLib;
using Il2CppReloaded;
using Il2CppReloaded.DataModels;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.Controllers;
using Il2CppTekly.DataModels.Models;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus;

[HarmonyPatch]
internal static class SeedBankPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.GetNumSeedsInBank))]
    [HarmonyPostfix]
    private static void Board_GetNumSeedsInBank_Postfix(ref int __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Set seed back size
            __result = IArenaSetupSeedbank.GetSeedPacketCount();
        }
    }

    [HarmonyPatch(typeof(SeedBank), nameof(SeedBank.GetPacketCount))]
    [HarmonyPrefix]
    private static bool SeedBank_GetPacketCount_Prefix(SeedBank __instance, ref int __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Work around hard coded required seeds in seedbank to start game
            int inBank = 0;
            foreach (var seedPacket in __instance.SeedPackets)
            {
                if (seedPacket.mPacketType != SeedType.None)
                {
                    inBank++;
                }
            }

            __result = inBank - (IArenaSetupSeedbank.GetSeedPacketCount() - VersusMode.k_numPackets);
        }

        return false;
    }

    [HarmonyPatch(typeof(SeedBankDataModel), nameof(SeedBankDataModel.UpdateEntries))]
    [HarmonyPostfix]
    private static void SeedBankDataModel_UpdateEntries_Postfix(SeedBankDataModel __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Fix custom seed packets not being selectable on keyboard and mouse
            var refList = __instance.m_entriesModel.Models.Cast<Il2CppSystem.Collections.Generic.List<ModelReference>>();
            foreach (var modelRef in refList)
            {
                var seedBankEntry = modelRef.Model.Cast<SeedBankEntryModel>();
                var trigger = new Il2CppTekly.Common.Observables.Triggerable<ButtonModel>();
                seedBankEntry.m_activateButtonModel.m_activated = trigger;
                trigger.Subscribe((Action<ButtonModel>)(buttom =>
                {
                    if (__instance.SeedBankPlayerIndex != ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX) return;
                    if (!seedBankEntry.CanAfford()) return;
                    if (!seedBankEntry.m_seedPacket.CanPickUp()) return;
                    seedBankEntry.m_seedPacket.mBoard.RefreshSeedPacketFromCursor(__instance.SeedBankPlayerIndex);
                    seedBankEntry.m_seedPacket.mBoard.ClearCursor(false, __instance.SeedBankPlayerIndex);
                    seedBankEntry.m_seedPacket.Activated(__instance.SeedBankPlayerIndex, false);
                    seedBankEntry.m_seedPacket.mFlashReady = false;
                }));
            }
        }
    }

    [HarmonyPatch(typeof(ButtonAudio), nameof(ButtonAudio.OnEnable))]
    [HarmonyPostfix]
    private static void ButtonAudio_OnEnable_Postfix(ButtonAudio __instance)
    {
        // Remove duplicate seedpacket sound 
        if (__instance.gameObject.name == "SeedBackground")
        {
            __instance.enabled = false;
        }
    }

    [HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.CreatePreviewController), [typeof(SeedType), typeof(ReloadedObject)])]
    [HarmonyPostfix]
    private static void GameplayActivity_CreatePreviewController_Postfix(GameplayActivity __instance, SeedType seedType, ref PreviewController __result)
    {
        CustomSeedType customSeedType = seedType;
        if (customSeedType.IsValid())
        {
            if (!customSeedType.HasValidZombieType())
            {
                __result.Set(seedType);
            }
            else
            {
                ZombieType zombieType = customSeedType;

                __result.Set(zombieType);
            }

            __result.m_visualOffset = __instance.m_dataService.GetPlantDefinition(seedType).PreviewSpriteOffset;
        }
    }
}
