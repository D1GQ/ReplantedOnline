using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Rpc;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class BoardSyncPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.AddALadder))]
    [HarmonyPrefix]
    private static bool Board_AddALadder_Prefix(int theGridX, int theGridY)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Send network message to sync this action with other players
            Rpc<AddLadderRpc>.Instance.Send(theGridX, theGridY);
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