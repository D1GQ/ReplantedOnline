using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Versus.NetworkSync;

[HarmonyPatch]
internal static class PlantSyncPatch
{
    /// <summary>
    /// Prefix patch that intercepts the Plant.Die method call
    /// Runs before the original method and can prevent it from executing
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
    [HarmonyPrefix]
    private static bool Die_Prefix(Plant __instance)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_Die) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (VersusState.ZombieSide) return false;

            // Get the networked plant representation and send death RPC to other players
            __instance.GetNetworked<PlantNetworked>()?.SendDieRpc();

            // Execute the original die method logic locally
            __instance.DieOriginal();

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original Die method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    internal static void DieOriginal(this Plant __instance)
    {
        // Set flag to indicate this is an internal call
        InternalCallContext.IsInternalCall_Die = true;
        try
        {
            // Call the original method - this won't trigger our patch due to the flag
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
        public static bool IsInternalCall_Die;
    }
}