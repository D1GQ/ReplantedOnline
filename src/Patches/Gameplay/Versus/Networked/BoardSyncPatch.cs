using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Online.ClientRPC;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class BoardSyncPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.AddALadder))]
    [HarmonyPrefix]
    private static bool Board_AddALadder_Prefix(Board __instance, int theGridX, int theGridY)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Send network message to sync this action with other players
            AddLadderClientRPC.Send(theGridX, theGridY);

            __instance.AddALadderOriginal(theGridX, theGridY);

            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Board), nameof(Board.AddALadder))]
    internal static GridItem AddALadderOriginal(this Board __instance, int theGridX, int theGridY)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}