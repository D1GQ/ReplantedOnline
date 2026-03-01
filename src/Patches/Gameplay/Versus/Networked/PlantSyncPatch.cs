using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Steam;

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
        if (NetLobby.AmInLobby())
        {
            if (PlantNetworked.DoNotSyncDeath(__instance)) return true;

            if (!VersusState.AmPlantSide) return false;

            __instance.GetNetworked<PlantNetworked>().SendDieRpc();

            __instance.DieOriginal();

            return false;
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
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Sync Squish
            var netPlant = __instance.GetNetworked<PlantNetworked>();
            if (netPlant != null)
            {
                netPlant.SendSquashPlantRpc();
            }
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