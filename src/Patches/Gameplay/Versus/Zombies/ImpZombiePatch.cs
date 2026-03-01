using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Steam;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class ImpZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPostfix]
    private static void Zombie_ZombieInitialize_Postfix(Zombie __instance, ZombieType theType)
    {
        if (theType != ZombieType.Imp) return;

        if (NetLobby.AmInLobby() && VersusState.AmPlantSide)
        {
            // Spawn imp on network, for some reason this gets initialized twice...
            if (__instance.GetNetworked<ZombieNetworked>() == null)
            {
                SeedPacketSyncPatch.SpawnZombieOnNetwork(__instance, 0, __instance.mRow, false);
            }
        }
    }
}