using HarmonyLib;
using Il2Cpp;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Binders;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Online.ClientRPC;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class SeedChooserScreenSyncPatch
{
    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInChooser))]
    [HarmonyPrefix]
    private static bool SeedChooserScreen_AddChosenSeedToBank_Prefix(SeedChooserScreen __instance, ChosenSeed theChosenSeed, int playerIndex)
    {
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

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(SeedChooserScreen), nameof(SeedChooserScreen.ClickedSeedInChooser))]
    internal static void ClickedSeedInChooserOriginal(this SeedChooserScreen __instance, ChosenSeed theChosenSeed, int playerIndex)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}