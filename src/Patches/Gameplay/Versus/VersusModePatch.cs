using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class VersusModePatch
{
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.InitializeGameplay))]
    [HarmonyPostfix]
    private static void VersusMode_InitializeGameplay_Postfix()
    {
        VersusManager.OnStart();
    }

    [HarmonyPatch(typeof(Board), nameof(Board.AddCoin))]
    [HarmonyPrefix]
    private static bool Board_BoardAddCoin_Prefix(CoinType theCoinType)
    {
        // Only apply these changes when in an online lobby
        if (NetLobby.AmInLobby())
        {
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

    [HarmonyPatch(typeof(Plant), nameof(Plant.Update))]
    [HarmonyPrefix]
    private static void Plant_Update_Prefix(Plant __instance)
    {
        if (NetLobby.AmInLobby())
        {
            // If player is NOT on plant team (zombie or spectator)
            if (!VersusState.AmPlantSide)
            {
                // If this plant produces sun
                if (__instance.MakesSun())
                {
                    // Set countdown to max value, effectively disabling sun production
                    __instance.mStateCountdown = int.MaxValue;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateGravestone))]
    [HarmonyPrefix]
    private static void Zombie_UpdateGravestone_Prefix(Zombie __instance, ref bool __state)
    {
        __state = false; // Initialize state to track if we should apply nerf

        if (NetLobby.AmInLobby())
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
        if (NetLobby.AmInLobby())
        {
            // If we marked this zombie for nerf in the prefix
            if (__state)
            {
                // Increase gravestone spawn timer by 35% (nerfing spawn rate)
                __instance.mPhaseCounter = VersusManager.MultiplyGraveCounter(__instance.mPhaseCounter);
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.UpdateSunSpawning))]
    [HarmonyPrefix]
    private static void Board_UpdateSunSpawning_Prefix(Board __instance, ref bool __state)
    {
        __state = false; // Initialize state

        if (NetLobby.AmInLobby())
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
        if (NetLobby.AmInLobby())
        {
            // If we marked for nerf in the prefix
            if (__state)
            {
                // Increase sun spawn timer by 35% (nerfing natural brain production)
                __instance.mSunCountDown = VersusManager.MultiplyBrainSpawnCounter(__instance.mSunCountDown);
            }
        }
    }
}
