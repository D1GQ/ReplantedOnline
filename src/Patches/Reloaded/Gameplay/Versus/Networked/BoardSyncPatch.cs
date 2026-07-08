using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using ReplantedOnline.Network.Reloaded.Client.Routing.Rpc;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class BoardSyncPatch
{
    [HarmonyPatch(typeof(Board), nameof(Board.AddALadder))]
    [HarmonyPrefix]
    private static bool Board_AddALadder_Prefix(int theGridX, int theGridY)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Send network message to sync this action with other players
            NetworkManager.Rpc<AddLadderRpc>.Singleton.Send(theGridX, theGridY);
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