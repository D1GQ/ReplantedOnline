using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.RPC.Handlers;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class SeedChooserScreenSyncPatch
{
    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInChooser))]
    [HarmonyPrefix]
    private static bool AddChosenSeedToBank_Prefix(SeedChooserScreen __instance, ChosenSeed theChosenSeed, int playerIndex)
    {
        // Skip if this is an internal recursive call to avoid infinite loops
        if (InternalCallContext.IsInternalCall_ClickedSeedInChooser) return true;

        if (NetLobby.AmInLobby())
        {
            __instance.ClickedSeedInChooserOriginal(theChosenSeed, playerIndex);
            ChooseSeedHandler.Send(theChosenSeed);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original ClickedSeedInChooser method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    internal static void ClickedSeedInChooserOriginal(this SeedChooserScreen __instance, ChosenSeed theChosenSeed, int playerIndex)
    {
        InternalCallContext.IsInternalCall_ClickedSeedInChooser = true;
        try
        {
            // This will trigger the prefix patch again, but the flag prevents recursion
            __instance.ClickedSeedInChooser(theChosenSeed, playerIndex);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_ClickedSeedInChooser = false;
        }
    }

    /// <summary>
    /// Thread-safe context flags to prevent infinite recursion when calling patched methods from within patches.
    /// [ThreadStatic] ensures each thread has its own copy of these flags.
    /// </summary>
    private static class InternalCallContext
    {
        [ThreadStatic]
        public static bool IsInternalCall_ClickedSeedInChooser;
    }
}
