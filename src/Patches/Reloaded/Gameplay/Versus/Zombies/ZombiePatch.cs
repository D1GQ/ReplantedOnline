using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class ZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.EatZombie))]
    [HarmonyPrefix]
    private static bool Zombie_EatZombie_Prefix(Zombie theZombie)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Prevent hypno affected zombies from eating target and gravestone zombies
            // This is a issue with replanted itself 
            if (theZombie.mZombieType.IsGravestoneOrTarget())
            {
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.TrySpawnLevelAward))]
    [HarmonyPrefix]
    private static bool Zombie_TrySpawnLevelAward_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DropArm))]
    [HarmonyPrefix]
    private static bool Zombie_DropArm_Prefix(Zombie __instance, DamageFlags theDamageFlags)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // For some reason this method consistently catches a null exception.
            // Garbage programmers I guess, so let's silent it out.
            try
            {
                __instance.DropArmOriginal(theDamageFlags);
            }
            catch { }
            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DropArm))]
    private static void DropArmOriginal(this Zombie __instance, DamageFlags theDamageFlags)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}