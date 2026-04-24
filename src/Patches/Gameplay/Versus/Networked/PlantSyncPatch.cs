using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class PlantSyncPatch
{
    /// <summary>
    /// Prefix patch that intercepts the Plant.Die method call
    /// Runs before the original method and can prevent it from executing
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
    [HarmonyPrefix]
    private static bool Plant_Die_Prefix(Plant __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            __instance.GetNetworked()?.SendDieRpc();
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
    internal static void DieOriginal(this Plant __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Squish))]
    [HarmonyPrefix]
    private static bool Plant_Squish_Prefix(Plant __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Sync Squish
            __instance.GetNetworked()?.SendSquashPlantRpc();
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.Squish))]
    internal static void SquishOriginal(this Plant __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}