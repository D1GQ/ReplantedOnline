using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.RPC.Handlers;

namespace ReplantedOnline.Patches.Versus.NetworkSync;

[HarmonyPatch]
internal static class BoardSyncPatch
{
    /// <summary>
    /// Prefix patch that intercepts the Board.AddSunMoney method call.
    /// Runs before the original method and can prevent it from executing.
    /// </summary>
    [HarmonyPatch(typeof(Board), nameof(Board.AddSunMoney))]
    [HarmonyPrefix]
    internal static bool AddSunMoney_Prefix(Board __instance, int theAmount, int playerIndex)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_AddSunMoney) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (playerIndex == ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX)
            {
                SyncOpponentMoneyHandler.Send(__instance.mSunMoney[playerIndex], theAmount);
            }

            __instance.AddSunMoneyOriginal(theAmount, playerIndex);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original AddSunMoney method
    /// while preventing our patch from intercepting the call (avoiding recursion).
    /// </summary>
    /// <param name="__instance">The Board instance to operate on.</param>
    /// <param name="theAmount">The amount of sun money to add.</param>
    /// <param name="playerIndex">The index of the player receiving sun money.</param>
    internal static void AddSunMoneyOriginal(this Board __instance, int theAmount, int playerIndex)
    {
        // Set flag to indicate this is an internal call
        InternalCallContext.IsInternalCall_AddSunMoney = true;
        try
        {
            // Call the original method - this won't trigger our patch due to the flag
            __instance.AddSunMoney(theAmount, playerIndex);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_AddSunMoney = false;
        }
    }

    /// <summary>
    /// Prefix patch that intercepts the Board.TakeSunMoney method call.
    /// Runs before the original method and can prevent it from executing.
    /// </summary>
    [HarmonyPatch(typeof(Board), nameof(Board.TakeSunMoney))]
    [HarmonyPrefix]
    internal static bool TakeSunMoney_Prefix(Board __instance, int theAmount, int playerIndex)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_TakeSunMoney) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (playerIndex == ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX)
            {
                SyncOpponentMoneyHandler.Send(__instance.mSunMoney[playerIndex], theAmount);
            }

            __instance.TakeSunMoneyOriginal(theAmount, playerIndex);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original TakeSunMoney method
    /// while preventing our patch from intercepting the call (avoiding recursion).
    /// </summary>
    /// <param name="__instance">The Board instance to operate on.</param>
    /// <param name="theAmount">The amount of sun money to take.</param>
    /// <param name="playerIndex">The index of the player losing sun money.</param>
    internal static void TakeSunMoneyOriginal(this Board __instance, int theAmount, int playerIndex)
    {
        // Set flag to indicate this is an internal call
        InternalCallContext.IsInternalCall_TakeSunMoney = true;
        try
        {
            // Call the original method - this won't trigger our patch due to the flag
            __instance.TakeSunMoney(theAmount, playerIndex);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_TakeSunMoney = false;
        }
    }

    /// <summary>
    /// Thread-safe context flags to prevent infinite recursion when calling patched methods from within patches.
    /// [ThreadStatic] ensures each thread has its own copy of these flags.
    /// </summary>
    private static class InternalCallContext
    {
        [ThreadStatic]
        public static bool IsInternalCall_AddSunMoney;

        [ThreadStatic]
        public static bool IsInternalCall_TakeSunMoney;
    }
}