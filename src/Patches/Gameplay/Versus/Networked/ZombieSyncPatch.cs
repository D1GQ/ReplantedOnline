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

            __instance.GetNetworked<ZombieNetworked>().SendDeathRpc(theDamageFlags);

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

            __instance.GetNetworked<ZombieNetworked>().SendTakeDamageRpc(theDamage, theDamageFlags);

            return true;
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

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.HitIceTrap))]
    [HarmonyPrefix]
    private static bool Zombie_HitIceTrap_Prefix(Zombie __instance)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_HitIceTrap) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Execute the original HitIceTrap logic locally
            __instance.HitIceTrapOriginal();

            __instance.GetNetworked<ZombieNetworked>().SendSetFrozenRpc(true);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original HitIceTrap method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    public static void HitIceTrapOriginal(this Zombie __instance)
    {
        // Set flag to indicate this is an internal call
        InternalCallContext.IsInternalCall_HitIceTrap = true;
        try
        {
            // Call the original method - this won't trigger our patch due to the flag
            __instance.HitIceTrap();
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_HitIceTrap = false;
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.RemoveIceTrap))]
    [HarmonyPrefix]
    private static bool Zombie_RemoveIceTrap_Prefix(Zombie __instance)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_RemoveIceTrap) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Execute the original RemoveIceTrap logic locally
            __instance.RemoveIceTrapOriginal();

            __instance.GetNetworked<ZombieNetworked>().SendSetFrozenRpc(false);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original RemoveIceTrap method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    public static void RemoveIceTrapOriginal(this Zombie __instance)
    {
        // Set flag to indicate this is an internal call
        InternalCallContext.IsInternalCall_RemoveIceTrap = true;
        try
        {
            // Call the original method - this won't trigger our patch due to the flag
            __instance.RemoveIceTrap();
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_RemoveIceTrap = false;
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ApplyBurn))]
    [HarmonyPrefix]
    private static bool Zombie_ApplyBurn_Prefix(Zombie __instance)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_ApplyBurn) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            if (__instance.mZombieType is not (ZombieType.Gargantuar or ZombieType.RedeyeGargantuar))
            {
                __instance.GetNetworked<ZombieNetworked>().SendApplyBurnRpc();
            }

            return true;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original ApplyBurn method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    public static void ApplyBurnOriginal(this Zombie __instance)
    {
        // Set flag to indicate this is an internal call
        InternalCallContext.IsInternalCall_ApplyBurn = true;
        try
        {
            // Call the original method - this won't trigger our patch due to the flag
            __instance.ApplyBurn();
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_ApplyBurn = false;
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

        [ThreadStatic]
        public static bool IsInternalCall_HitIceTrap;

        [ThreadStatic]
        public static bool IsInternalCall_RemoveIceTrap;

        [ThreadStatic]
        public static bool IsInternalCall_ApplyBurn;
    }
}