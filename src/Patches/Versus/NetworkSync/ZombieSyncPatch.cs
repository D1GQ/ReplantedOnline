using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Versus.NetworkSync;

[HarmonyPatch]
internal static class ZombieSyncPatch
{
    /// <summary>
    /// Prefix patch that intercepts the Zombie.PlayDeathAnim method call
    /// Runs before the original method and can prevent it from executing
    /// </summary>
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PlayDeathAnim))]
    [HarmonyPrefix]
    internal static bool PlayDeathAnim_Prefix(Zombie __instance, DamageFlags theDamageFlags)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_PlayDeathAnim) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // If we're on the plant side in versus mode, don't process zombie death animations
            // (Zombie side has priority over zombie death events)
            if (VersusState.PlantSide) return false;

            // Get the networked zombie representation and send death RPC to other players
            // Includes damage flags to communicate how the zombie died
            __instance.GetNetworkedZombie()?.SendDeathRpc(theDamageFlags);

            // Execute the original death animation logic locally
            __instance.PlayDeathAnimOriginal(theDamageFlags);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original PlayDeathAnim method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    internal static void PlayDeathAnimOriginal(this Zombie __instance, DamageFlags theDamageFlags)
    {
        // Set flag to indicate this is an internal call
        InternalCallContext.IsInternalCall_PlayDeathAnim = true;
        try
        {
            // Call the original method - this won't trigger our patch due to the flag
            __instance.PlayDeathAnim(theDamageFlags);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_PlayDeathAnim = false;
        }
    }

    /// <summary>
    /// Thread-safe context flags to prevent infinite recursion when calling patched methods from within patches.
    /// [ThreadStatic] ensures each thread has its own copy of these flags.
    /// </summary>
    private static class InternalCallContext
    {
        [ThreadStatic]
        public static bool IsInternalCall_PlayDeathAnim;
    }
}