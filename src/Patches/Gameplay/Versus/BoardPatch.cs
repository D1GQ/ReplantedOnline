using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class BoardPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.DoPlantingEffects))]
    [HarmonyPrefix]
    private static bool Board_DoPlantingEffects_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
        {
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
