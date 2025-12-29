using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.RPC.Handlers;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class BoardSyncPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.AddALadder))]
    [HarmonyPrefix]
    private static bool Board_AddALadder_Prefix(Board __instance, int theGridX, int theGridY)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_AddALadder) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Send network message to sync this action with other players
            AddLadderHandler.Send(theGridX, theGridY);

            return true;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original AddALadder method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    internal static void AddALadderOriginal(this Board __instance, int theGridX, int theGridY)
    {
        InternalCallContext.IsInternalCall_AddALadder = true;
        try
        {
            __instance.AddALadder(theGridX, theGridY);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_AddALadder = false;
        }
    }

    /// <summary>
    /// Thread-safe context flags to prevent infinite recursion when calling patched methods from within patches.
    /// [ThreadStatic] ensures each thread has its own copy of these flags.
    /// </summary>
    private static class InternalCallContext
    {
        [ThreadStatic]
        public static bool IsInternalCall_AddALadder;
    }
}