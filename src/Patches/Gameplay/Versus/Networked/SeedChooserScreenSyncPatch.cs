#pragma warning disable CS0162

using HarmonyLib;
using Il2Cpp;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Binders;
using ReplantedOnline.Exceptions;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Rpc;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class SeedChooserScreenSyncPatch
{
    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInChooser))]
    [HarmonyPrefix]
    private static bool SeedChooserScreen_AddChosenSeedToBank_Prefix(SeedChooserScreen __instance, ChosenSeed theChosenSeed, int playerIndex)
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (!ReplantedLobby.LobbyData.AllClientsReady()) return false;

            Rpc<ChooseSeedRpc>.Instance.Send(theChosenSeed);
            __instance.ClickedSeedInChooserOriginal(theChosenSeed, playerIndex);

            if (ModInfo.DEBUG)
            {
                if (ReplantedLobby.GetLobbyMemberCount() == 1)
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

            // Fully prevent the original method from running, due to using reverse patche to call the original method instead of the prefix
            throw new SilentPatchException();
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInChooser))]
    internal static void ClickedSeedInChooserOriginal(this SeedChooserScreen __instance, ChosenSeed theChosenSeed, int playerIndex)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}