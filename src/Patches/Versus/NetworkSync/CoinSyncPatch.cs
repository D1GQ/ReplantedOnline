using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using UnityEngine;

namespace ReplantedOnline.Patches.Versus.NetworkSync;

[HarmonyPatch]
internal static class CoinSyncPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.AddCoin))]
    [HarmonyPrefix]
    private static bool BoardAddCoin_Prefix(Board __instance, float theX, float theY, CoinType theCoinType, CoinMotion theCoinMotion, ref Coin __result)
    {
        // Skip if this is an internal recursive call to avoid infinite loops
        if (InternalCallContext.IsInternalCall_AddCoin) return true;

        // Only handle network synchronization when in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            var doSpawn = theCoinType != CoinType.Sun && theCoinType != CoinType.Brain;

            if (doSpawn)
            {
                if (!NetLobby.AmLobbyHost()) return false;

                var coin = __instance.AddCoinOriginal(theX, theY, theCoinType, theCoinMotion);
                __result = coin;

                var netClass = NetworkClass.SpawnNew<CoinNetworked>(net =>
                {
                    net._Coin = coin;
                    net.BoardGridPos = new Vector2(theX, theY);
                    net.TheCoinType = theCoinType;
                    net.TheCoinMotion = theCoinMotion;
                });

                // Track the relationship between coin and its network controller
                coin.AddNetworkedLookup(netClass);
            }
            else
            {
                if (theCoinType == CoinType.Sun && VersusState.PlantSide)
                {
                    var coin = __instance.AddCoinOriginal(theX, theY, theCoinType, theCoinMotion);
                    __result = coin;
                }
                else if (theCoinType == CoinType.Brain && VersusState.ZombieSide)
                {
                    var coin = __instance.AddCoinOriginal(theX, theY, theCoinType, theCoinMotion);
                    __result = coin;
                }
            }

            // Skip the original method since we already called it manually
            return false;
        }

        // Not in lobby - allow normal coin creation
        return true;
    }

    internal static Coin AddCoinOriginal(this Board __instance, float theX, float theY, CoinType theCoinType, CoinMotion theCoinMotion)
    {
        InternalCallContext.IsInternalCall_AddCoin = true;
        try
        {
            // This will trigger the prefix patch again, but the flag prevents recursion
            return __instance.AddCoin(theX, theY, theCoinType, theCoinMotion);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_AddCoin = false;
        }
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.Collect))]
    [HarmonyPrefix]
    private static bool CoinCollect_Prefix(Coin __instance, int playerIndex, bool spawnCoins)
    {
        // Skip if this is an internal recursive call
        if (InternalCallContext.IsInternalCall_CoinCollect) return true;

        // Only handle network synchronization when in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // If this coin has a network controller, notify other clients about collection
            __instance.GetNetworked<CoinNetworked>()?.SendCollectRpc();

            // Call the original collection logic
            __instance.CollectOriginal(playerIndex, spawnCoins);

            // Skip the original method since we already called it manually
            return false;
        }

        // Not in lobby - allow normal coin collection
        return true;
    }

    internal static void CollectOriginal(this Coin __instance, int playerIndex, bool spawnCoins = true)
    {
        InternalCallContext.IsInternalCall_CoinCollect = true;
        try
        {
            // This will trigger the prefix patch again, but the flag prevents recursion
            __instance.Collect(playerIndex, spawnCoins);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_CoinCollect = false;
        }
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.Die))]
    [HarmonyPrefix]
    internal static bool Die_Prefix(Coin __instance)
    {
        if (InternalCallContext.IsInternalCall_Die) return true;

        if (NetLobby.AmInLobby())
        {
            if (!NetLobby.AmLobbyHost()) return false;

            __instance.GetNetworked<CoinNetworked>()?.SendDieRpc();
            __instance.DieOriginal();

            return false;
        }

        return true;
    }

    internal static void DieOriginal(this Coin __instance)
    {
        InternalCallContext.IsInternalCall_Die = true;
        try
        {
            __instance.Die();
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_Die = false;
        }
    }


    /// <summary>
    /// Thread-safe context flags to prevent infinite recursion when calling patched methods from within patches.
    /// [ThreadStatic] ensures each thread has its own copy of these flags.
    /// </summary>
    private static class InternalCallContext
    {
        [ThreadStatic]
        public static bool IsInternalCall_AddCoin;

        [ThreadStatic]
        public static bool IsInternalCall_CoinCollect;

        [ThreadStatic]
        public static bool IsInternalCall_Die;
    }
}
