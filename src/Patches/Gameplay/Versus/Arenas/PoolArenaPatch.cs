using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Gameplay.Versus.Arenas;

[HarmonyPatch]
internal static class PoolArenaPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.CheckForPool))]
    [HarmonyPrefix]
    private static bool Zombie_CheckForPool_Prefix(Zombie __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // All logic is remade in ZombieInPoolNetworkComponent.cs
            return false;
        }

        return true;
    }
}
