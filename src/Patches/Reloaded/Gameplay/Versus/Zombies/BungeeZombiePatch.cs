using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Reloaded.Client;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class BungeeZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PickBungeeZombieTarget))]
    [HarmonyPrefix]
    private static bool Zombie_PickBungeeZombieTarget_Prefix()
    {
        // Prevents Bungee Zombies from picking random targets
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }
}
