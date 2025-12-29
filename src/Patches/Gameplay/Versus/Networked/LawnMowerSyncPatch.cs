using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.RPC.Handlers;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class LawnMowerSyncPatch
{
    [HarmonyPatch(typeof(LawnMower), nameof(LawnMower.MowZombie))]
    [HarmonyPrefix]
    private static bool LawnMower_MowZombie_Prefix(LawnMower __instance, Zombie theZombie)
    {
        // Skip network logic if this is an internal call (prevents infinite recursion)
        if (InternalCallContext.IsInternalCall_StartMower) return true;

        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Send network message to sync this action with other players
            var netZombie = theZombie.GetNetworked<ZombieNetworked>();

            MowZombieHandler.Send(__instance.Row, netZombie);

            __instance.MowZombieOriginal(theZombie);

            netZombie.DespawnAndDestroy();

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original MowZombie method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    internal static void MowZombieOriginal(this LawnMower __instance, Zombie theZombie)
    {
        InternalCallContext.IsInternalCall_StartMower = true;
        try
        {
            __instance.MowZombie(theZombie);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_StartMower = false;
        }
    }

    /// <summary>
    /// Thread-safe context flags to prevent infinite recursion when calling patched methods from within patches.
    /// [ThreadStatic] ensures each thread has its own copy of these flags.
    /// </summary>
    private static class InternalCallContext
    {
        [ThreadStatic]
        public static bool IsInternalCall_StartMower;
    }
}