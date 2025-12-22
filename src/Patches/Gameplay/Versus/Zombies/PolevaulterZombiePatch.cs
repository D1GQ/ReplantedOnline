using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class PolevaulterZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateZombiePolevaulter))]
    [HarmonyPrefix]
    private static bool Zombie_UpdateZombiePolevaulter_Prefix(Zombie __instance)
    {
        if (__instance.mZombieType is not ZombieType.Polevaulter) return true;

        if (NetLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }
}