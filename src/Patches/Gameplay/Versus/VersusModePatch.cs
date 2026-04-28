using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Exceptions;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class VersusModePatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.GetNumSeedsInBank))]
    [HarmonyPostfix]
    private static void Board_GetNumSeedsInBank_Postfix(ref int __result)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Set seed back size
            __result = IArenaSetupSeedbank.GetSeedPacketCount();
        }
    }

    [HarmonyPatch(typeof(SeedBank), nameof(SeedBank.GetPacketCount))]
    [HarmonyPrefix]
    private static bool SeedBank_GetPacketCount_Prefix(SeedBank __instance, ref int __result)
    {
        if (ReplantedLobby.AmInLobby())
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

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.InitializeGameplay))]
    [HarmonyPrefix]
    private static void VersusMode_InitializeGameplay_Prefix(VersusMode __instance)
    {
        updateInterval.Reset();
        __instance.m_app.BackgroundController.EnableBowlingLine(true, 515);
        __instance.ClearBoard();
        IArena.GetCurrentArena()?.InitializeArena(__instance);
        foreach (var plant in __instance.m_board.GetPlants())
        {
            // Update position visually in count down
            plant.UpdateInternal();
        }
        foreach (var zombie in __instance.m_board.GetZombies())
        {
            // Update position visually in count down
            zombie.UpdateReanim();
        }
        IVersusGamemode.GetCurrentGamemode()?.OnGameplayStart(__instance);
        VersusGameplayManager.OnStart();

        // Prevent initial plants and zombies from being placed
        throw new SilentPatchException();
    }

    private readonly static ExecuteInterval updateInterval = new();
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateGameplay))]
    [HarmonyPostfix]
    private static void VersusMode_UpdateGameplay_Postfix(VersusMode __instance)
    {
        if (!ReplantedLobby.AmInLobby()) return;

        __instance.ZombieLife = ReplantedLobby.LobbyData.ZombieLife;

        if (updateInterval.Execute())
        {
            IArena.GetCurrentArena()?.UpdateArena(__instance);
            IVersusGamemode.GetCurrentGamemode()?.UpdateGameplay(__instance);
        }
    }

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.SetFocus))]
    [HarmonyPrefix]
    private static bool VersusMode_SetFocus_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.SetFocus))]
    internal static void SetFocusOriginal(this VersusMode __instance, GameObject focusTarget, Vector3 focusOffset)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateBobsledSpawning))]
    [HarmonyPrefix]
    private static bool VersusMode_UpdateBobsledSpawning_Prefix()
    {
        // Only apply these changes when in an online lobby
        if (ReplantedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Board), nameof(Board.AddCoin))]
    [HarmonyPrefix]
    private static bool Board_BoardAddCoin_Prefix(CoinType theCoinType)
    {
        // Only apply these changes when in an online lobby
        if (ReplantedLobby.AmInLobby())
        {
            if (theCoinType is CoinType.Silver or CoinType.Gold or CoinType.Diamond)
            {
                return false;
            }

            if (theCoinType is CoinType.VersusTrophyPlant or CoinType.VersusTrophyZombie)
            {
                return false;
            }

            if (theCoinType == CoinType.Sun && (VersusState.AmZombieSide || VersusState.AmSpectator))
            {
                return false; // Don't allow sun to spawn 
            }
            else if (theCoinType == CoinType.Brain && (VersusState.AmPlantSide || VersusState.AmSpectator))
            {
                return false; // Don't allow brain to spawn 
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Board), nameof(Board.CanPlantAt))]
    [HarmonyPostfix]
    private static void Board_CanPlantAt_Postfix(int theGridX, int theGridY, SeedType theType, ref PlantingReason __result)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Custom place conditions 
            if (!SeedPacketDefinitions.CanPlace(theType, theGridX, theGridY))
            {
                __result = PlantingReason.NotHere;
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.AddGraveStones))]
    [HarmonyPrefix]
    private static bool Board_CanAddGraveStoneAt_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Don't spawn gravestones in Night Arena
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Update))]
    [HarmonyPrefix]
    private static void Plant_Update_Prefix(Plant __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // If player is NOT on plant team (zombie or spectator)
            if (!VersusState.AmPlantSide)
            {
                // If this plant produces sun
                if (__instance.MakesSun())
                {
                    // Set countdown to max value, effectively disabling sun production
                    __instance.mLaunchCounter = int.MaxValue;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateGravestone))]
    [HarmonyPrefix]
    private static void Zombie_UpdateGravestone_Prefix(Zombie __instance, ref bool __state)
    {
        __state = false; // Initialize state to track if we should apply nerf

        if (ReplantedLobby.AmInLobby())
        {
            // If player IS on zombie team
            if (VersusState.AmZombieSide)
            {
                // Check if gravestone is about to spawn (counter <= 1)
                if (__instance.mPhaseCounter <= 1)
                {
                    __state = true; // Mark to apply nerf in postfix
                }
            }
            else // If NOT on zombie team (plant or spectator)
            {
                // Completely disable gravestone spawning
                __instance.mPhaseCounter = int.MaxValue;
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateGravestone))]
    [HarmonyPostfix]
    private static void Zombie_UpdateGravestone_Postfix(Zombie __instance, bool __state)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // If we marked this zombie for nerf in the prefix
            if (__state)
            {
                // Increase gravestone spawn timer by 35% (nerfing spawn rate)
                __instance.mPhaseCounter = VersusGameplayManager.MultiplyGraveCounter(__instance.mPhaseCounter);
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.UpdateSunSpawning))]
    [HarmonyPrefix]
    private static void Board_UpdateSunSpawning_Prefix(Board __instance, ref bool __state)
    {
        __state = false; // Initialize state

        if (ReplantedLobby.AmInLobby())
        {
            // If player IS on zombie team
            if (VersusState.AmZombieSide)
            {
                // Check if sun is about to spawn naturally
                if (__instance.mSunCountDown <= 1)
                {
                    __state = true; // Mark to apply nerf in postfix
                }
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.UpdateSunSpawning))]
    [HarmonyPostfix]
    private static void Board_UpdateSunSpawning_Postfix(Board __instance, bool __state)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // If we marked for nerf in the prefix
            if (__state)
            {
                // Increase sun spawn timer 
                __instance.mSunCountDown = VersusGameplayManager.MultiplyBrainSpawnCounter(__instance.mSunCountDown);
            }
        }
    }
}
