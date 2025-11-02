using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using UnityEngine;

namespace ReplantedOnline.Patches.Versus;

[HarmonyPatch]
internal static class NetworkSyncPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.AddCoin))]
    [HarmonyPrefix]
    internal static bool BoardAddCoin_Prefix(Board __instance, float theX, float theY, CoinType theCoinType, CoinMotion theCoinMotion, ref Coin __result)
    {
        // Skip if this is an internal recursive call to avoid infinite loops
        if (InternalCallContext.IsInternalCall_1) return true;

        // Only handle network synchronization when in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // Only the host should create coins - clients wait for network spawn
            if (!NetLobby.AmLobbyHost()) return false;

            // Call the original method to create the actual coin
            var coin = __instance.BoardAddCoinOriginal(theX, theY, theCoinType, theCoinMotion);
            __result = coin;

            // Spawn a networked controller for this coin to sync across clients
            var netClass = NetworkClass.SpawnNew<CoinControllerNetworked>(net =>
            {
                net.coin = coin;
                net.boardPos = new Vector2(theX, theY);
                net.theCoinType = theCoinType;
                net.theCoinMotion = theCoinMotion;
            });

            // Track the relationship between coin and its network controller
            CoinControllerNetworked.NetworkedCoinControllers[coin] = netClass;

            // Skip the original method since we already called it manually
            return false;
        }

        // Not in lobby - allow normal coin creation
        return true;
    }

    internal static Coin BoardAddCoinOriginal(this Board __instance, float theX, float theY, CoinType theCoinType, CoinMotion theCoinMotion)
    {
        InternalCallContext.IsInternalCall_1 = true;
        try
        {
            // This will trigger the prefix patch again, but the flag prevents recursion
            return __instance.AddCoin(theX, theY, theCoinType, theCoinMotion);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_1 = false;
        }
    }

    [HarmonyPatch(typeof(Coin), nameof(Coin.Collect))]
    [HarmonyPrefix]
    internal static bool CoinCollect_Prefix(Coin __instance, int playerIndex, bool spawnCoins = true)
    {
        // Skip if this is an internal recursive call
        if (InternalCallContext.IsInternalCall_2) return true;

        // Only handle network synchronization when in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // If this coin has a network controller, notify other clients about collection
            if (CoinControllerNetworked.NetworkedCoinControllers.TryGetValue(__instance, out var networkedCoinControllers))
            {
                networkedCoinControllers.SendRpc(0, null, false);
            }

            // Call the original collection logic
            __instance.OriginalCoinCollect(playerIndex, spawnCoins);

            // Skip the original method since we already called it manually
            return false;
        }

        // Not in lobby - allow normal coin collection
        return true;
    }

    internal static void OriginalCoinCollect(this Coin __instance, int playerIndex, bool spawnCoins = true)
    {
        InternalCallContext.IsInternalCall_2 = true;
        try
        {
            // This will trigger the prefix patch again, but the flag prevents recursion
            __instance.Collect(playerIndex, spawnCoins);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_2 = false;
        }
    }

    /// <summary>
    /// Thread-safe context flags to prevent infinite recursion when calling patched methods from within patches.
    /// [ThreadStatic] ensures each thread has its own copy of these flags.
    /// </summary>
    private static class InternalCallContext
    {
        [ThreadStatic]
        public static bool IsInternalCall_1;

        [ThreadStatic]
        public static bool IsInternalCall_2;
    }
}