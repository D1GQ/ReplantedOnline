using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class BungeeZombiePatch
{
    /// Prevents Bungee Zombies from picking random targets in multiplayer
    /// This fixes synchronization issues with Bungee Zombie spawning positions
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PickBungeeZombieTarget))]
    [HarmonyPrefix]
    private static bool Zombie_PickBungeeZombieTarget_Prefix()
    {
        // Disable random Bungee Zombie target selection in multiplayer
        // Target selection should be handled through network synchronization instead
        if (NetLobby.AmInLobby())
        {
            return false; // Skip the original method
        }

        return true; // Allow original method in single player
    }
}
