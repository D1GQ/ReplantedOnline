using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Rpc;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class LawnMowerSyncPatch
{
    [HarmonyPatch(typeof(LawnMower), nameof(LawnMower.MowZombie))]
    [HarmonyPrefix]
    private static bool LawnMower_MowZombie_Prefix(LawnMower __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Send network message to sync starting Mower
            if (__instance.mMowerState == LawnMowerState.Ready)
            {
                Rpc<StartMowerRpc>.Instance.Send(__instance);
            }

            return true;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(LawnMower), nameof(LawnMower.MowZombie))]
    internal static void MowZombieOriginal(this LawnMower __instance, Zombie theZombie)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}