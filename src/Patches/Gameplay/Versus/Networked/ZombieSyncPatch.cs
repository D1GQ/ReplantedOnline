using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class ZombieSyncPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PlayDeathAnim))]
    [HarmonyPrefix]
    private static bool Zombie_PlayDeathAnim_Prefix(Zombie __instance, DamageFlags theDamageFlags)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_PlayDeathAnim) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Get the networked zombie representation and send death RPC to other players
            // Includes damage flags to communicate how the zombie died
            __instance.GetNetworked<ZombieNetworked>().SendDeathRpc(theDamageFlags);

            // Execute the original death animation logic locally
            __instance.GetNetworked<ZombieNetworked>().CheckDeath(() =>
            {
                __instance.PlayDeathAnimOriginal(theDamageFlags);
            });

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

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.TakeDamage))]
    [HarmonyPrefix]
    private static bool Zombie_TakeDamage_Prefix(Zombie __instance, int theDamage, DamageFlags theDamageFlags)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_TakeDamage) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            __instance.TakeDamageOriginal(theDamage, theDamageFlags);
            __instance.GetNetworked<ZombieNetworked>().SendTakeDamageRpc(theDamage, theDamageFlags);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original TakeDamage method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    internal static void TakeDamageOriginal(this Zombie __instance, int theDamage, DamageFlags theDamageFlags)
    {
        // Set flag to indicate this is an internal call
        InternalCallContext.IsInternalCall_TakeDamage = true;
        try
        {
            // Call the original method - this won't trigger our patch due to the flag
            __instance.TakeDamage(theDamage, theDamageFlags);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_TakeDamage = false;
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

        [ThreadStatic]
        public static bool IsInternalCall_TakeDamage;
    }
}