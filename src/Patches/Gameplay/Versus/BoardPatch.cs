using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Exceptions;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class BoardPatch
{
    /// Reworks wave zombie spawning to use RPCs for network synchronization
    /// Handles zombies spawned during waves
    [HarmonyPatch(typeof(Board), nameof(Board.AddZombieInRow))]
    [HarmonyPrefix]
    private static bool Board_AddZombieInRow_Prefix(ZombieType theZombieType, int theRow, int theFromWave, ref Zombie __result)
    {
        // Only intercept during active gameplay in multiplayer
        if (ReplantedLobby.AmInLobby() && VersusState.IsInGameplay)
        {
            // Allow Target zombies (like Target Zombie from I Zombie) to use original logic
            if (theZombieType is ZombieType.Target) return true;

            // Prevent zombies from spawning when flag zombie is spawned, this is handled in FlagZombiePatch.cs
            if (theZombieType is ZombieType.Normal or ZombieType.TrafficCone or ZombieType.Pail)
            {
                __result = ObjectUtils.CreateReloadedObject<Zombie>();
                return false;
            }

            if (!VersusState.AmPlantSide)
            {
                throw new SilentPatchException();
            }

            // Prevent imps from spawning normally, this is handled in GargantuarZombiePatch.cs
            if (theZombieType is ZombieType.Imp) throw new SilentPatchException();

            // Spawn zombie at column 9 (right side of board) with network synchronization
            __result = SeedPacketDefinitions.SpawnZombie(theZombieType, 9, theRow, true).Zombie;

            // Prevent VersusMode from handling Bobsled, this is done in BobsledZombiePatch.cs
            if (theZombieType == ZombieType.Bobsled) throw new SilentPatchException();

            // Skip original method since we handled spawning with network sync
            return false;
        }

        return true; // Allow original method in single player or non-gameplay phases
    }

    [HarmonyPatch(typeof(Board), nameof(Board.InitLawnMowers))]
    [HarmonyPrefix]
    private static bool Board_InitLawnMowers_Prefix(Board __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Always initialize lawn mowers.
            for (int row = 0; row < __instance.GetNumRows(); row++)
            {
                var lawMower = __instance.m_lawnMowers.DataArrayAlloc();
                lawMower.LawnMowerInitialize(row, __instance.mApp);
            }

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(CutScene), nameof(CutScene.AddFlowerPots))]
    [HarmonyPrefix]
    private static bool CutScene_AddFlowerPots_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Prevent flower pots from spawning, this is handled by the Arena class!
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Board), nameof(Board.DoPlantingEffects))]
    [HarmonyPrefix]
    private static bool Board_DoPlantingEffects_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Prevent planting effect and sound playing during countdown
            if (VersusState.IsInCountDown)
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

    [HarmonyPatch(typeof(Board), nameof(Board.VacuumCoins))]
    [HarmonyPrefix]
    private static bool Board_VacuumCoins_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Remove ability to collect all coins on the screen!
            return false;
        }

        return true;
    }
}
