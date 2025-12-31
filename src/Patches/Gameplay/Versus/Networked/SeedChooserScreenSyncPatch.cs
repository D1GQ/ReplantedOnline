using HarmonyLib;
using Il2Cpp;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Binders;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.ClientRPC;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class SeedChooserScreenSyncPatch
{
    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInChooser))]
    [HarmonyPrefix]
    private static bool SeedChooserScreen_AddChosenSeedToBank_Prefix(SeedChooserScreen __instance, ChosenSeed theChosenSeed, int playerIndex)
    {
        // Skip if this is an internal recursive call to avoid infinite loops
        if (InternalCallContext.IsInternalCall_ClickedSeedInChooser) return true;

        if (NetLobby.AmInLobby())
        {
            if (!NetLobby.LobbyData.AllClientsReady()) return false;

            ChooseSeedClientRPC.Send(theChosenSeed);
            __instance.ClickedSeedInChooserOriginal(theChosenSeed, playerIndex);

            if (ModInfo.DEBUG)
            {
                if (NetLobby.GetLobbyMemberCount() == 1)
                {
                    var seedChooserVSSwap = UnityEngine.Object.FindObjectOfType<SeedChooserVSSwap>();
                    seedChooserVSSwap.playerTurn = 0;
                    seedChooserVSSwap.GetComponent<VersusChooserSwapBinder>().PlayerTurn = 0;

                    if (VersusState.AmPlantSide)
                    {
                        Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChoosePlantPacket;
                    }
                    else
                    {
                        Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChooseZombiePacket;
                    }
                }
            }

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
