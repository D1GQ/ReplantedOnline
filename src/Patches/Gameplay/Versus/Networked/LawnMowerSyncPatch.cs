using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Server.ClientRPC;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class LawnMowerSyncPatch
{
    [HarmonyPatch(typeof(LawnMower), nameof(LawnMower.MowZombie))]
    [HarmonyPrefix]
    private static bool LawnMower_MowZombie_Prefix(LawnMower __instance, Zombie theZombie)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            // Send network message to sync this action with other players
            var netZombie = theZombie.GetNetworked<ZombieNetworked>();
            MowZombieClientRPC.Send(__instance, netZombie);

            return true;
        }

        return true;
    }

    [HarmonyPatch(typeof(LawnMower), nameof(LawnMower.MowZombie))]
    [HarmonyPostfix]
    private static void LawnMower_MowZombie_Postfix(LawnMower __instance, Zombie theZombie)
    {
        if (NetLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                var netZombie = theZombie.GetNetworked<ZombieNetworked>();

                if (netZombie.ZombieType is not (ZombieType.Target or ZombieType.Gravestone))
                {
                    netZombie.DespawnAndDestroy();
                }
            }
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(LawnMower), nameof(LawnMower.MowZombie))]
    internal static void MowZombieOriginal(this LawnMower __instance, Zombie theZombie)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}